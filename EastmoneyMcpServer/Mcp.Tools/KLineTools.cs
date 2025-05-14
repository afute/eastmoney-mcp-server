using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using EastmoneyMcpServer.Models;
using EastmoneyMcpServer.Models.Enums;
using EastmoneyMcpServer.Models.Attributes;
using EastmoneyMcpServer.Models.Helper;
using EastmoneyMcpServer.Models.Metrics;
using EastmoneyMcpServer.Models.Response;
using EastmoneyMcpServer.Models.Settings;
using LazyCache;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using MongoDB.Driver;

namespace EastmoneyMcpServer.Mcp.Tools;

[McpServerToolType]
public sealed partial class KLineTools(
    IHttpClientFactory clientFactory,
    IServiceProvider serviceProvider,
    IMongoClient mongoClient,
    ILogger<KLineTools> logger,
    IAppCache appCache
    )
{
    private const string DatabaseName = "klines";
    private readonly HttpClient _httpClient = clientFactory.CreateClient("push2his.eastmoney.com");
}

#region 更新数据库
public sealed partial class KLineTools
{
    /// <summary>
    /// 更新并获取K线集合
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    private async Task<IMongoCollection<KLine>> GetCollection(string code)
    {
        var options = serviceProvider.GetRequiredService<IOptions<KLineSettings>>();
        
        var task = await appCache.GetOrAddAsync(code, async entry =>
        {
            var database = mongoClient.GetDatabase(DatabaseName);
            var collection = database.GetCollection<KLine>(code);
            var indexer = Builders<KLine>
                .IndexKeys.Ascending(k => k.Date);
            var model = new CreateIndexModel<KLine>(indexer, new CreateIndexOptions { Unique = true });
            await collection.Indexes.CreateOneAsync(model);

            // 数据库中最新的2根K线
            var innerKlines = await collection.Find(Builders<KLine>.Filter.Empty)
                .Sort(Builders<KLine>.Sort.Descending(k => k.Date))
                .Limit(2)
                .ToListAsync();

            // 需要更新到的时间
            var date = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

            // 数据库中没有数据, 直接跳转到全部更新
            if (innerKlines.Count != 2) goto updateAll;

            var offset = (date - innerKlines[1].Date).Days + 1;
            var (_, latestKlines) = await GetEastmoneyKLineData(code, date, offset);

            // 对比是否除权
            var oldFullKline = innerKlines[1]; // 旧的完整K线
            var networkKline = latestKlines.First(k => k.Date == oldFullKline.Date);
            if (oldFullKline.Open == networkKline.Open && oldFullKline.Close == networkKline.Close)
            {
                // 未除权, 拼接最新的K线
                logger.LogInformation("更新部分[{code}]K线至数据库, 更新数量{len}", code, latestKlines.Length);
                var delFilter = Builders<KLine>.Filter.Gte(x => x.Date, latestKlines[0].Date);
                await collection.DeleteManyAsync(delFilter);
                await collection.InsertManyAsync(latestKlines);
                goto @return;
            }

            // 更新全部K线
            updateAll:
            await collection.DeleteManyAsync(Builders<KLine>.Filter.Empty);
            var (total, allKLines) = await GetEastmoneyKLineData(code, date);
            logger.LogInformation("更新全部[{code}]K线至数据库, 更新数量{len}", code, total);
            await collection.InsertManyAsync(allKLines);

            @return:
            entry.AbsoluteExpirationRelativeToNow = options.Value.CacheTime;
            return collection;
        });
        return task;
    }
}
#endregion

#region K线数据请求
public sealed partial class KLineTools
{
    private async Task<ValueTuple<int, KLine[]>> GetEastmoneyKLineData(QueryBuilder query)
    {
        using var response = await _httpClient.GetAsync("/api/qt/stock/kline/get" + query);
        response.EnsureSuccessStatusCode();
        
        var text = await response.Content.ReadAsStringAsync();
        var res = JsonSerializer.Deserialize<Response1>(text);
        if (res is null) throw new Exception(text);
        var klines = (from val in res.Data?.Lines ?? [] select KLine.Create(val)).ToArray();
        return ValueTuple.Create(res.Data?.DkTotal ?? 0, klines);
    }
    
    private async Task<ValueTuple<int, KLine[]>> GetEastmoneyKLineData(string code, DateTime end)
    {
        var exchange = StockHelper.GetStockExchange(code);
        var query = new QueryBuilder
        {
            { "secid", exchange.GetRequiredValue<string>("code") + "." + code },
            { "fields1", "f1,f3,f5" },
            { "fields2", "f51,f52,f53,f54,f55,f56" },
            { "klt", "101" },
            { "fqt", "1" },
            { "end", end.ToString("yyyyMMdd") },
            { "beg", "0" }
        };
        return await GetEastmoneyKLineData(query);
    }
    
    private async Task<ValueTuple<int, KLine[]>> GetEastmoneyKLineData(string code, DateTime end, int length)
    {
        var exchange = StockHelper.GetStockExchange(code);
        
        var query = new QueryBuilder
        {
            { "secid", exchange.GetRequiredValue<string>("code") + "." + code },
            { "fields1", "f1,f3,f5" },
            { "fields2", "f51,f52,f53,f54,f55,f56" },
            { "klt", "101" },
            { "fqt", "1" },
            { "end", end.ToString("yyyyMMdd") },
            { "lmt", length.ToString() }
        };
        
        return await GetEastmoneyKLineData(query);
    }
}
#endregion

public sealed partial class KLineTools
{
    private async Task<KLine[]> GetKLines(string code, string endDate, KLineType klineType)
    {
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        
        IMongoCollection<KLine> collection;
        try
        {
            collection = await GetCollection(code);
        }
        catch (Exception e)
        {
            logger.LogError(e, "获取K线数据失败");
            throw;
        }

        var filter = Builders<KLine>.Filter.Lte(x => x.Date, end);
        var result = await collection.Find(filter)
            .Sort(Builders<KLine>.Sort.Ascending(x => x.Date))
            .ToListAsync() ?? [];
        return result.MergeKlines(klineType);
    }
}

public sealed partial class KLineTools
{
    [McpServerTool(Name = "get_kline_data", Title = "获取股票K线数据")]
    [Description("获取K线数据")]
    [return: Description("日期,开盘价,收盘价,最低价,最高价,成交量")]
    public async Task<IEnumerable<string>> GetKLineData(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,
        
        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [Required]
        string endDate,
        
        [Description("K线数量 ps:最多请求250根")]
        [Required]
        int length,
    
        [Description("k线类型 ps:参考enum")]
        [EnumDataType(typeof(KLineType))]
        [Required]
        KLineType klineType
    )
    {
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        
        IMongoCollection<KLine> collection;
        try
        {
            collection = await GetCollection(code);
        }
        catch (Exception e)
        {
            logger.LogError(e, "获取K线数据失败");
            throw;
        }
        
        var filter = Builders<KLine>.Filter.Lte(x => x.Date, end);
        var result = await collection.Find(filter)
            .Sort(Builders<KLine>.Sort.Descending(x => x.Date))
            .Limit(length)
            .ToListAsync() ?? [];
        return result.MergeKlines(klineType).Select(k => k.ToRaw()).Reverse();
    }
    
    #region 获取指标 CCI
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_cci", Title = "获取K线指标[CCI]")]
    [Description("#指标获取 商品路径指标数据[CCI]")]
    [return: Description("日期,CCI")]
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
        
        [Description("指标计算所需参数, 代表天数, 默认14")]
        [Range(2, 100)]
        int n = 14)
    {
        var klines = await GetKLines(code, endDate, klineType);
        var metrics = CCI.Calc(klines, n).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
    #endregion
    
    #region 获取指标 KDJ
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_kdj", Title = "获取K线指标[KDJ]")]
    [Description("#指标获取 随机指标数据[KDJ]")]
    [return: Description("日期,K,D,J")]
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
        var klines = await GetKLines(code, endDate, klineType);
        var metrics = KDJ.Calc(klines, n, m1, m2).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
    #endregion
    
    #region 获取指标 MACD
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_macd", Title = "获取K线指标[MACD]")]
    [Description("#指标获取 平滑异同平均线数据[MACD]")]
    [return: Description("日期,DIF,DEA,MACD")]
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
        var klines = await GetKLines(code, endDate, klineType);
        var metrics = MACD.Calc(klines, @short, @long, mid).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
    #endregion
    
    #region 获取指标 ROC
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_roc", Title = "获取K线指标[ROC]")]
    [Description("#指标获取 变动率指标数据[ROC]")]
    [return: Description("日期,ROC,MAROC")]
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
        
        [Description("指标计算所需参数, 代表天数, 默认12")]
        [Range(2, 120)]
        int n = 12,
        
        [Description("指标计算所需参数, 代表天数, 默认6")]
        [Range(2, 60)]
        int m = 6)
    {
        var klines = await GetKLines(code, endDate, klineType);
        var metrics = ROC.Calc(klines, n, m).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
    #endregion
    
    #region 获取指标 RSI
    // ReSharper disable once InconsistentNaming
    [McpServerTool(Name = "get_rsi", Title = "获取K线指标[RSI]")]
    [Description("#指标获取 相对强弱指标数据[RSI]")]
    [return: Description("日期,RSI1,RSI2,RSI3")]
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
        var klines = await GetKLines(code, endDate, klineType);
        var metrics = RSI.Calc(klines, n1, n2, n3).ToArray()[^length..];
        return metrics.Select(x => x.ToString());
    }
    #endregion
}
