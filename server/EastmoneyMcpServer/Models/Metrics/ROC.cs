using EastmoneyMcpServer.Interfaces;
using MongoDB.Bson.Serialization.Attributes;

namespace EastmoneyMcpServer.Models.Metrics;

// ReSharper disable once InconsistentNaming
public readonly struct ROC : IMetric
{
    [BsonElement("roc")]
    public required double Value { get; init; }

    [BsonElement("maroc")]
    public required double MaRoc { get; init; }

    public static IEnumerable<IMetric> Calc(KLine[] klines, int n, int m)
    {
        var closes = klines.Select(k => k.Close).ToArray();
        var roc = CalculateRoc(closes, n).ToArray();
        var maroc = CalculateMaRoc(roc, m).ToArray();
        return klines.Select((_, i) => (IMetric)new ROC
        {
            Value = Math.Round(roc[i], 3), 
            MaRoc = Math.Round(maroc[i], 3)
        });
    }

    private static IEnumerable<double> CalculateRoc(double[] closes, int n)
    {
        for (var i = 0; i < closes.Length; i++)
        {
            var nn = Math.Min(i + 1, n);
            if (i >= nn && closes[i - nn] != 0) // 确保除数不为零
            {
                var rocValue = 100 * (closes[i] - closes[i - nn]) / closes[i - nn];
                yield return rocValue;
            }
            else yield return 0; // 前nn个数据点无法计算ROC
        }
    }

    private static IEnumerable<double> CalculateMaRoc(IEnumerable<double> roc, int m)
    {
        // 简单移动平均实现
        var buffer = new double[m];
        var index = 0;
        double sum = 0;
        var count = 0;

        foreach (var value in roc)
        {
            if (count >= m) sum -= buffer[index];

            sum += value;
            buffer[index] = value;
            index = (index + 1) % m;
            count++;

            if (count >= m) yield return sum / m;
            else yield return 0; // 前m-1个数据点无法计算MAROC
        }
    }
    
    public override string ToString() => $"{Value},{MaRoc}";
}
