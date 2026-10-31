// Sets the ETS password from the command line. A mentor runs it on the robotics server:
//
//     sudo robotics-team docker 38350 exec -it live-web-1 dotnet TeamSite.Web.dll ets-password
//
// (dev-web-1 for the preview, which has its own database and its own password). It asks
// for the new password twice without showing it. Changing it signs everyone out of ETS.

namespace TeamSite.Web.Ets;

public static class EtsPasswordCommand
{
    private const int MinimumLength = 8;

    public static async Task RunAsync(EtsStore store)
    {
        if (!store.HasDatabase)
        {
            Console.Error.WriteLine("ETS has no database here (PGHOST is not set). Run this inside the site's container on the robotics server.");
            Environment.ExitCode = 1;
            return;
        }

        var password = Ask("New ETS password: ");
        if (password.Length < MinimumLength)
        {
            Console.Error.WriteLine($"The password needs at least {MinimumLength} characters. Nothing was changed.");
            Environment.ExitCode = 1;
            return;
        }
        if (Ask("Type it again: ") != password)
        {
            Console.Error.WriteLine("The two didn't match. Nothing was changed.");
            Environment.ExitCode = 1;
            return;
        }

        await store.SetPasswordAsync(password);
        Console.WriteLine("The ETS password is set. Everyone signed in with the old one has been signed out.");
    }

    // Reads a line without showing it. When the input comes from a pipe, reads it as it is.
    private static string Ask(string prompt)
    {
        Console.Write(prompt);
        if (Console.IsInputRedirected)
        {
            var piped = Console.ReadLine() ?? "";
            Console.WriteLine();
            return piped;
        }

        var typed = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (typed.Length > 0) typed.Length--; continue; }
            if (!char.IsControl(key.KeyChar)) typed.Append(key.KeyChar);
        }
        Console.WriteLine();
        return typed.ToString();
    }
}
