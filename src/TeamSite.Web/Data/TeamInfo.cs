using TeamSite.Web.Models;

namespace TeamSite.Web.Data;

/// <summary>
/// Everything your team's site says. To change the site, edit this file.
/// Images go in wwwroot/images/.
/// </summary>
public static class TeamInfo
{
    public static readonly Team Team = new()
    {
        Name = "Prime Suspects",
        ShortName = "Prime Suspects",
        Program = "FTC",
        Number = 38350,
        Tagline = "Pleasant Valley's newest FTC team, on the case.",
        Logo = "/images/prime-suspects-logo.jpg",
        LogoBackground = "#000000",
        AccentColor = "#3b82f6",
        Story =
        [
            "Prime Suspects is FTC Team #38350, the newest FIRST Tech Challenge team at Pleasant Valley High School. Like our other teams, Prime Suspects students design, build and program a competition robot while learning teamwork, problem solving and communication along the way.",
            "More about the team, its members' goals and its outreach is coming soon.",
        ],
    };

    // The menu at the top of every page. Add a line when you add a page.
    public static readonly List<(string Label, string Href)> Nav =
    [
        ("Home", "/"),
    ];

    public const string PvSite = "https://www.firstpvrobotics.com";
}
