namespace EastmoneyMcpServer.Models.Metrics;

// ReSharper disable once InconsistentNaming
public readonly struct KDJ
{
    public required DateTime Date { get; init; }
    
    public required double K { get; init; }
    public required double D { get; init; }
    public required double J { get; init; }
    
    private static (double, double) GetHighAndLow(ReadOnlySpan<KLine> value)
    {
        if (value.IsEmpty) throw new ArgumentException("Span is empty");
        
        var high = value[0].High;
        var low = value[0].Low;
        
        for (var i = 1; i < value.Length; i++)
        {
            var kline = value[i];
            if (high < kline.High) high = kline.High;
            if (low > kline.Low) low = kline.Low;
        }
        return (high, low);
    }

    public static IEnumerable<KDJ> Calc(KLine[] klines, int n, int m1, int m2)
    {
        var lastK = .0;
        var lastD = .0;
        
        for (var index = 0; index < klines.Length; index++)
        {
            var kline = klines[index];
            
            var startIndex = index - n + 1 < 0 ? 0 : index - n + 1;
            var frame = klines[startIndex..(index + 1)];
            var (high, low) = GetHighAndLow(frame);

            var rsv = high - low == 0 ? 0 : (kline.Close - low) / (high - low) * 100;
            var k = index == 0 ? rsv : rsv / m1 + lastK * (m1 - 1.0) / m1;
            var d = index == 0 ? k : k / m2 + lastD * (m2 - 1.0) / m2;
            var j = 3 * k - 2 * d;

            lastK = k;
            lastD = d;
            yield return new KDJ
            {
                Date = kline.Date,
                K = Math.Round(k, 3), 
                D = Math.Round(d, 3), 
                J = Math.Round(j, 3)
            };
        }
    }

    public override string ToString()
    {
        var date = Date.ToString("yyyy-MM-dd");
        return $"{date},{K},{D},{J}";
    }
}
