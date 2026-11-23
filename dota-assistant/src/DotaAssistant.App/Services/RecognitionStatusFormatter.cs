using System.Text;
using DotaAssistant.Vision.Models;
using DotaAssistant.Vision.Recognition;

namespace DotaAssistant.App.Services;

/// <summary>
/// Russian labels for the ordered recognition status panel.
/// </summary>
public static class RecognitionStatusFormatter
{
    public static string FormatHero(MatchCandidate? hero)
        => FormatSingle("Ваш герой", hero);

    public static string FormatEffectiveHero(EffectivePlayerIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(identity.HeroId))
        {
            return $"Ваш герой: — ({identity.HeroModeRu})";
        }

        var name = string.IsNullOrWhiteSpace(identity.HeroDisplayNameRu)
            ? identity.HeroId
            : identity.HeroDisplayNameRu;

        if (identity.HeroIsManual)
        {
            return $"Ваш герой: {name} ({identity.HeroModeRu})";
        }

        if (identity.HeroConfidence is { } conf)
        {
            if (identity.HasUsableHero)
            {
                return $"Ваш герой: {name} ({conf:P0}, {identity.HeroModeRu})";
            }

            return $"Ваш герой: не уверен — {name} ({conf:P0}, {identity.HeroModeRu})";
        }

        return $"Ваш герой: {name} ({identity.HeroModeRu})";
    }

    public static string FormatSide(EffectivePlayerIdentity identity)
    {
        if (identity.Side is null)
        {
            return $"Сторона: не определена ({identity.SideModeRu})";
        }

        var sideRu = FormatSideName(identity.Side.Value);
        return $"Сторона: {sideRu} ({identity.SideModeRu})";
    }

    public static string FormatSideName(PlayerSide side) => side switch
    {
        PlayerSide.Radiant => "Radiant",
        PlayerSide.Dire => "Dire",
        _ => side.ToString()
    };

    public static string FormatAllies(IReadOnlyList<MatchCandidate> allies)
        => FormatList("Союзники", allies);

    public static string FormatEnemies(IReadOnlyList<MatchCandidate> enemies)
        => FormatList("Противники", enemies);

    public static string FormatItems(IReadOnlyList<MatchCandidate> items)
    {
        if (items.Count == 0)
        {
            return "Предметы: —";
        }

        var parts = items.Select(FormatCandidateShort);
        return "Предметы: " + string.Join(", ", parts);
    }

    public static string FormatConfidence(RecognitionResult result, EffectivePlayerIdentity? identity = null)
    {
        if (identity is { HeroIsManual: true })
        {
            var autoNote = result.Hero is null
                ? "авто: нет совпадений"
                : $"авто угадало: {result.Hero.DisplayNameRu} ({result.Hero.Confidence:P0})";
            return $"Уверенность: герой задан вручную; {autoNote}.";
        }

        if (result.Hero is null)
        {
            return "Уверенность: нет совпадений (топ-бар пуст или шаблоны не подошли).";
        }

        if (!result.Hero.IsConfident)
        {
            return $"Уверенность: не уверен — {result.Hero.DisplayNameRu} {result.Hero.Confidence:P0} (порог {result.MinConfidence:P0}).";
        }

        var confItems = result.InventoryItems.Count(i => i.IsConfident);
        var lowItems = result.InventoryItems.Count(i => !i.IsConfident);
        var alliesOk = result.Allies.Count(a => a.IsConfident);
        var enemiesOk = result.Enemies.Count(e => e.IsConfident);
        return $"Уверенность: герой {result.Hero.Confidence:P0}; союзники {alliesOk}/{result.Allies.Count}; противники {enemiesOk}/{result.Enemies.Count}; предметы {confItems} ок" +
               (lowItems > 0 ? $", {lowItems} не уверен" : "") +
               ".";
    }

    public static string FormatCandidateShort(MatchCandidate c)
    {
        if (c.IsConfident)
        {
            return $"{c.DisplayNameRu} ({c.Confidence:P0})";
        }

        return $"не уверен: {c.DisplayNameRu} ({c.Confidence:P0})";
    }

    private static string FormatSingle(string title, MatchCandidate? c)
    {
        if (c is null)
        {
            return $"{title}: —";
        }

        if (c.IsConfident)
        {
            return $"{title}: {c.DisplayNameRu} ({c.Confidence:P0})";
        }

        return $"{title}: не уверен — {c.DisplayNameRu} ({c.Confidence:P0})";
    }

    private static string FormatList(string title, IReadOnlyList<MatchCandidate> items)
    {
        if (items.Count == 0)
        {
            return $"{title}: —";
        }

        var sb = new StringBuilder();
        sb.Append(title).Append(": ");
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(FormatCandidateShort(items[i]));
        }

        return sb.ToString();
    }
}
