using EastmoneyMcpServer.Models.Enums;

namespace EastmoneyMcpServer.Models.Helper;

public static class StockHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static StockExchange GetStockExchange(string code)
    {
        if (code.Length == 5) return StockExchange.HongKong;
        if (code.Length != 6 || !int.TryParse(code, out _))
            throw new FormatException("不支持A股和港股以外的股票");

        return code[0].ToString() switch
        {
            "5" or "6" => StockExchange.Shanghai,
            "1" or "0" or "3" => StockExchange.Shenzhen,
            "8" => StockExchange.Beijing,
            _ => throw new FormatException("不支持A股和港股以外的股票")
        };
    } 
    
    /// <summary>
    /// 合并K线
    /// </summary>
    /// <param name="kls"></param>
    /// <param name="klineType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static KLine[] MergeKlines(this IEnumerable<KLine> kls, KLineType klineType)
    {
        var klines = kls.ToArray();
        var result = new List<KLine>(klines.Length);
        
        Func<DateTime, int> mergeFunc = klineType switch
        {
            KLineType.Day => datetime =>
            {
                var year = datetime.Year;
                var month = datetime.Month;
                var day = datetime.Day;
                return year * 10000 + month * 100 + day;
            },

            KLineType.Week => datetime =>
            {
                var (yearNum, weekNum) = datetime.GetIsoYearAndWeek();
                return yearNum * 100 + weekNum;
            },

            KLineType.Month => datetime =>
            {
                var year = datetime.Year;
                var month = datetime.Month;
                return year * 100 + month;
            },

            _ => throw new ArgumentOutOfRangeException(nameof(klineType), klineType, null)
        };
        
        var logo = mergeFunc(klines[0].Date);
        var kline = klines[0];
        
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 1; i < klines.Length; i++)
        {
            var newLogo = mergeFunc(klines[i].Date);
            if (newLogo != logo)
            {
                result.Add(kline);
                kline = klines[i];
            }
            else kline = kline.Merge(klines[i]);
            logo = newLogo;
        }
        result.Add(kline);
        
        return result.ToArray();
    }
}
