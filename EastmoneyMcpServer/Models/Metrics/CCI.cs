namespace EastmoneyMcpServer.Models.Metrics;

// ReSharper disable once InconsistentNaming
public sealed class CCI
{
    public required DateTime Date { get; init; }
    
    public required double Value { get; init; }

    public static IEnumerable<CCI> Calc(KLine[] klines, int n)
    {
        var typicalPrices = new double[klines.Length];
        for (var i = 0; i < klines.Length; i++)
            typicalPrices[i] = (klines[i].High + klines[i].Low + klines[i].Close) / 3.0;
        
        for (var i = 0; i < klines.Length; i++)
        {
            if (i < n - 1)
            {
                yield return new CCI { Date = klines[i].Date, Value = 0 };
                continue;
            }
            var ma = typicalPrices[(i - n + 1)..(i + 1)].Average();
            var sumDev = .0;
            for (var j = i - n + 1; j <= i; j++)
                sumDev += Math.Abs(typicalPrices[j] - ma);
            var aveDev = sumDev / n;
            var value = aveDev != 0 ? (typicalPrices[i] - ma) * 1000.0 / (15 * aveDev) : 0;
            yield return new CCI { Date = klines[i].Date, Value = Math.Round(value, 3) };
        }
    }

    public override string ToString()
    {
        var date = Date.ToString("yyyy-MM-dd");
        return $"{date},{Value}";
    }
}
