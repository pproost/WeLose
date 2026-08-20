using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;
using WeightTracker.Models;

namespace WeightTracker.Services;

public class WeightDataService
{
    private const string StorageKey = "weighttracker.entries";
    private const string GoalStorageKey = "weighttracker.goal";

    private readonly IJSRuntime _js;
    private List<WeightEntry>? _cache;
    private double? _goalCache;
    private bool _goalLoaded;

    public WeightDataService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<List<WeightEntry>> GetEntriesAsync()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        _cache = string.IsNullOrWhiteSpace(json)
            ? new List<WeightEntry>()
            : JsonSerializer.Deserialize<List<WeightEntry>>(json) ?? new List<WeightEntry>();

        return _cache.OrderBy(e => e.Date).ToList();
    }

    public async Task AddEntryAsync(WeightEntry entry)
    {
        var entries = await GetEntriesAsync();

        var existing = entries.FirstOrDefault(e => e.Date == entry.Date);
        if (existing is not null)
        {
            existing.WeightKg = entry.WeightKg;
        }
        else
        {
            entries.Add(entry);
        }

        await SaveAsync(entries);
    }

    public async Task DeleteEntryAsync(DateOnly date)
    {
        var entries = await GetEntriesAsync();
        entries.RemoveAll(e => e.Date == date);
        await SaveAsync(entries);
    }

    public async Task ClearAllEntriesAsync()
    {
        _cache = new List<WeightEntry>();
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    public async Task<double?> GetGoalAsync()
    {
        if (_goalLoaded)
        {
            return _goalCache;
        }

        var raw = await _js.InvokeAsync<string?>("localStorage.getItem", GoalStorageKey);
        _goalCache = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var goal)
            ? goal
            : null;
        _goalLoaded = true;

        return _goalCache;
    }

    public async Task SetGoalAsync(double? goalKg)
    {
        _goalCache = goalKg;
        _goalLoaded = true;

        if (goalKg is null)
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", GoalStorageKey);
        }
        else
        {
            await _js.InvokeVoidAsync("localStorage.setItem", GoalStorageKey, goalKg.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private async Task SaveAsync(List<WeightEntry> entries)
    {
        _cache = entries.OrderBy(e => e.Date).ToList();
        var json = JsonSerializer.Serialize(_cache);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public static TrendInfo ComputeTrend(IReadOnlyList<WeightEntry> entries, double? goalKg)
    {
        var ordered = entries.OrderBy(e => e.Date).ToList();
        var info = new TrendInfo();

        info.LastVsPrevious = ordered.Count < 2
            ? TrendDirection.Unknown
            : DirectionOf(ordered[^1].WeightKg - ordered[^2].WeightKg);

        var window = ordered.TakeLast(10).ToList();
        info.Overall = window.Count < 2
            ? TrendDirection.Unknown
            : DirectionOf(LinearRegressionSlope(window));

        var wantsDecline = WantsDecline(ordered, goalKg);

        info.LastVsPreviousGood = IsGood(info.LastVsPrevious, wantsDecline);
        info.OverallGood = IsGood(info.Overall, wantsDecline);

        return info;
    }

    public static AchievementInfo BuildAchievement(IReadOnlyList<WeightEntry> entries, double? goalKg)
    {
        var ordered = entries.OrderBy(e => e.Date).ToList();
        var trend = ComputeTrend(ordered, goalKg);

        var achievement = new AchievementInfo
        {
            Trend = trend.Overall,
            TrendGood = trend.OverallGood,
            Streak = ComputeStreak(ordered, goalKg)
        };

        if (goalKg is not null && ordered.Count > 0)
        {
            var startWeight = ordered[0].WeightKg;
            var currentWeight = ordered[^1].WeightKg;
            var totalToLose = startWeight - goalKg.Value;

            if (Math.Abs(totalToLose) > 0.001)
            {
                var progressed = startWeight - currentWeight;
                achievement.ProgressPercent = Math.Clamp(progressed / totalToLose * 100, -100, 100);
            }
        }

        return achievement;
    }

    private static int ComputeStreak(List<WeightEntry> ordered, double? goalKg)
    {
        if (ordered.Count < 2)
        {
            return 0;
        }

        var wantsDecline = WantsDecline(ordered, goalKg);
        var streak = 0;

        for (var i = ordered.Count - 1; i > 0; i--)
        {
            var delta = ordered[i].WeightKg - ordered[i - 1].WeightKg;
            var improved = wantsDecline ? delta < -0.001 : delta > 0.001;

            if (!improved)
            {
                break;
            }

            streak++;
        }

        return streak;
    }

    // Without a goal, the default aim of this app is weight loss (declining is "good").
    internal static bool WantsDecline(IReadOnlyList<WeightEntry> ordered, double? goalKg)
    {
        var currentWeight = ordered.Count > 0 ? ordered[^1].WeightKg : (double?)null;
        return goalKg is null || currentWeight is null || goalKg < currentWeight;
    }

    private static TrendDirection DirectionOf(double delta)
    {
        const double epsilon = 0.05;
        return delta < -epsilon ? TrendDirection.Down : delta > epsilon ? TrendDirection.Up : TrendDirection.Flat;
    }

    private static bool IsGood(TrendDirection direction, bool wantsDecline) => direction switch
    {
        TrendDirection.Down => wantsDecline,
        TrendDirection.Up => !wantsDecline,
        _ => false
    };

    private static double LinearRegressionSlope(IReadOnlyList<WeightEntry> points)
    {
        var n = points.Count;
        var xMean = (n - 1) / 2.0;
        var yMean = points.Average(p => p.WeightKg);

        double numerator = 0;
        double denominator = 0;

        for (var i = 0; i < n; i++)
        {
            var dx = i - xMean;
            numerator += dx * (points[i].WeightKg - yMean);
            denominator += dx * dx;
        }

        return denominator == 0 ? 0 : numerator / denominator;
    }
}
