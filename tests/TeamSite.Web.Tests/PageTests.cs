using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TeamSite.Web.Data;

namespace TeamSite.Web.Tests;

/// <summary>
/// These tests start the whole website in memory and visit its pages, the way a
/// browser would. GitHub runs them on every pull request; if one fails, the pull
/// request can't be merged until it's fixed.
/// Run them yourself with:  dotnet test
/// </summary>
public class PageTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Home_page_shows_the_team()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Razor writes some characters as HTML codes (' becomes &#x27;), so decode before comparing.
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(TeamInfo.Team.Name, html);
        Assert.Contains($"#{TeamInfo.Team.Number}", html);
    }

    [Fact]
    public async Task Missing_page_shows_the_404_page()
    {
        var response = await _client.GetAsync("/this-page-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("drove off the field", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_check_answers()
    {
        // The robotics server asks this before a release counts.
        var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Every_menu_link_works()
    {
        foreach (var (label, href) in TeamInfo.Nav)
        {
            var response = await _client.GetAsync(href);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"The menu link \"{label}\" goes to {href}, which answered {(int)response.StatusCode}");
        }
    }

    [Fact]
    public void Pages_folder_is_switched_on()
    {
        // A .cshtml file in Pages/ only becomes a page if Program.cs calls MapRazorPages().
        // There is no page in Pages/ yet, so this checks the switch itself is there.
        var endpoints = (CompositeEndpointDataSource)factory.Services.GetRequiredService<EndpointDataSource>();
        Assert.Contains(endpoints.DataSources, d => d.GetType().Name == "PageActionEndpointDataSource");
    }
}
