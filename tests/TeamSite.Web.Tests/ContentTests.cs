using TeamSite.Web.Data;

namespace TeamSite.Web.Tests;

/// <summary>Checks on Data/TeamInfo.cs that catch typos before they reach the website.</summary>
public class ContentTests
{
    private static readonly string WwwRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "TeamSite.Web", "wwwroot"));

    [Fact]
    public void Logo_and_photo_files_exist()
    {
        var team = TeamInfo.Team;
        foreach (var image in new[] { team.Logo, team.Photo })
        {
            if (image is null) continue;
            var file = Path.Combine(WwwRoot, image.TrimStart('/'));
            Assert.True(File.Exists(file), $"{image} is in TeamInfo.cs but there is no file at {file}");
        }
    }

    [Fact]
    public void Program_is_FTC_or_FRC()
    {
        Assert.Contains(TeamInfo.Team.Program, new[] { "FTC", "FRC" });
    }

    [Fact]
    public void Accent_color_is_a_hex_color()
    {
        Assert.Matches("^#[0-9a-fA-F]{6}$", TeamInfo.Team.AccentColor);
    }

    [Fact]
    public void Social_links_are_full_web_addresses()
    {
        Assert.All(TeamInfo.Team.Socials, s => Assert.StartsWith("https://", s.Url));
    }
}
