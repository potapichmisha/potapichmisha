using System.Text.Json.Serialization;

namespace DotaAssistant.Tips.Rules;

/// <summary>
/// Deserialized shape of assets/tips/rules.json.
/// </summary>
public sealed class TipRulesDocument
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("lane")]
    public string Lane { get; set; } = "mid";

    [JsonPropertyName("items")]
    public Dictionary<string, TipItemDto> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("builds")]
    public List<TipBuildDto> Builds { get; set; } = new();

    [JsonPropertyName("matchups")]
    public List<TipMatchupDto> Matchups { get; set; } = new();

    [JsonPropertyName("phrases")]
    public Dictionary<string, string> Phrases { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("hero_names_ru")]
    public Dictionary<string, string> HeroNamesRu { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("demo")]
    public TipDemoDto? Demo { get; set; }
}

public sealed class TipItemDto
{
    [JsonPropertyName("name_ru")]
    public string NameRu { get; set; } = "";

    [JsonPropertyName("why_ru")]
    public string WhyRu { get; set; } = "";
}

public sealed class TipBuildDto
{
    [JsonPropertyName("hero_id")]
    public string HeroId { get; set; } = "";

    [JsonPropertyName("name_ru")]
    public string NameRu { get; set; } = "";

    [JsonPropertyName("core")]
    public List<string> Core { get; set; } = new();
}

public sealed class TipMatchupDto
{
    [JsonPropertyName("hero_id")]
    public string HeroId { get; set; } = "";

    [JsonPropertyName("opponent_id")]
    public string OpponentId { get; set; } = "";

    [JsonPropertyName("level_min")]
    public int LevelMin { get; set; } = 1;

    [JsonPropertyName("level_max")]
    public int LevelMax { get; set; } = 30;

    [JsonPropertyName("aggression")]
    public string Aggression { get; set; } = "farm";
}

public sealed class TipDemoDto
{
    [JsonPropertyName("opponent_id")]
    public string OpponentId { get; set; } = "";

    [JsonPropertyName("level")]
    public int Level { get; set; } = 5;

    [JsonPropertyName("note_ru")]
    public string NoteRu { get; set; } = "";
}
