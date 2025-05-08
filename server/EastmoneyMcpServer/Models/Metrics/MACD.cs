using EastmoneyMcpServer.Interfaces;
using MongoDB.Bson.Serialization.Attributes;

namespace EastmoneyMcpServer.Models.Metrics;

// ReSharper disable once InconsistentNaming
public readonly struct MACD : IMetric
{
    [BsonElement("dif")]
    public double Dif { get; init; }
    
    [BsonElement("dea")]
    public double Dea { get; init; }
    
    [BsonElement("macd")]
    public double Macd { get; init; }
    
    private static IEnumerable<double> Ema(IEnumerable<double> source, int period)
    {
        var multiplier = 2.0 / (period + 1);
        double? prevEma = null;
        foreach (var value in source)
        {
            prevEma = prevEma.HasValue ? (value - prevEma.Value) * multiplier + prevEma.Value : value;
            yield return prevEma.Value;
        }
    }
    
    public static IEnumerable<IMetric> Calc(KLine[] klines, int @short, int @long, int mid)
    {
        var closes = klines.Select(k => k.Close).ToArray();
        var emaShort = Ema(closes, @short).ToArray();
        var emaLong = Ema(closes, @long).ToArray();
        var dif = emaShort.Select((t, i) => t - emaLong[i]).ToArray();
        var dea = Ema(dif, mid).ToArray();
        var macd = dif.Select((t, i) => (t - dea[i]) * 2).ToArray();
        return klines.Select((_, i) => (IMetric)new MACD
        {
            Dif = Math.Round(dif[i], 3), 
            Dea = Math.Round(dea[i], 3), 
            Macd = Math.Round(macd[i], 3)
        });
    }

    public override string ToString() => $"{Dif},{Dea},{Macd}";
}
