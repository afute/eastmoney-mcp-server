using ModelContextProtocol.Server;

namespace EastmoneyMcpServer.Mcp.Tools;

/// <summary>
/// 指标工具
/// </summary>
// [McpServerToolType]
public sealed class MetricsTools
{
    
    // [McpServerTool(Name = "GetStockKlineMetricsData", Title = "获取指定股票K线指标数据")]
    // [Description("获取股票K线指标数据")]
    // public async Task<IEnumerable<string>> GetStockKlineMetricsData(
    //     [Description("股票代码 ps:A股长度为6位, 港股为5位")]
    //     [Required]
    //     string code,
    //     
    //     [Description("截止时间 ps:格式[yyyyMMdd]")]
    //     [StringLength(10)]
    //     [Required]
    //     string endDate, 
    //     
    //     [Description("指标数量 ps:最多请求250根")]
    //     [MaxLength(250)]
    //     [Required]
    //     int length, 
    //     
    //     [Description("k线类型 enum:[day, week, month]")]
    //     [EnumDataType(typeof(KLineType))]
    //     [Required]
    //     KLineType klineType,
    //     
    //     [Description("指标类型 enum:[cci, kdj, macd, roc, rsi]")]
    //     [EnumDataType(typeof(KLineMetricsType))]
    //     [Required]
    //     KLineMetricsType metricsType
    // )
    // {
    //     
    //     
    //     var collection = await CheckAndUpdate(code);
    //     
    //     var info = CultureInfo.InvariantCulture;
    //     var date = DateTime.ParseExact(endDate, "yyyyMMdd", info);
    //     date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
    //     var filter = Builders<KLine>.Filter.Lte(x => x.Date, date);
    //     var allKlines = await collection.Find(filter)
    //         .Sort(Builders<KLine>.Sort.Ascending(x => x.Date))
    //         .ToListAsync() ?? [];
    //     
    //     var klines = MergeKlines(allKlines.ToArray(), klineType);
    //     
    //     var metrics = (metricsType switch
    //     {
    //         KLineMetricsType.Cci => CCI.Calc(klines, 14),
    //         KLineMetricsType.Kdj => KDJ.Calc(klines, 9, 3, 3),
    //         KLineMetricsType.Macd => MACD.Calc(klines, 12, 26, 9),
    //         KLineMetricsType.Roc => ROC.Calc(klines, 12, 6),
    //         KLineMetricsType.Rsi => RSI.Calc(klines, 6, 12, 24),
    //         _ => throw new ArgumentOutOfRangeException(nameof(metricsType), metricsType, null)
    //     }).ToArray()[^length..];
    //     
    //     return metrics.Select(x => x.ToString()).ToArray();
    // }
}
