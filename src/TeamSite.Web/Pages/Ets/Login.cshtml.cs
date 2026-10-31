// The ETS password page: /ets/login. You don't need to change this file.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TeamSite.Web.Ets;

namespace TeamSite.Web.Pages.Ets;

[EnableRateLimiting("ets-login")]
public class LoginModel(EtsStore store, ILogger<LoginModel> log) : PageModel
{
    [BindProperty] public string Password { get; set; } = "";

    public bool HasDatabase { get; private set; }
    public bool DatabaseDown { get; private set; }
    public bool HasPassword { get; private set; }
    public bool Wait { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(bool wait = false)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/ets");
        Wait = wait;
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        if (!HasPassword) return Page();

        var stamp = await store.CheckPasswordAsync(Password);
        if (stamp is null)
        {
            Error = "That password isn't right.";
            return Page();
        }

        var identity = new ClaimsIdentity([new Claim("ets_stamp", stamp)], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
        return Redirect("/ets");
    }

    private async Task LoadAsync()
    {
        HasDatabase = store.HasDatabase;
        if (!HasDatabase) return;
        try
        {
            HasPassword = await store.HasPasswordAsync();
        }
        catch (Exception e) when (e is Npgsql.NpgsqlException or System.Net.Sockets.SocketException or TimeoutException)
        {
            log.LogWarning(e, "ETS couldn't reach its database.");
            DatabaseDown = true;
        }
    }
}
