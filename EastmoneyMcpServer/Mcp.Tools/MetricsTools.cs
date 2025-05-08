using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EastmoneyMcpServer.Helper;
using EastmoneyMcpServer.Models.Enums;
using EastmoneyMcpServer.Models.Metrics;
using ModelContextProtocol.Server;
using MongoDB.Driver;

namespace EastmoneyMcpServer.Mcp.Tools;

/// <summary>
/// 指标工具
/// </summary>
[McpServerToolType]
public sealed partial class MetricsTools(IHttpClientFactory httpClientFactory, IMongoClient mongoClient)
{
    [McpServerTool(Name = "CalcCCI", Title = "计算K线指标[CCI]")]
    [Description("获取股票K线商品路径指标数据[CCI]")]
    [return: Description("日期,CCI")]
    // ReSharper disable once InconsistentNaming
    public async Task<IEnumerable<string>> CalcCCI(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,
        
        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,
        
        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,
        
        [Description("k线类型 enum:[day, week, month]")]
        [EnumDataType(typeof(KLineType))]
        [Required]
        KLineType klineType,
        
        [Description("指标计算所需参数, 代表天数, 默认14")]
        [Range(2, 100)]
        int n = 14)
    {
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        
        var klineTool = new KLineTools(httpClientFactory, mongoClient);
        var klines = await klineTool.GetKlineData(code, end, false);
        klines = klines.MergeKlines(klineType);
        var metrics = CCI.Calc(klines.ToArray(), n).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
}

public sealed partial class MetricsTools
{
    [McpServerTool(Name = "CalcKDJ", Title = "计算K线指标[KDJ]")]
    [Description("获取股票K线随机指标数据[KDJ]")]
    [return: Description("日期,K,D,J")]
    // ReSharper disable once InconsistentNaming
    public async Task<IEnumerable<string>> CalcKDJ(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,
        
        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,
        
        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,
        
        [Description("k线类型 enum:[day, week, month]")]
        [EnumDataType(typeof(KLineType))]
        [Required]
        KLineType klineType,
        
        [Description("指标计算所需参数, 代表天数, 默认9")]
        [Range(2, 90)]
        int n = 9,
        
        [Description("指标计算所需参数, 代表天数, 默认3")]
        [Range(2, 30)]
        int m1 = 3,
        
        [Description("指标计算所需参数, 代表天数, 默认3")]
        [Range(2, 30)]
        int m2 = 3)
    {
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        
        var klineTool = new KLineTools(httpClientFactory, mongoClient);
        var klines = await klineTool.GetKlineData(code, end, false);
        klines = klines.MergeKlines(klineType);
        var metrics = KDJ.Calc(klines.ToArray(), n, m1, m2).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
}

public sealed partial class MetricsTools
{
    [McpServerTool(Name = "CalcMACD", Title = "计算K线指标[MACD]")]
    [Description("获取股票K线平滑异同平均线数据[MACD]")]
    [return: Description("日期,DIF,DEA,MACD")]
    // ReSharper disable once InconsistentNaming
    public async Task<IEnumerable<string>> CalcMACD(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,
        
        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,
        
        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,
        
        [Description("k线类型 enum:[day, week, month]")]
        [EnumDataType(typeof(KLineType))]
        [Required]
        KLineType klineType,
        
        [Description("指标计算所需参数, 代表天数, 默认12")]
        [Range(2, 200)]
        int @short = 12,
        
        [Description("指标计算所需参数, 代表天数, 默认26")]
        [Range(2, 200)]
        int @long = 26,
        
        [Description("指标计算所需参数, 代表天数, 默认9")]
        [Range(2, 200)]
        int mid = 9)
    {
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        
        var klineTool = new KLineTools(httpClientFactory, mongoClient);
        var klines = await klineTool.GetKlineData(code, end, false);
        klines = klines.MergeKlines(klineType);
        var metrics = MACD.Calc(klines.ToArray(), @short, @long, mid).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
}

public sealed partial class MetricsTools
{
    [McpServerTool(Name = "CalcROC", Title = "计算K线指标[ROC]")]
    [Description("获取股票K线变动率指标数据[ROC]")]
    [return: Description("日期,ROC,MAROC")]
    // ReSharper disable once InconsistentNaming
    public async Task<IEnumerable<string>> CalcROC(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,
        
        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,
        
        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,
        
        [Description("k线类型 enum:[day, week, month]")]
        [EnumDataType(typeof(KLineType))]
        [Required]
        KLineType klineType,
        
        [Description("指标计算所需参数, 代表天数, 默认12")]
        [Range(2, 120)]
        int n = 12,
        
        [Description("指标计算所需参数, 代表天数, 默认6")]
        [Range(2, 60)]
        int m = 6)
    {
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        
        var klineTool = new KLineTools(httpClientFactory, mongoClient);
        var klines = await klineTool.GetKlineData(code, end, false);
        klines = klines.MergeKlines(klineType);
        var metrics = ROC.Calc(klines.ToArray(), n, m).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
}

public sealed partial class MetricsTools
{
    [McpServerTool(Name = "CalcRSI", Title = "计算K线指标[RSI]")]
    [Description("获取股票K线相对强弱指标数据[RSI]")]
    [return: Description("日期,RSI1,RSI2,RSI3")]
    // ReSharper disable once InconsistentNaming
    public async Task<IEnumerable<string>> CalcRSI(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,
        
        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,
        
        [Description("指标数量 ps:最多请求250根")]
        [Required]
        int length,
        
        [Description("k线类型 enum:[day, week, month]")]
        [EnumDataType(typeof(KLineType))]
        [Required]
        KLineType klineType,
        
        [Description("指标计算所需参数, 代表天数, 默认6")]
        [Range(2, 120)]
        int n1 = 6,
        
        [Description("指标计算所需参数, 代表天数, 默认12")]
        [Range(2, 250)]
        int n2 = 12,
        
        [Description("指标计算所需参数, 代表天数, 默认24")]
        [Range(2, 500)]
        int n3 = 24)
    {
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        
        var klineTool = new KLineTools(httpClientFactory, mongoClient);
        var klines = await klineTool.GetKlineData(code, end, false);
        klines = klines.MergeKlines(klineType);
        var metrics = RSI.Calc(klines.ToArray(), n1, n2, n3).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
}
