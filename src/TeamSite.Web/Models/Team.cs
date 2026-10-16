namespace TeamSite.Web.Models;

/// <summary>Everything about the team that the site shows. Filled in by Data/TeamInfo.cs.</summary>
public class Team
{
    public required string Name { get; init; }

    /// <summary>A short name for headings, like "Radical".</summary>
    public required string ShortName { get; init; }

    /// <summary>"FTC" or "FRC".</summary>
    public required string Program { get; init; }

    public required int Number { get; init; }

    /// <summary>The year the team started, like "2013". Leave it null if you're not sure.</summary>
    public string? Founded { get; init; }

    public required string Tagline { get; init; }

    /// <summary>Path to the logo inside wwwroot, like "/images/radical-logo.png".</summary>
    public required string Logo { get; init; }

    /// <summary>Color behind the logo. White suits most logos; use black for logos drawn on black.</summary>
    public string LogoBackground { get; init; } = "#ffffff";

    /// <summary>The team's color, used for borders and highlights.</summary>
    public required string AccentColor { get; init; }

    /// <summary>A team photo inside wwwroot, or null for none.</summary>
    public string? Photo { get; init; }

    /// <summary>Describes the photo for people who can't see it. Also shown as its caption.</summary>
    public string? PhotoCaption { get; init; }

    public string? Email { get; init; }

    public List<SocialLink> Socials { get; init; } = [];

    /// <summary>Short facts shown as bubbles under the tagline.</summary>
    public List<string> Highlights { get; init; } = [];

    /// <summary>The team's story, one string per paragraph.</summary>
    public List<string> Story { get; init; } = [];

    /// <summary>Extra groups of cards, like "Departments".</summary>
    public List<CardSection> Sections { get; init; } = [];

    /// <summary>The team's outreach projects, one card each.</summary>
    public List<Card> Outreach { get; init; } = [];
}

public record SocialLink(string Label, string Url);

public record Card(string Title, string Body);

public record CardSection(string Title, List<Card> Cards);
