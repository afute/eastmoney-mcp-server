using EastmoneyMcpServer.Attributes;

namespace EastmoneyMcpServer.Models.Indicators;

// ReSharper disable once InconsistentNaming
public sealed class KDJ : McpIndicators
{
    [McpToolCallResult("K")]
    public required decimal K { get; init; }
    
    [McpToolCallResult("D")]
    public required decimal D { get; init; }
    
    [McpToolCallResult("J")]
    public required decimal J { get; init; }
    
    private static (decimal, decimal) GetHighAndLow(ReadOnlySpan<StockKLine> value)
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

    public static IEnumerable<KDJ> Calc(StockKLine[] klines, int n, int m1, int m2)
    {
        var lastK = (decimal).0;
        var lastD = (decimal).0;
        
        for (var index = 0; index < klines.Length; index++)
        {
            var kline = klines[index];
            
            var startIndex = index - n + 1 < 0 ? 0 : index - n + 1;
            var frame = klines[startIndex..(index + 1)];
            var (high, low) = GetHighAndLow(frame);

            var rsv = high - low == 0 ? 0 : (kline.Close - low) / (high - low) * 100;
            var k = index == 0 ? rsv : rsv / m1 + lastK * (m1 - (decimal)1.0) / m1;
            var d = index == 0 ? k : k / m2 + lastD * (m2 - (decimal)1.0) / m2;
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
}
