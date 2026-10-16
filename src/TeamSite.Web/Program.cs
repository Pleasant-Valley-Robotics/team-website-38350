// Program.cs is where the website starts. It sets up the services the site uses,
// then lists, in order, what happens to every request that comes in.

using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// MVC: Controllers pick the data, Views turn it into HTML.
builder.Services.AddControllersWithViews();

// Razor Pages: a .cshtml file in Pages/ is a page of its own, its address from its
// file name (Pages/Robot.cshtml is /robot). Both kinds of page work side by side.
builder.Services.AddRazorPages();

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
    // A crash shows a friendly page instead of the error details.
    app.UseExceptionHandler("/error");
}

// A page that doesn't exist shows our 404 page (ErrorsController.NotFoundPage).
app.UseStatusCodePagesWithReExecute("/not-found");

// Files in wwwroot/ (CSS, images, fonts) are sent as they are.
app.UseStaticFiles();

app.UseRouting();

app.MapHealthChecks("/healthz");

// Every page's address is written on its controller action with [Route("...")].
app.MapControllers();

// Pages/Robot.cshtml is "/robot", and so on.
app.MapRazorPages();

app.Run();

// Lets the tests in tests/TeamSite.Web.Tests start the site in memory.
public partial class Program { }
