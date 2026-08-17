using WeightTracker.Models;

namespace WeightTracker.Services;

public static class GamificationEngine
{
    public static GamificationInfo Build(IReadOnlyList<WeightEntry> entries, double? goalKg, DateOnly today)
    {
        var ordered = entries.OrderBy(e => e.Date).ToList();
        var wantsDecline = WeightDataService.WantsDecline(ordered, goalKg);

        var longestStreak = LongestStreak(ordered, wantsDecline);
        var bestPercent = BestProgressPercent(ordered, goalKg);
        var comeback = ordered.Count >= 4 && HasComeback(ordered, wantsDecline);

        var badges = BuildBadges(ordered.Count, longestStreak, bestPercent, comeback);

        var baseXp = ordered.Count * 10;
        var streakXp = longestStreak * 15;
        var milestoneXp = MilestoneXp(bestPercent);
        var badgeXp = badges.Count(b => b.Unlocked) * 25;
        var totalXp = baseXp + streakXp + milestoneXp + badgeXp;

        var (level, xpIntoLevel, xpForNextLevel, rank) = LevelFor(totalXp);

        return new GamificationInfo
        {
            TotalXp = totalXp,
            Level = level,
            XpIntoLevel = xpIntoLevel,
            XpForNextLevel = xpForNextLevel,
            RankTitle = rank,
            Badges = badges,
            WeeklyQuest = BuildWeeklyQuest(ordered, today)
        };
    }

    private static bool IsImproving(double previous, double current, bool wantsDecline)
    {
        var delta = current - previous;
        return wantsDecline ? delta < -0.001 : delta > 0.001;
    }

    private static int LongestStreak(List<WeightEntry> ordered, bool wantsDecline)
    {
        var longest = 0;
        var current = 0;

        for (var i = 1; i < ordered.Count; i++)
        {
            if (IsImproving(ordered[i - 1].WeightKg, ordered[i].WeightKg, wantsDecline))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }

        return longest;
    }

    private static bool HasComeback(List<WeightEntry> ordered, bool wantsDecline)
    {
        for (var i = 1; i < ordered.Count - 2; i++)
        {
            var badStep = !IsImproving(ordered[i - 1].WeightKg, ordered[i].WeightKg, wantsDecline);
            var recovered1 = IsImproving(ordered[i].WeightKg, ordered[i + 1].WeightKg, wantsDecline);
            var recovered2 = IsImproving(ordered[i + 1].WeightKg, ordered[i + 2].WeightKg, wantsDecline);

            if (badStep && recovered1 && recovered2)
            {
                return true;
            }
        }

        return false;
    }

    private static double? BestProgressPercent(List<WeightEntry> ordered, double? goalKg)
    {
        if (goalKg is null || ordered.Count == 0)
        {
            return null;
        }

        var startWeight = ordered[0].WeightKg;
        var totalToLose = startWeight - goalKg.Value;

        if (Math.Abs(totalToLose) < 0.001)
        {
            return null;
        }

        var best = double.MinValue;
        foreach (var entry in ordered)
        {
            var progressed = startWeight - entry.WeightKg;
            var pct = Math.Clamp(progressed / totalToLose * 100, 0, 100);
            best = Math.Max(best, pct);
        }

        return best;
    }

    private static int MilestoneXp(double? bestPercent)
    {
        if (bestPercent is null)
        {
            return 0;
        }

        var xp = 0;
        if (bestPercent >= 25) xp += 50;
        if (bestPercent >= 50) xp += 100;
        if (bestPercent >= 75) xp += 150;
        if (bestPercent >= 100) xp += 250;
        return xp;
    }

    private static (int Level, int XpIntoLevel, int XpForNextLevel, string Rank) LevelFor(int totalXp)
    {
        int Threshold(int n) => 25 * n * n;

        var level = 1;
        while (Threshold(level) <= totalXp)
        {
            level++;
        }

        var xpIntoLevel = totalXp - Threshold(level - 1);
        var xpForNextLevel = Threshold(level) - Threshold(level - 1);

        var rank = level switch
        {
            <= 2 => "Brons",
            <= 5 => "Zilver",
            <= 9 => "Goud",
            _ => "Platina"
        };

        return (level, xpIntoLevel, xpForNextLevel, rank);
    }

    public static List<BadgeInfo> AllBadgeDefinitions() => BuildBadges(0, 0, null, false);

    private static List<BadgeInfo> BuildBadges(int entryCount, int longestStreak, double? bestPercent, bool comeback)
    {
        bool AtLeast(double? pct, double threshold) => pct is not null && pct >= threshold;

        return new List<BadgeInfo>
        {
            new() { Id = "first_step", Emoji = "👣", Title = "Eerste stap", Description = "Log je eerste weging", Unlocked = entryCount >= 1 },
            new() { Id = "loyal10", Emoji = "📆", Title = "Trouwe weger", Description = "10 wegingen gelogd", Unlocked = entryCount >= 10 },
            new() { Id = "loyal30", Emoji = "🗓️", Title = "Volhouder", Description = "30 wegingen gelogd", Unlocked = entryCount >= 30 },
            new() { Id = "streak3", Emoji = "🔥", Title = "Op dreef", Description = "3 metingen op rij vooruitgang", Unlocked = longestStreak >= 3 },
            new() { Id = "streak7", Emoji = "⚡", Title = "Onstopbaar", Description = "7 metingen op rij vooruitgang", Unlocked = longestStreak >= 7 },
            new() { Id = "comeback", Emoji = "💪", Title = "Comeback", Description = "Weer op koers na een tegenslag", Unlocked = comeback },
            new() { Id = "quarter", Emoji = "🎯", Title = "Kwart op weg", Description = "25% van je doel bereikt", Unlocked = AtLeast(bestPercent, 25) },
            new() { Id = "half", Emoji = "🏅", Title = "Halverwege", Description = "50% van je doel bereikt", Unlocked = AtLeast(bestPercent, 50) },
            new() { Id = "near", Emoji = "🚀", Title = "Bijna er", Description = "75% van je doel bereikt", Unlocked = AtLeast(bestPercent, 75) },
            new() { Id = "goal", Emoji = "🏆", Title = "Doel bereikt", Description = "100% van je doel bereikt", Unlocked = AtLeast(bestPercent, 100) }
        };
    }

    private static QuestInfo BuildWeeklyQuest(List<WeightEntry> ordered, DateOnly today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysSinceMonday);

        var countThisWeek = ordered.Count(e => e.Date >= weekStart && e.Date <= today);

        return new QuestInfo
        {
            Title = "Weeg 3x deze week",
            Progress = Math.Min(countThisWeek, 3),
            Target = 3
        };
    }
}
