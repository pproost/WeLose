namespace WeightTracker.Models;

public class AchievementInfo
{
    public double? ProgressPercent { get; set; }
    public int Streak { get; set; }
    public TrendDirection Trend { get; set; }
    public bool TrendGood { get; set; }
}
