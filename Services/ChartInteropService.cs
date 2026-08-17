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

    public ValueTask RenderAchievementAsync(string canvasId, double? percent, string emoji, string title, string? subtitle)
        => _js.InvokeVoidAsync("achievementCard.render", canvasId, new { percent, emoji, title, subtitle });
}
