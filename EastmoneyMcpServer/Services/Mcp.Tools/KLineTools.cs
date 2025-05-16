using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EastmoneyMcpServer.Extensions;
using EastmoneyMcpServer.Interfaces;
using EastmoneyMcpServer.Models;
using EastmoneyMcpServer.Models.Enums;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using MongoDB.Driver;

namespace EastmoneyMcpServer.Services.Mcp.Tools;

// ReSharper disable once ClassNeverInstantiated.Global
[McpServerToolType]
public sealed class KLineTools(IKLineInstance kLineInstance, ILogger<KLineTools> logger)
{
    [McpServerTool(Name = "get_kline_data", Title = "获取股票K线数据")]
    [Description("获取股票K线数据 返回格式: 日期,开盘价,收盘价,最低价,最高价,成交量")]
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
        KLineType klineType,
        
        [Description("复权类型 ps:参考enum 无特殊情况默认为前复权")]
        [EnumDataType(typeof(AdjustedType))]
        AdjustedType adjustedType = AdjustedType.Forward,
        
        CancellationToken token = default
    )
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
            logger.LogError(e, "更新K线数据至缓存失败");
            throw new McpException("更新K线数据至缓存失败", McpErrorCode.InternalError);
        }
        
        var filter = Builders<StockKLine>.Filter.Lte(x => x.Date, end);
        var result = await collection.Find(filter)
            .Sort(Builders<StockKLine>.Sort.Descending(x => x.Date))
            .Limit(length)
            .ToListAsync(token) ?? [];
        return result.MergeKlines(klineType).Select(k => k.ToMcpResult()).Reverse();
    }
}
