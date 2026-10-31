// ETS, the team's private scrum board: where its cards and its password are kept.
//
// Everything is in the team's own PostgreSQL database on the robotics server, in a
// schema called "ets". The server hands the site the database's address and login
// in the PGHOST, PGPORT, PGUSER, PGPASSWORD and PGDATABASE settings (compose.yaml).
// Without them (on a computer with no database, or in the tests) ETS says it isn't
// set up, and the rest of the site works as usual.
//
// The password is never stored as it is: only a salted PBKDF2 hash of it, so even
// someone who reads the database can't see it. A mentor sets it with a command on
// the server; see README.md, "ETS, the private scrum board".

using System.Security.Cryptography;
using Npgsql;

namespace TeamSite.Web.Ets;

public enum CardStatus { Todo, Doing, Done }

/// <summary>
/// A card on the board. Progress is 0 in To do, 25, 50, 75 or 100 in progress, and 100 when
/// done. Due is an optional due date; DoneOn is the day it was moved to Done.
/// </summary>
public sealed record Card(int Id, string Title, string Notes, string Subteam, string Owner, CardStatus Status,
    int Progress, DateOnly? Due, DateOnly? DoneOn);

public sealed class EtsStore
{
    public static readonly string[] Subteams = ["Programming", "Technical", "Business"];

    /// <summary>The steps a card in progress can be at.</summary>
    public static readonly int[] ProgressSteps = [25, 50, 75, 100];

    /// <summary>The team's time zone, for "today", due dates and the day a card was done.</summary>
    public static readonly TimeZoneInfo TeamTime = FindTeamTime();

    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, TeamTime));

    private static TimeZoneInfo FindTeamTime()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
    }

    private const int Iterations = 600_000;
    private readonly NpgsqlDataSource? _db;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);
    private bool _schemaReady;

    public EtsStore(IConfiguration config)
    {
        var host = config["PGHOST"];
        if (string.IsNullOrWhiteSpace(host)) return;

        var cs = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(config["PGPORT"], out var port) ? port : 5432,
            Username = config["PGUSER"],
            Password = config["PGPASSWORD"],
            Database = config["PGDATABASE"],
        };
        _db = NpgsqlDataSource.Create(cs.ConnectionString);
    }

    /// <summary>True when the site has a database to keep ETS in.</summary>
    public bool HasDatabase => _db is not null;

    private NpgsqlDataSource Db => _db ?? throw new InvalidOperationException("ETS has no database (PGHOST is not set).");

    private async Task EnsureSchemaAsync()
    {
        if (_schemaReady) return;
        await _schemaLock.WaitAsync();
        try
        {
            if (_schemaReady) return;
            await using var cmd = Db.CreateCommand("""
                CREATE SCHEMA IF NOT EXISTS ets;
                CREATE TABLE IF NOT EXISTS ets.settings (
                    key   text PRIMARY KEY,
                    value text NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ets.cards (
                    id         serial PRIMARY KEY,
                    title      text NOT NULL,
                    notes      text NOT NULL DEFAULT '',
                    subteam    text NOT NULL DEFAULT '',
                    owner      text NOT NULL DEFAULT '',
                    status     text NOT NULL DEFAULT 'todo' CHECK (status IN ('todo', 'doing', 'done')),
                    created_at timestamptz NOT NULL DEFAULT now(),
                    updated_at timestamptz NOT NULL DEFAULT now(),
                    moved_at   timestamptz NOT NULL DEFAULT now()
                );
                ALTER TABLE ets.cards ADD COLUMN IF NOT EXISTS progress int NOT NULL DEFAULT 0;
                ALTER TABLE ets.cards ADD COLUMN IF NOT EXISTS due_date date;
                ALTER TABLE ets.cards ADD COLUMN IF NOT EXISTS done_on  date;
                """);
            await cmd.ExecuteNonQueryAsync();
            _schemaReady = true;
        }
        finally
        {
            _schemaLock.Release();
        }
    }

    // ---------- The password ----------

    /// <summary>True once a mentor has set the ETS password.</summary>
    public async Task<bool> HasPasswordAsync() => await GetSettingAsync("password_hash") is not null;

    /// <summary>
    /// Checks a password. Returns the current sign-in stamp when it's right, or null.
    /// The stamp changes whenever the password does, which signs everyone out.
    /// </summary>
    public async Task<string?> CheckPasswordAsync(string password)
    {
        var stored = await GetSettingAsync("password_hash");
        if (stored is null || !VerifyHash(password, stored)) return null;
        return await GetSettingAsync("password_stamp");
    }

    /// <summary>The current sign-in stamp, to check a signed-in visitor is still allowed in.</summary>
    public Task<string?> CurrentStampAsync() => GetSettingAsync("password_stamp");

    /// <summary>Sets a new password and signs everyone out.</summary>
    public async Task SetPasswordAsync(string password)
    {
        await SetSettingAsync("password_hash", Hash(password));
        await SetSettingAsync("password_stamp", Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
    }

    private static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyHash(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private async Task<string?> GetSettingAsync(string key)
    {
        await EnsureSchemaAsync();
        await using var cmd = Db.CreateCommand("SELECT value FROM ets.settings WHERE key = $1");
        cmd.Parameters.AddWithValue(key);
        return await cmd.ExecuteScalarAsync() as string;
    }

    private async Task SetSettingAsync(string key, string value)
    {
        await EnsureSchemaAsync();
        await using var cmd = Db.CreateCommand("""
            INSERT INTO ets.settings (key, value) VALUES ($1, $2)
            ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value
            """);
        cmd.Parameters.AddWithValue(key);
        cmd.Parameters.AddWithValue(value);
        await cmd.ExecuteNonQueryAsync();
    }

    // ---------- The cards ----------

    public async Task<List<Card>> CardsAsync()
    {
        await EnsureSchemaAsync();
        await using var cmd = Db.CreateCommand(
            "SELECT id, title, notes, subteam, owner, status, progress, due_date, done_on FROM ets.cards ORDER BY moved_at, id");
        await using var reader = await cmd.ExecuteReaderAsync();
        var cards = new List<Card>();
        while (await reader.ReadAsync())
        {
            cards.Add(new Card(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), ParseStatus(reader.GetString(5)), reader.GetInt32(6),
                reader.IsDBNull(7) ? null : reader.GetFieldValue<DateOnly>(7),
                reader.IsDBNull(8) ? null : reader.GetFieldValue<DateOnly>(8)));
        }
        return cards;
    }

    public async Task AddCardAsync(string title, string notes, string subteam, string owner, DateOnly? due)
    {
        await EnsureSchemaAsync();
        await using var cmd = Db.CreateCommand(
            "INSERT INTO ets.cards (title, notes, subteam, owner, due_date) VALUES ($1, $2, $3, $4, $5)");
        cmd.Parameters.AddWithValue(title);
        cmd.Parameters.AddWithValue(notes);
        cmd.Parameters.AddWithValue(subteam);
        cmd.Parameters.AddWithValue(owner);
        cmd.Parameters.Add(new NpgsqlParameter<DateOnly?> { TypedValue = due, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Date });
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EditCardAsync(int id, string title, string notes, string subteam, string owner, DateOnly? due)
    {
        await EnsureSchemaAsync();
        await using var cmd = Db.CreateCommand(
            "UPDATE ets.cards SET title = $2, notes = $3, subteam = $4, owner = $5, due_date = $6, updated_at = now() WHERE id = $1");
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(title);
        cmd.Parameters.AddWithValue(notes);
        cmd.Parameters.AddWithValue(subteam);
        cmd.Parameters.AddWithValue(owner);
        cmd.Parameters.Add(new NpgsqlParameter<DateOnly?> { TypedValue = due, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Date });
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Moves a card to another column. To do is 0%, Done is 100% and records today as the day
    /// it was done. A card moved into In progress keeps its step if it had one, or starts at 25%.
    /// </summary>
    public async Task MoveCardAsync(int id, CardStatus to)
    {
        await EnsureSchemaAsync();
        await using var cmd = Db.CreateCommand("""
            UPDATE ets.cards SET
                status   = $2,
                progress = CASE $2 WHEN 'todo' THEN 0
                                   WHEN 'done' THEN 100
                                   ELSE CASE WHEN progress IN (25, 50, 75) THEN progress ELSE 25 END END,
                done_on  = CASE WHEN $2 = 'done' THEN $3 ELSE NULL END,
                moved_at = now(), updated_at = now()
            WHERE id = $1 AND status <> $2
            """);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(StatusText(to));
        cmd.Parameters.AddWithValue(Today);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Sets how far along a card in progress is: 25, 50, 75 or 100.</summary>
    public async Task SetProgressAsync(int id, int progress)
    {
        if (!ProgressSteps.Contains(progress)) return;
        await EnsureSchemaAsync();
        await using var cmd = Db.CreateCommand(
            "UPDATE ets.cards SET progress = $2, updated_at = now() WHERE id = $1 AND status = 'doing'");
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(progress);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCardAsync(int id)
    {
        await EnsureSchemaAsync();
        await using var cmd = Db.CreateCommand("DELETE FROM ets.cards WHERE id = $1");
        cmd.Parameters.AddWithValue(id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static CardStatus ParseStatus(string s) => s switch
    {
        "doing" => CardStatus.Doing,
        "done" => CardStatus.Done,
        _ => CardStatus.Todo,
    };

    private static string StatusText(CardStatus s) => s switch
    {
        CardStatus.Doing => "doing",
        CardStatus.Done => "done",
        _ => "todo",
    };
}
