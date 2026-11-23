using DotaAssistant.Tips.Models;
using DotaAssistant.Tips.Rules;

namespace DotaAssistant.Tips.Engine;

/// <summary>
/// Rule-based mid tip engine: next 1–3 items + matchup aggression phrase.
/// Template Russian strings only — no LLM.
/// </summary>
public sealed class TipEngine
{
    public const int DefaultMaxNextItems = 3;

    private readonly TipRulesStore _store;
    private readonly int _maxNextItems;

    public TipEngine(TipRulesStore store, int maxNextItems = DefaultMaxNextItems)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _maxNextItems = Math.Clamp(maxNextItems, 1, 3);
    }

    public TipRulesStore Store => _store;

    /// <summary>
    /// Builds advice for the given mid state.
    /// </summary>
    public TipAdvice Advise(TipContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.HeroId))
        {
            throw new ArgumentException("HeroId обязателен.", nameof(context));
        }

        var level = Math.Clamp(context.Level <= 0 ? 1 : context.Level, 1, 30);
        var owned = new HashSet<string>(
            context.OwnedItemIds.Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);

        var nextItems = BuildNextItems(context.HeroId, owned, out var buildStatus);
        var (matchup, matchupStatus) = BuildMatchup(context.HeroId, context.OpponentId, level);

        string? demoContext = null;
        if (!string.IsNullOrWhiteSpace(context.OpponentId))
        {
            demoContext = Format(
                _store.Phrase("demo_context", "Демо mid: {hero} против {opponent}, уровень {level}."),
                ("hero", _store.HeroNameRu(context.HeroId)),
                ("opponent", _store.HeroNameRu(context.OpponentId)),
                ("level", level.ToString()));
        }

        return new TipAdvice
        {
            NextItems = nextItems,
            Matchup = matchup,
            BuildStatusRu = buildStatus,
            MatchupStatusRu = matchupStatus,
            DemoContextRu = demoContext
        };
    }

    /// <summary>
    /// Applies demo opponent/level from rules when recognition does not provide them.
    /// </summary>
    public TipContext EnrichWithDemoDefaults(TipContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var demo = _store.Document.Demo;
        if (demo is null)
        {
            return context;
        }

        return new TipContext
        {
            HeroId = context.HeroId,
            OpponentId = string.IsNullOrWhiteSpace(context.OpponentId) ? demo.OpponentId : context.OpponentId,
            Level = context.Level > 0 ? context.Level : Math.Max(1, demo.Level),
            OwnedItemIds = context.OwnedItemIds,
            SideId = context.SideId
        };
    }

    private IReadOnlyList<NextItemTip> BuildNextItems(
        string heroId,
        HashSet<string> owned,
        out string? buildStatus)
    {
        var build = _store.TryGetBuild(heroId);
        if (build is null)
        {
            buildStatus = Format(
                _store.Phrase("unknown_build", "Нет mid-сборки для героя «{hero}»."),
                ("hero", _store.HeroNameRu(heroId)));
            return Array.Empty<NextItemTip>();
        }

        var tips = new List<NextItemTip>(_maxNextItems);
        foreach (var itemId in build.Core)
        {
            if (owned.Contains(itemId))
            {
                continue;
            }

            var item = _store.TryGetItem(itemId);
            var nameRu = item?.NameRu ?? itemId;
            var whyRu = item?.WhyRu ?? "";
            var phrase = Format(
                _store.Phrase("next_item", "{item} — {why}"),
                ("item", nameRu),
                ("why", whyRu));

            tips.Add(new NextItemTip
            {
                ItemId = itemId,
                NameRu = nameRu,
                WhyRu = whyRu,
                PhraseRu = phrase
            });

            if (tips.Count >= _maxNextItems)
            {
                break;
            }
        }

        buildStatus = tips.Count == 0
            ? _store.Phrase("build_complete", "Ключевые предметы из mid-сборки уже куплены.")
            : null;
        return tips;
    }

    private (MatchupTip? Tip, string? Status) BuildMatchup(string heroId, string? opponentId, int level)
    {
        if (string.IsNullOrWhiteSpace(opponentId))
        {
            return (null, _store.Phrase("need_opponent", "Оппонент mid неизвестен."));
        }

        var rule = _store.TryGetMatchup(heroId, opponentId, level);
        if (rule is null)
        {
            var status = Format(
                _store.Phrase("unknown_matchup", "Нет правила mid-матчапа: {hero} против {opponent}."),
                ("hero", _store.HeroNameRu(heroId)),
                ("opponent", _store.HeroNameRu(opponentId)));
            return (null, status);
        }

        var aggression = rule.Aggression.Trim().ToLowerInvariant();
        var template = _store.Phrase(aggression);
        if (string.IsNullOrWhiteSpace(template))
        {
            template = _store.Phrase("farm", "Фарми против {opponent} на {level} уровне.");
        }

        var phrase = Format(
            template,
            ("hero", _store.HeroNameRu(heroId)),
            ("opponent", _store.HeroNameRu(opponentId)),
            ("level", level.ToString()));

        return (new MatchupTip
        {
            Aggression = aggression,
            PhraseRu = phrase,
            OpponentId = opponentId,
            OpponentNameRu = _store.HeroNameRu(opponentId)
        }, null);
    }

    private static string Format(string template, params (string Key, string Value)[] pairs)
    {
        var result = template;
        foreach (var (key, value) in pairs)
        {
            result = result.Replace("{" + key + "}", value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
