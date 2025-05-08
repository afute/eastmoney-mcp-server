using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using UserAgentGenerator;
using EastmoneyMcpServer.Attributes;
using EastmoneyMcpServer.Helper;
using EastmoneyMcpServer.Models;
using EastmoneyMcpServer.Models.Enums;
using EastmoneyMcpServer.Models.Response;
using Microsoft.AspNetCore.Http.Extensions;
using ModelContextProtocol.Server;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace EastmoneyMcpServer.Mcp.Tools;

/// <summary>
/// k线工具
/// </summary>
[McpServerToolType]
public sealed partial class KLineTools(IHttpClientFactory httpClientFactory, IMongoClient mongoClient)
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
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyyMMdd";
        var end = DateTime.ParseExact(endDate, format, info);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

        var klines = (await GetKlineData(code, end, true)).AsSpan();
        var mergeKlines = MergeKlines(klines, klineType)[^length..];
        return mergeKlines.Select(x => x.ToString());
    }
}

public sealed partial class KLineTools
{
    private async Task<KLine[]> GetKlineData(string code, DateTime endDate, bool update)
    {
        var database = mongoClient.GetDatabase("klines");
        var collection = database.GetCollection<KLine>(code);
        
        try
        {
            var indexer = Builders<KLine>.IndexKeys.Ascending(k => k.Date);
            var model = new CreateIndexModel<KLine>(indexer, new CreateIndexOptions { Unique = true });
            await collection.Indexes.CreateOneAsync(model);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
        // 不检查更新
        if (!update) goto @return;

        var checkKlines = await collection.Find(Builders<KLine>.Filter.Empty)
            .Sort(Builders<KLine>.Sort.Descending(x => x.Date))
            .Limit(2)
            .ToListAsync();
        
        var nowDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        
        if (checkKlines.Count != 2)
        {
            await collection.DeleteManyAsync(Builders<KLine>.Filter.Empty);
            var response = await RequestKlineData(code, nowDate);
            await collection.InsertManyAsync(response);
            goto @return;
        }
        
        var lastUpdateDate = checkKlines[0].UpdateDate - TimeSpan.FromHours(15);
        var lastKlineDate = checkKlines[0].Date;
        var lastFullKLine = lastUpdateDate > lastKlineDate ? checkKlines[0] : checkKlines[^1];
        
        var length = (nowDate - lastFullKLine.Date).Days + 1;
        var paddedKlines = (await RequestKlineData(code, nowDate, length)).ToArray();
        if (paddedKlines[^1].Date == lastFullKLine.Date) goto @return;
        
        var newFullKline = paddedKlines.First(x => x.Date == lastFullKLine.Date);
        if (Math.Abs(newFullKline.Open - lastFullKLine.Open) < 0.00001)
        {
            // 未除权
            var delFilter = Builders<KLine>.Filter.Gte(x => x.Date, paddedKlines[0].Date);
            await collection.DeleteManyAsync(delFilter);
            await collection.InsertManyAsync(paddedKlines);
        }
        else
        {
            // 除权
            await collection.DeleteManyAsync(Builders<KLine>.Filter.Empty);
            var response = await RequestKlineData(code, nowDate);
            await collection.InsertManyAsync(response);
        }
        
        @return:
        var filter = Builders<KLine>.Filter.Lte(x => x.Date, endDate);
        var data = await collection.Find(filter)
            .Sort(Builders<KLine>.Sort.Ascending(x => x.Date))
            .ToListAsync() ?? [];
        return data.ToArray();
    }
}

public sealed partial class KLineTools
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("push2his.eastmoney.com");

    private async Task<IEnumerable<KLine>> RequestKlineData(QueryBuilder query)
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
    
    private async Task<IEnumerable<KLine>> RequestKlineData(string code, DateTime end, int length)
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
        
        return await RequestKlineData(query);
    }

    private async Task<IEnumerable<KLine>> RequestKlineData(string code, DateTime end)
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
        
        return await RequestKlineData(query);
    }
}

#region 合并K线方法
public sealed partial class KLineTools
{
    /// <summary>
    /// 日K线合并其他周期K线
    /// </summary>
    /// <param name="klines"></param>
    /// <param name="klineType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static KLine[] MergeKlines(ReadOnlySpan<KLine> klines, KLineType klineType)
    {
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
#endregion
