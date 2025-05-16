using System.Text.Json;
using EastmoneyMcpServer.Extensions;
using EastmoneyMcpServer.Interfaces;
using EastmoneyMcpServer.Models;
using EastmoneyMcpServer.Models.Enums;
using EastmoneyMcpServer.Models.Response;
using LazyCache;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EastmoneyMcpServer.Services.Instances;

public sealed class KLineInstance(
    IServiceProvider serviceProvider,
    IHttpClientFactory clientFactory,
    IMongoClient mongoClient,
    ILogger<KLineInstance> logger,
    IAppCache appCache
    ) : IKLineInstance
{
    private readonly HttpClient _httpClient = clientFactory.CreateClient("push2his.eastmoney.com");
    
    /// <summary>
    /// 检查K线缓存是否存在, 不存在则更新
    /// </summary>
    /// <param name="code"></param>
    /// <param name="adjustedType"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<IMongoCollection<StockKLine>> GetCollectionFromCheck(string code, AdjustedType adjustedType, 
        CancellationToken token)
    {
        var options = serviceProvider.GetRequiredService<IOptions<AppSettings>>();
        var databaseName = options.Value.Database.KLineCacheDatabaseName;
        var collectionId = code + "." + adjustedType.GetRequiredValue<string>("code");
        var task = await appCache.GetOrAddAsync(collectionId, async entry =>
        {
            var database = mongoClient.GetDatabase(databaseName);
            var collection = database.GetCollection<StockKLine>(collectionId);
            
            // 创建索引
            var indexer = Builders<StockKLine>.IndexKeys.Ascending(k => k.Date);
            var model = new CreateIndexModel<StockKLine>(indexer, new CreateIndexOptions { Unique = true });
            await collection.Indexes.CreateOneAsync(model, cancellationToken: token);
            
            var innerKlines = await collection.Find(Builders<StockKLine>.Filter.Empty)
                .Sort(Builders<StockKLine>.Sort.Descending(k => k.Date))
                .Limit(2)
                .ToListAsync(token);
            var date = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            
            if (innerKlines.Count != 2) goto updateAll;
            var offset = (date - innerKlines[1].Date).Days + 1;
            var latestKlines = (await GetEastmoneyKLineData(code, date, offset, adjustedType, token)).ToArray();
            // 对比是否除权
            var oldFullKline = innerKlines[1]; // 旧的完整K线
            var networkKline = latestKlines.First(k => k.Date == oldFullKline.Date);
            if (oldFullKline.Open == networkKline.Open && oldFullKline.Close == networkKline.Close)
            {
                // 未除权, 拼接最新的K线
                logger.LogInformation("更新部分[{code}]K线至数据库, 更新数量{len}", code, latestKlines.Length);
                var delFilter = Builders<StockKLine>.Filter.Gte(x => x.Date, latestKlines[0].Date);
                await collection.DeleteManyAsync(delFilter, cancellationToken: token);
                await collection.InsertManyAsync(latestKlines, cancellationToken: token);
                goto @return;
            }
            
            updateAll:
            await collection.DeleteManyAsync(Builders<StockKLine>.Filter.Empty,  cancellationToken: token);
            var allKLines = (await GetEastmoneyKLineData(code, date, adjustedType, token)).ToArray();
            logger.LogInformation("更新全部[{code}]K线至数据库, 更新数量{len}", code, allKLines.Length);
            await collection.InsertManyAsync(allKLines,  cancellationToken: token);
            
            @return:
            entry.AbsoluteExpirationRelativeToNow = options.Value.Database.KLineCacheTime;
            return collection;
        });
        return task;
    }
    
    private async Task<IEnumerable<StockKLine>> GetEastmoneyKLineData(QueryBuilder query, CancellationToken token)
    {
        using var response = await _httpClient.GetAsync("/api/qt/stock/kline/get" + query, token);
        response.EnsureSuccessStatusCode();
        
        var text = await response.Content.ReadAsStringAsync(token);
        var res = JsonSerializer.Deserialize<Response1>(text);
        if (res is null) throw new Exception(text);
        return from val in res.Data?.Lines ?? [] select StockKLine.Create(val);
    }
    
    private async Task<IEnumerable<StockKLine>> GetEastmoneyKLineData(string code, DateTime end, 
        AdjustedType adjustedType, CancellationToken token)
    {
        var exchange = code.GetStockExchangeFromString();
        var query = new QueryBuilder
        {
            { "secid", exchange.GetRequiredValue<string>("code") + "." + code },
            { "fields1", "f1,f3,f5" },
            { "fields2", "f51,f52,f53,f54,f55,f56" },
            { "klt", "101" },
            { "fqt", adjustedType.GetRequiredValue<string>("code") },
            { "end", end.ToString("yyyyMMdd") },
            { "beg", "0" }
        };
        return await GetEastmoneyKLineData(query, token);
    }
    
    private async Task<IEnumerable<StockKLine>> GetEastmoneyKLineData(string code, DateTime end, int length, 
        AdjustedType adjustedType, CancellationToken token)
    {
        var exchange = code.GetStockExchangeFromString();
        
        var query = new QueryBuilder
        {
            { "secid", exchange.GetRequiredValue<string>("code") + "." + code },
            { "fields1", "f1,f3,f5" },
            { "fields2", "f51,f52,f53,f54,f55,f56" },
            { "klt", "101" },
            { "fqt", adjustedType.GetRequiredValue<string>("code") },
            { "end", end.ToString("yyyyMMdd") },
            { "lmt", length.ToString() }
        };
        
        return await GetEastmoneyKLineData(query, token);
    }
}
