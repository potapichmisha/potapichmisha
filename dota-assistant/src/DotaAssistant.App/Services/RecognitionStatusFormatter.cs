using System.Text;
using DotaAssistant.Vision.Models;

namespace DotaAssistant.App.Services;

/// <summary>
/// Russian labels for the ordered recognition status panel.
/// </summary>
public static class RecognitionStatusFormatter
{
    public static string FormatHero(MatchCandidate? hero)
        => FormatSingle("Ваш герой", hero);

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

    public static string FormatConfidence(RecognitionResult result)
    {
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
