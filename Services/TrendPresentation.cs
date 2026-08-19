using MudBlazor;
using WeightTracker.Models;

namespace WeightTracker.Services;

public static class TrendPresentation
{
    public static string TrendIcon(TrendDirection direction) => direction switch
    {
        TrendDirection.Down => Icons.Material.Filled.TrendingDown,
        TrendDirection.Up => Icons.Material.Filled.TrendingUp,
        TrendDirection.Flat => Icons.Material.Filled.TrendingFlat,
        _ => Icons.Material.Filled.HourglassEmpty
    };

    public static Color TrendColor(TrendDirection direction, bool good) => direction switch
    {
        TrendDirection.Unknown => Color.Default,
        TrendDirection.Flat => Color.Info,
        _ => good ? Color.Success : Color.Error
    };

    public static string LastVsPreviousLabel(TrendDirection direction) => direction switch
    {
        TrendDirection.Down => "Lager dan vorige meting",
        TrendDirection.Up => "Hoger dan vorige meting",
        TrendDirection.Flat => "Gelijk aan vorige meting",
        _ => "Nog niet genoeg data"
    };

    public static string OverallLabel(TrendDirection direction) => direction switch
    {
        TrendDirection.Down => "Dalende trend",
        TrendDirection.Up => "Stijgende trend",
        TrendDirection.Flat => "Vlakke trend",
        _ => "Nog niet genoeg data"
    };
}
