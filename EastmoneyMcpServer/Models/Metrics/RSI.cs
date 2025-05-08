using EastmoneyMcpServer.Interfaces;
using MongoDB.Bson.Serialization.Attributes;

namespace EastmoneyMcpServer.Models.Metrics;

// ReSharper disable once InconsistentNaming
public readonly struct RSI : IMetric
{
    [BsonElement("rsi1")]
    public required double Rsi1 { get; init; }
    
    [BsonElement("rsi2")]
    public required double Rsi2 { get; init; }
    
    [BsonElement("rsi3")]
    public required double Rsi3 { get; init; }
    
    public static IEnumerable<IMetric> Calc(KLine[] klines, int n1, int n2, int n3)
    {
        var closes = klines.Select(k => k.Close).ToArray();
        var lc = closes.Select((_, i) => i > 0 ? closes[i - 1] : 0).ToArray();
        
        var gains = closes.Zip(lc, (c, l) => Math.Max(c - l, 0)).ToArray();
        var absDeltas = closes.Zip(lc, (c, l) => Math.Abs(c - l)).ToArray();
        
        // 计算三个周期的RSI
        var rsi1 = CalculateRsi(gains, absDeltas, n1).ToArray();
        var rsi2 = CalculateRsi(gains, absDeltas, n2).ToArray();
        var rsi3 = CalculateRsi(gains, absDeltas, n3).ToArray();
        
        return klines.Select((_, i) => (IMetric)new RSI
        {
            Rsi1 = Math.Round(rsi1[i], 3), 
            Rsi2 = Math.Round(rsi2[i], 3), 
            Rsi3 = Math.Round(rsi3[i], 3)
        });
    }
    
    private static IEnumerable<double> CalculateRsi(double[] gains, double[] absDeltas, int period)
    {
        yield return 0;
        
        double? smaGain = null;
        double? smaAbsDelta = null;
        
        for (var i = 1; i < gains.Length; i++) // 从第1个数据开始计算（需要前一日收盘价）
        {
            // 计算SMA（第一个值为简单平均，后续为平滑移动平均）
            if (i >= period)
            {
                smaGain = smaGain.HasValue 
                    ? (gains[i] + (period - 1) * smaGain.Value) / period
                    : gains.Skip(i - period + 1).Take(period).Average();
                
                smaAbsDelta = smaAbsDelta.HasValue
                    ? (absDeltas[i] + (period - 1) * smaAbsDelta.Value) / period
                    : absDeltas.Skip(i - period + 1).Take(period).Average();
                
                var rsi = smaAbsDelta.Value != 0 
                    ? smaGain.Value / smaAbsDelta.Value * 100 
                    : 50; // 当分母为0时设为中性值50
                
                yield return rsi;
            }
            else yield return 0; // 前period-1个数据点无法计算RSI
        }
    }
    
    public override string ToString() => $"{Rsi1},{Rsi2},{Rsi3}";
}
