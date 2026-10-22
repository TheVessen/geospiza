#nullable enable
using System.Linq;
using GeospizaCore.Advisor;
using Newtonsoft.Json.Linq;

namespace GeospizaPlugin.AI;

/// <summary>
///     Turns <see cref="RunSignals" /> into named categories for a model. Decision models judge words far
///     better than raw numbers (TypeSafe: "keep the arithmetic in code"), so every threshold lives here.
/// </summary>
public static class RunStateDescriber
{
    public static JObject Describe(RunSignals s)
    {
        return new JObject
        {
            ["optimisation"] = s.IsMultiObjective
                ? "multiple objectives: grow a set of best trade-off designs"
                : "single objective: maximise one fitness value",
            ["run_phase"] = s.Progress < 1.0 / 3 ? "early" : s.Progress < 2.0 / 3 ? "middle" : "late",
            ["generation"] = $"{s.Generation} of {s.MaxGenerations}",
            ["progress"] = Progress(s.GenerationsWithoutImprovement, s.IsStagnating),
            ["population_variety"] = Variety(s.GenotypicDiversity),
            ["duplicate_designs"] = Duplicates(1.0 - s.UniqueFraction),
            ["exploration_level"] = s.MutationBoost < 1.05 ? "at the scheduled level"
                : s.MutationBoost < 2.0 ? "raised"
                : "strongly raised",
            ["room_to_raise_exploration"] = Room(s.MutationBoost, s.MaxMutationBoost),
            ["recent_decisions"] = new JArray(s.RecentDecisions.Select(r =>
                $"generation {r.Generation}: {Describe(r.Pressure)}" + (r.InjectImmigrants ? ", added random designs" : "")))
        };
    }

    public static string Describe(ExplorationPressure pressure) => pressure switch
    {
        ExplorationPressure.Raise => "explore more",
        ExplorationPressure.Relax => "refine",
        _ => "keep"
    };

    private static string Progress(int generationsWithoutImprovement, bool stagnating)
    {
        if (generationsWithoutImprovement == 0) return "improving: the latest generation set a new best";
        var phrase = $"no improvement for {generationsWithoutImprovement} generation" +
                     (generationsWithoutImprovement == 1 ? "" : "s");
        return stagnating ? $"stalled: {phrase}" : $"slowing: {phrase}";
    }

    // A uniformly random population scores about 0.33 (mean normalized distance between two random ticks).
    private static string Variety(double? genotypicDiversity) => genotypicDiversity switch
    {
        null => "unknown",
        < 0.10 => "collapsed: designs are nearly identical",
        < 0.15 => "low",
        < 0.25 => "moderate",
        _ => "high"
    };

    private static string Duplicates(double duplicateFraction) => duplicateFraction switch
    {
        < 0.1 => "almost none",
        < 0.4 => "some",
        < 0.7 => "many",
        _ => "most designs are duplicates"
    };

    private static string Room(double boost, double maxBoost)
    {
        var headroom = maxBoost / boost;
        return headroom >= 2.0 ? "plenty" : headroom >= 1.2 ? "some" : "none";
    }
}
