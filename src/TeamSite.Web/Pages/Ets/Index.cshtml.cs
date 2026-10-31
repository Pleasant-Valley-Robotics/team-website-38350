// The ETS board: /ets. Only open after the team password (Program.cs).
// You don't need to change this file to use the board.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeamSite.Web.Ets;

namespace TeamSite.Web.Pages.Ets;

public class IndexModel(EtsStore store) : PageModel
{
    public const int TitleMax = 200, NotesMax = 2000, OwnerMax = 60;

    public static readonly (CardStatus Status, string Name)[] Columns =
    [
        (CardStatus.Todo, "To do"),
        (CardStatus.Doing, "In progress"),
        (CardStatus.Done, "Done"),
    ];

    public List<Card> Cards { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync() => Cards = await store.CardsAsync();

    public async Task<IActionResult> OnPostAddAsync(string? title, string? notes, string? subteam, string? owner, DateOnly? due)
    {
        if (Clean(title, TitleMax) is not { Length: > 0 } t)
        {
            Error = "A card needs a title.";
            Cards = await store.CardsAsync();
            return Page();
        }
        await store.AddCardAsync(t, Clean(notes, NotesMax), CleanSubteam(subteam), Clean(owner, OwnerMax), due);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string? title, string? notes, string? subteam, string? owner, DateOnly? due)
    {
        if (Clean(title, TitleMax) is { Length: > 0 } t)
        {
            await store.EditCardAsync(id, t, Clean(notes, NotesMax), CleanSubteam(subteam), Clean(owner, OwnerMax), due);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMoveAsync(int id, CardStatus to)
    {
        if (Enum.IsDefined(to)) await store.MoveCardAsync(id, to);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostProgressAsync(int id, int progress)
    {
        await store.SetProgressAsync(id, progress);
        return RedirectToPage();
    }

    /// <summary>"Nov 5" this year, "Nov 5, 2027" otherwise.</summary>
    public static string ShortDate(DateOnly d) =>
        d.ToString(d.Year == EtsStore.Today.Year ? "MMM d" : "MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await store.DeleteCardAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/ets/login");
    }

    private static string Clean(string? text, int max)
    {
        var s = (text ?? "").Trim();
        return s.Length > max ? s[..max] : s;
    }

    private static string CleanSubteam(string? subteam) =>
        EtsStore.Subteams.Contains(subteam) ? subteam! : "";
}
