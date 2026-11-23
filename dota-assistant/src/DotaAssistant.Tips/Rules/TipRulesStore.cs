using System.Text.Json;

namespace DotaAssistant.Tips.Rules;

/// <summary>
/// Loads and caches local tip rules JSON (no network).
/// </summary>
public sealed class TipRulesStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public TipRulesDocument Document { get; }

    public TipRulesStore(TipRulesDocument document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        NormalizeDictionaries(Document);
    }

    public static TipRulesStore LoadFromFile(string rulesJsonPath)
    {
        if (!File.Exists(rulesJsonPath))
        {
            throw new FileNotFoundException("Не найден файл правил советов.", rulesJsonPath);
        }

        var json = File.ReadAllText(rulesJsonPath);
        var doc = JsonSerializer.Deserialize<TipRulesDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("rules.json пуст или некорректен.");

        if (!string.Equals(doc.Lane, "mid", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"V1-alpha поддерживает только mid, в файле lane={doc.Lane}.");
        }

        return new TipRulesStore(doc);
    }

    public static TipRulesStore LoadFromTipsDirectory(string tipsDirectory)
    {
        var path = Path.Combine(tipsDirectory, "rules.json");
        return LoadFromFile(path);
    }

    public string HeroNameRu(string heroId)
    {
        if (Document.HeroNamesRu.TryGetValue(heroId, out var name) && !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return heroId;
    }

    public TipItemDto? TryGetItem(string itemId)
    {
        return Document.Items.TryGetValue(itemId, out var item) ? item : null;
    }

    public TipBuildDto? TryGetBuild(string heroId)
    {
        return Document.Builds.FirstOrDefault(b =>
            string.Equals(b.HeroId, heroId, StringComparison.OrdinalIgnoreCase));
    }

    public TipMatchupDto? TryGetMatchup(string heroId, string opponentId, int level)
    {
        var clamped = Math.Clamp(level, 1, 30);
        return Document.Matchups.FirstOrDefault(m =>
            string.Equals(m.HeroId, heroId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.OpponentId, opponentId, StringComparison.OrdinalIgnoreCase)
            && clamped >= m.LevelMin
            && clamped <= m.LevelMax);
    }

    public string Phrase(string key, string fallback = "")
    {
        return Document.Phrases.TryGetValue(key, out var phrase) && !string.IsNullOrWhiteSpace(phrase)
            ? phrase
            : fallback;
    }

    private static void NormalizeDictionaries(TipRulesDocument doc)
    {
        doc.Items = new Dictionary<string, TipItemDto>(doc.Items, StringComparer.OrdinalIgnoreCase);
        doc.Phrases = new Dictionary<string, string>(doc.Phrases, StringComparer.OrdinalIgnoreCase);
        doc.HeroNamesRu = new Dictionary<string, string>(doc.HeroNamesRu, StringComparer.OrdinalIgnoreCase);
    }
}
