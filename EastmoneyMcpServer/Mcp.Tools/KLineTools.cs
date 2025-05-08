using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using UserAgentGenerator;
using EastmoneyMcpServer.Attributes;
using EastmoneyMcpServer.Helper;
using EastmoneyMcpServer.Models;
using EastmoneyMcpServer.Models.Enums;
using EastmoneyMcpServer.Models.Metrics;
using EastmoneyMcpServer.Models.Response;
using Microsoft.AspNetCore.Http.Extensions;
using ModelContextProtocol.Server;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace EastmoneyMcpServer.Mcp.Tools;

[McpServerToolType]
public sealed partial class KLineTools(IHttpClientFactory factory, IMongoDatabase database)
{
    [McpServerTool(Name = "GetStockKlineData", Title = "获取指定股票K线数据")]
    [Description("获取股票K线数据")]
    [return: Description("日期,开盘价,收盘价,最低价,最高价,成交量")]
    public async Task<IEnumerable<string>> GetStockKlineData(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,
        
        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [StringLength(10)]
        [Required]
        string endDate,
        
        [Description("K线数量 ps:最多请求250根")]
        [MaxLength(250)]
        [Required]
        int length,
        
        [Description("k线类型 enum:[day, week, month]")]
        [EnumDataType(typeof(KLineType))]
        [Required]
        KLineType klineType
        )
    {
        var collection = await CheckAndUpdate(code);
        
        var info = CultureInfo.InvariantCulture;
        var date = DateTime.ParseExact(endDate, "yyyyMMdd", info);
        date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        var filter = Builders<KLine>.Filter.Lte(x => x.Date, date);
        var data = await collection.Find(filter)
            .Sort(Builders<KLine>.Sort.Ascending(x => x.Date))
            .ToListAsync() ?? [];

        var mergeFn = GetMergeFunc(klineType);
        var klines = MergeKLines(data.ToArray(), mergeFn)[^length..];
        return klines.Select(k => k.ToString());
    }

    [McpServerTool(Name = "GetStockKlineMetricsData", Title = "获取指定股票K线指标数据")]
    [Description("获取股票K线指标数据")]
    public async Task<IEnumerable<string>> GetStockKlineMetricsData(
        [Description("股票代码 ps:A股长度为6位, 港股为5位")]
        [Required]
        string code,
        
        [Description("截止时间 ps:格式[yyyyMMdd]")]
        [StringLength(10)]
        [Required]
        string endDate, 
        
        [Description("指标数量 ps:最多请求250根")]
        [MaxLength(250)]
        [Required]
        int length, 
        
        [Description("k线类型 enum:[day, week, month]")]
        [EnumDataType(typeof(KLineType))]
        [Required]
        KLineType klineType,
        
        [Description("指标类型 enum:[cci, kdj, macd, roc, rsi]")]
        [EnumDataType(typeof(KLineMetricsType))]
        [Required]
        KLineMetricsType metricsType
        )
    {
        var collection = await CheckAndUpdate(code);
        
        var info = CultureInfo.InvariantCulture;
        var date = DateTime.ParseExact(endDate, "yyyyMMdd", info);
        date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        var filter = Builders<KLine>.Filter.Lte(x => x.Date, date);
        var allKlines = await collection.Find(filter)
            .Sort(Builders<KLine>.Sort.Ascending(x => x.Date))
            .ToListAsync() ?? [];

        var mergeFn = GetMergeFunc(klineType);
        var klines = MergeKLines(allKlines.ToArray(), mergeFn);
        
        var metrics = (metricsType switch
        {
            KLineMetricsType.Cci => CCI.Calc(klines, 14),
            KLineMetricsType.Kdj => KDJ.Calc(klines, 9, 3, 3),
            KLineMetricsType.Macd => MACD.Calc(klines, 12, 26, 9),
            KLineMetricsType.Roc => ROC.Calc(klines, 12, 6),
            KLineMetricsType.Rsi => RSI.Calc(klines, 6, 12, 24),
            _ => throw new ArgumentOutOfRangeException(nameof(metricsType), metricsType, null)
        }).ToArray()[^length..];
        
        return metrics.Select(x => x.ToString()).ToArray();
    }
}

public sealed partial class KLineTools
{
    private static Func<DateTime, int> GetMergeFunc(KLineType klineType)
    {
        Func<DateTime, int> mergeFn = klineType switch
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
        
        return mergeFn;
    }
    
    private static KLine[] MergeKLines(KLine[] klines, Func<DateTime, int> mergeFunc)
    {
        var result = new List<KLine>(klines.Length);
        
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
    
    private async Task<IMongoCollection<KLine>> CheckAndUpdate(string code)
    {
        var collection = database.GetCollection<KLine>(code);
        
        var date = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        
        var dataResult = await collection.Find(Builders<KLine>.Filter.Empty)
            .Sort(Builders<KLine>.Sort.Descending(x => x.Date))
            .Limit(2)
            .ToListAsync();
        
        if (dataResult.Count == 2)
        {
            var kDate = dataResult[0].Date;
            var updateDate = dataResult[0].UpdateDate;
            if (updateDate - TimeSpan.FromHours(15) > kDate) 
                if ((date - updateDate).Hours < 24)
                    return collection;
        }

        await database.DropCollectionAsync(code);
        
        var klines = await AllKlineData(code, date);
        await collection.InsertManyAsync(klines);
        return collection;
    }
}

public sealed partial class KLineTools
{
    private readonly HttpClient _httpClient = factory.CreateClient("push2his.eastmoney.com");

    private async Task<IEnumerable<KLine>> KlineData(QueryBuilder query)
    {
        var uri = "/api/qt/stock/kline/get" + query;
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var userAgent = UserAgent.Generate(Browser.Chrome, Platform.Desktop) ?? "";
        request.Headers.Add("User-Agent", userAgent);
        request.Headers.Add("Referer", "https://quote.eastmoney.com/");
        
        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var text = await response.Content.ReadAsStringAsync();
        var res = JsonConvert.DeserializeObject<Response1>(text);
        if (res is null) throw new Exception(text);

        return from val in res.Data?.Lines ?? [] select KLine.FromEastmoney(val);
    }
    
    private async Task<IEnumerable<KLine>> KlineData(string code, DateTime end, int length)
    {
        var bourse = StockHelper.GetStockBourse(code);
        
        var query = new QueryBuilder
        {
            { "secid", bourse.GetLastValue<string>("code") + "." + code },
            { "fields1", "f1,f3" },
            { "fields2", "f51,f52,f53,f54,f55,f56" },
            { "klt", "101" },
            { "fqt", "1" },
            { "end", end.ToString("yyyyMMdd") },
            { "lmt", length.ToString() }
        };
        
        return await KlineData(query);
    }

    private async Task<IEnumerable<KLine>> AllKlineData(string code, DateTime end)
    {
        var bourse = StockHelper.GetStockBourse(code);
        
        var query = new QueryBuilder
        {
            { "secid", bourse.GetLastValue<string>("code") + "." + code },
            { "fields1", "f1,f3" },
            { "fields2", "f51,f52,f53,f54,f55,f56" },
            { "klt", "101" },
            { "fqt", "1" },
            { "beg", "0" },
            { "end", end.ToString("yyyyMMdd") },
        };
        
        return await KlineData(query);
    }
}
