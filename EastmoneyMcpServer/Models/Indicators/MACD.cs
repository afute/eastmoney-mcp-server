using EastmoneyMcpServer.Attributes;

namespace EastmoneyMcpServer.Models.Indicators;

// ReSharper disable once InconsistentNaming
public sealed class MACD : McpIndicators
{
    [McpToolCallResult("DIF")]
    public required decimal Dif { get; init; }
    
    [McpToolCallResult("DEA")]
    public required decimal Dea { get; init; }
    
    [McpToolCallResult("MACD")]
    public required decimal Macd { get; init; }
    
    private static IEnumerable<decimal> Ema(IEnumerable<decimal> source, int period)
    {
        var multiplier = (decimal)2.0 / (period + 1);
        decimal? prevEma = null;
        foreach (var value in source)
        {
            prevEma = prevEma.HasValue ? (value - prevEma.Value) * multiplier + prevEma.Value : value;
            yield return prevEma.Value;
        }
    }
    
    public static IEnumerable<MACD> Calc(StockKLine[] klines, int @short, int @long, int mid)
    {
        var closes = klines.Select(k => k.Close).ToArray();
        var emaShort = Ema(closes, @short).ToArray();
        var emaLong = Ema(closes, @long).ToArray();
        var dif = emaShort.Select((t, i) => t - emaLong[i]).ToArray();
        var dea = Ema(dif, mid).ToArray();
        var macd = dif.Select((t, i) => (t - dea[i]) * 2).ToArray();
        return klines.Select((_, i) => new MACD
        {
            Date = klines[i].Date,
            Dif = Math.Round(dif[i], 3), 
            Dea = Math.Round(dea[i], 3), 
            Macd = Math.Round(macd[i], 3)
        });
    }
}
