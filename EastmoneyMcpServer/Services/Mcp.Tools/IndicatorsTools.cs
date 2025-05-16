using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EastmoneyMcpServer.Extensions;
using EastmoneyMcpServer.Interfaces;
using EastmoneyMcpServer.Models;
using EastmoneyMcpServer.Models.Enums;
using EastmoneyMcpServer.Models.Indicators;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using MongoDB.Driver;

namespace EastmoneyMcpServer.Services.Mcp.Tools;

[McpServerToolType]
public sealed partial class IndicatorsTools(IKLineInstance kLineInstance, ILogger<IndicatorsTools> logger)
{
    private async Task<IEnumerable<StockKLine>> GetKLines(string code, string endDate, KLineType klineType, 
        AdjustedType adjustedType, CancellationToken token)
    {
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        
        IMongoCollection<StockKLine> collection;
        try
        {
            collection = await kLineInstance.GetCollectionFromCheck(code, adjustedType, token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "获取K线数据失败");
            throw new McpException("更新K线数据至缓存失败", McpErrorCode.InternalError);
        }

        var filter = Builders<StockKLine>.Filter.Lte(x => x.Date, end);
        var result = await collection.Find(filter)
            .Sort(Builders<StockKLine>.Sort.Ascending(x => x.Date))
            .ToListAsync(token) ?? [];
        return result.MergeKlines(klineType);
    }
}

#region 获取指标 CCI
public sealed partial class IndicatorsTools
{
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_cci", Title = "获取K线指标[CCI]")]
    [Description("#指标获取 商品路径指标数据[CCI]")]
    public async Task<IEnumerable<string>> GetCCI(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,

        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,

        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,

        [Description("k线类型 ps:参考enum")]
        [EnumDataType(typeof(KLineType))] 
        [Required]
        KLineType klineType,

        [Description("复权类型 ps:参考enum 无特殊情况默认为前复权")]
        [EnumDataType(typeof(AdjustedType))]
        AdjustedType adjustedType = AdjustedType.Forward,

        [Description("指标计算所需参数, 代表天数, 默认14")]
        [Range(2, 100)]
        int n = 14,
        
        CancellationToken token = default
    )
    {
        var klines = await GetKLines(code, endDate, klineType, adjustedType, token);
        var metrics = CCI.Calc(klines.ToArray(), n).ToArray()[^length..];
        return metrics.Select(x => x.ToMcpResult());
    }
}
#endregion

#region 获取指标 KDJ
public sealed partial class IndicatorsTools
{
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_kdj", Title = "获取K线指标[KDJ]")]
    [Description("#指标获取 随机指标数据[KDJ]")]
    public async Task<IEnumerable<string>> GetKDJ(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,

        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,

        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,

        [Description("k线类型 ps:参考enum")]
        [EnumDataType(typeof(KLineType))] 
        [Required]
        KLineType klineType,

        [Description("复权类型 ps:参考enum 无特殊情况默认为前复权")]
        [EnumDataType(typeof(AdjustedType))]
        AdjustedType adjustedType = AdjustedType.Forward,
        
        [Description("指标计算所需参数, 代表天数, 默认9")]
        [Range(2, 90)]
        int n = 9,
        
        [Description("指标计算所需参数, 代表天数, 默认3")]
        [Range(2, 30)]
        int m1 = 3,
        
        [Description("指标计算所需参数, 代表天数, 默认3")]
        [Range(2, 30)]
        int m2 = 3,
        
        CancellationToken token = default
        )
    {
        var klines = await GetKLines(code, endDate, klineType, adjustedType, token);
        var metrics = KDJ.Calc(klines.ToArray(), n, m1, m2).ToArray()[^length..];
        return metrics.Select(x => x.ToMcpResult());
    }
}
#endregion

#region 获取指标 MACD

public sealed partial class IndicatorsTools
{
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_macd", Title = "获取K线指标[MACD]")]
    [Description("#指标获取 平滑异同平均线数据[MACD]")]
    public async Task<IEnumerable<string>> GetMACD(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,

        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,

        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,

        [Description("k线类型 ps:参考enum")]
        [EnumDataType(typeof(KLineType))] 
        [Required]
        KLineType klineType,

        [Description("复权类型 ps:参考enum 无特殊情况默认为前复权")]
        [EnumDataType(typeof(AdjustedType))]
        AdjustedType adjustedType = AdjustedType.Forward,
        
        [Description("指标计算所需参数, 代表天数, 默认12")]
        [Range(2, 200)]
        int @short = 12,
        
        [Description("指标计算所需参数, 代表天数, 默认26")]
        [Range(2, 200)]
        int @long = 26,
        
        [Description("指标计算所需参数, 代表天数, 默认9")]
        [Range(2, 200)]
        int mid = 9,
        
        CancellationToken token = default
        )
    {
        var klines = await GetKLines(code, endDate, klineType, adjustedType, token);
        var metrics = MACD.Calc(klines.ToArray(), @short, @long, mid).ToArray()[^length..];
        return metrics.Select(x => x.ToMcpResult());
    }
}
#endregion

#region 获取指标 ROC

public sealed partial class IndicatorsTools
{
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_roc", Title = "获取K线指标[ROC]")]
    [Description("#指标获取 变动率指标数据[ROC]")]
    public async Task<IEnumerable<string>> GetROC(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,

        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,

        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,

        [Description("k线类型 ps:参考enum")]
        [EnumDataType(typeof(KLineType))] 
        [Required]
        KLineType klineType,

        [Description("复权类型 ps:参考enum 无特殊情况默认为前复权")]
        [EnumDataType(typeof(AdjustedType))]
        AdjustedType adjustedType = AdjustedType.Forward,
        
        [Description("指标计算所需参数, 代表天数, 默认12")]
        [Range(2, 120)]
        int n = 12,
        
        [Description("指标计算所需参数, 代表天数, 默认6")]
        [Range(2, 60)]
        int m = 6,
        
        CancellationToken token = default
        )
    {
        var klines = await GetKLines(code, endDate, klineType, adjustedType, token);
        var metrics = ROC.Calc(klines.ToArray(), n, m).ToArray()[^length..];
        return metrics.Select(x => x.ToMcpResult());
    }
}
#endregion

#region 获取指标 RSI

public sealed partial class IndicatorsTools
{
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_rsi", Title = "获取K线指标[RSI]")]
    [Description("#指标获取 相对强弱指标数据[RSI]")]
    public async Task<IEnumerable<string>> GetRSI(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,

        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,

        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,

        [Description("k线类型 ps:参考enum")]
        [EnumDataType(typeof(KLineType))] 
        [Required]
        KLineType klineType,

        [Description("复权类型 ps:参考enum 无特殊情况默认为前复权")]
        [EnumDataType(typeof(AdjustedType))]
        AdjustedType adjustedType = AdjustedType.Forward,
        
        [Description("指标计算所需参数, 代表天数, 默认6")]
        [Range(2, 120)]
        int n1 = 6,
        
        [Description("指标计算所需参数, 代表天数, 默认12")]
        [Range(2, 250)]
        int n2 = 12,
        
        [Description("指标计算所需参数, 代表天数, 默认24")]
        [Range(2, 500)]
        int n3 = 24,
        
        CancellationToken token = default
        )
    {
        var klines = await GetKLines(code, endDate, klineType, adjustedType, token);
        var metrics = RSI.Calc(klines.ToArray(), n1, n2, n3).ToArray()[^length..];
        return metrics.Select(x => x.ToMcpResult());
    }
}
#endregion
