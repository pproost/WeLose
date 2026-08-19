using Microsoft.JSInterop;

namespace WeightTracker.Services;

public class ChartInteropService
{
    private readonly IJSRuntime _js;

    public ChartInteropService(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask RenderLineChartAsync(string canvasId, string label, IEnumerable<string> labels, IEnumerable<double> values, string color)
        => _js.InvokeVoidAsync("weightChart.render", canvasId, label, labels, values, color);

    public ValueTask ShareCanvasAsync(string canvasId, string title, string text)
        => _js.InvokeVoidAsync("weightChart.share", canvasId, title, text);

    public ValueTask RenderAchievementAsync(
        string canvasId, double? percent, string emoji, string title, string? subtitle,
        (string Symbol, string Color, string Label) trendLast,
        (string Symbol, string Color, string Label) trendOverall)
        => _js.InvokeVoidAsync("achievementCard.render", canvasId, new
        {
            percent,
            emoji,
            title,
            subtitle,
            trendLast = new { symbol = trendLast.Symbol, color = trendLast.Color, label = trendLast.Label },
            trendOverall = new { symbol = trendOverall.Symbol, color = trendOverall.Color, label = trendOverall.Label }
        });
}
