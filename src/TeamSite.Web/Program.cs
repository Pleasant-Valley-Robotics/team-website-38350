// Program.cs is where the website starts. You don't need to change it to change
// the words on a page or to add a page: that is all done in the Pages/ folder.

using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using TeamSite.Web.Ets;

var builder = WebApplication.CreateBuilder(args);

// ETS, the private scrum board, keeps its cards and password in the team's database.
builder.Services.AddSingleton<EtsStore>();

// "dotnet TeamSite.Web.dll ets-password" sets the ETS password instead of starting the
// site. A mentor runs it on the robotics server; see README.md.
if (args.FirstOrDefault() == "ets-password")
{
    await EtsPasswordCommand.RunAsync(new EtsStore(builder.Configuration));
    return;
}

// Razor Pages: every .cshtml file in Pages/ is a page of the site.
// Every page in Pages/Ets/ needs the ETS password, except the page that asks for it.
// Addresses the site builds (like the board's buttons) are lowercase: /ets, not /Ets.
// The ETS sign-in cookie only goes to /ets, and browsers treat the two as different.
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Ets");
    options.Conventions.AllowAnonymousToPage("/Ets/Login");
});

// Signing in to ETS: a cookie, only sent to /ets pages, that lasts two weeks.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ets";
        options.Cookie.Path = "/ets";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.LoginPath = "/ets/login";
        options.LogoutPath = "/ets/login";
        // When a mentor changes the password, everyone signed in with the old one is signed out.
        options.Events.OnValidatePrincipal = async context =>
        {
            var store = context.HttpContext.RequestServices.GetRequiredService<EtsStore>();
            var stamp = context.Principal?.FindFirstValue("ets_stamp");
            if (stamp is null || stamp != await store.CurrentStampAsync())
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });
builder.Services.AddAuthorization();

// Sign-in cookies are protected with keys. On the robotics server the keys are kept in
// a volume (compose.yaml), so a new release doesn't sign everyone out.
if (builder.Configuration["KEYS_DIR"] is { Length: > 0 } keysDir)
{
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysDir));
}

// At most 10 password tries a minute, so nobody can guess the password by trying many.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Redirect("/ets/login?wait=true");
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("ets-login", context =>
        HttpMethods.IsPost(context.Request.Method)
            ? RateLimitPartition.GetFixedWindowLimiter("ets-login", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            })
            : RateLimitPartition.GetNoLimiter("ets-login-get"));
});

// /healthz answers "Healthy" when the site is running. The robotics server asks it
// before it switches visitors to a new release.
builder.Services.AddHealthChecks();

// On the robotics server, visitors reach us through a proxy that tells us they used
// https with the X-Forwarded-Proto header. Trust it so links we build use https.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    // A crash shows a friendly page (Pages/Error.cshtml) instead of the error details.
    app.UseExceptionHandler("/error");
}

// A page that doesn't exist shows our 404 page (Pages/NotFound.cshtml).
app.UseStatusCodePagesWithReExecute("/not-found");

// Files in wwwroot/ (CSS, images, fonts) are sent as they are.
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

// Pages/Index.cshtml is "/", Pages/Robot.cshtml is "/robot", and so on.
app.MapRazorPages();

app.Run();

// Lets the tests in tests/TeamSite.Web.Tests start the site in memory.
public partial class Program { }
