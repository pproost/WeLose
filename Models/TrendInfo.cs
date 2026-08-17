namespace WeightTracker.Models;

public enum TrendDirection
{
    Unknown,
    Down,
    Up,
    Flat
}

public class TrendInfo
{
    public TrendDirection LastVsPrevious { get; set; }
    public bool LastVsPreviousGood { get; set; }
    public TrendDirection Overall { get; set; }
    public bool OverallGood { get; set; }
}
