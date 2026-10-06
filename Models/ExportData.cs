namespace WeightTracker.Models;

public class ExportData
{
    public int Version { get; set; } = 1;
    public List<WeightEntry>? Entries { get; set; }
    public double? GoalKg { get; set; }
}
