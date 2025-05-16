using System.Globalization;
using EastmoneyMcpServer.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EastmoneyMcpServer.Models;

public sealed class StockKLine : IMcpToolCallResult
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    
    /// <summary>
    /// 直接把本地时间标记为utc时间
    /// </summary>
    [BsonElement("date")]
    public required DateTime Date { get; init; }
    
    /// <summary>
    /// 开盘价
    /// </summary>
    [BsonElement("open")]
    [BsonRepresentation(BsonType.Decimal128)]
    public required decimal Open { get; init; }
    
    /// <summary>
    /// 收盘价
    /// </summary>
    [BsonElement("close")]
    [BsonRepresentation(BsonType.Decimal128)]
    public required decimal Close { get; init; }
    
    /// <summary>
    /// 最高价
    /// </summary>
    [BsonElement("high")]
    [BsonRepresentation(BsonType.Decimal128)]
    public required decimal High { get; init; }
    
    /// <summary>
    /// 最低价
    /// </summary>
    [BsonElement("low")]
    [BsonRepresentation(BsonType.Decimal128)]
    public required decimal Low { get; init; }
    
    /// <summary>
    /// 成交量
    /// </summary>
    [BsonElement("volume")]
    [BsonRepresentation(BsonType.Decimal128)]
    public required decimal Volume { get; init; }
    
    [BsonElement("turnover")]
    [BsonRepresentation(BsonType.Decimal128)]
    public required decimal Turnover { get; init; }
    
    [BsonElement("turnover_rate")]
    [BsonRepresentation(BsonType.Decimal128)]
    public required decimal TurnoverRate { get; init; }
    
    public static StockKLine Create(string data)
    {
        var splits = data.Split(",");
        
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyy-MM-dd HH:mm";
        var dateString = splits[0];
        if (dateString.Length == 10) dateString += " 00:00";
        var date = DateTime.ParseExact(dateString, format, info);
        date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        
        var kline = new StockKLine
        {
            Date = date,
            Open = decimal.Parse(splits[1]),
            Close = decimal.Parse(splits[2]),
            High = decimal.Parse(splits[3]),
            Low = decimal.Parse(splits[4]),
            Volume = decimal.Parse(splits[5]),
            Turnover = decimal.Parse(splits[6]),
            TurnoverRate = decimal.Parse(splits[7])
        };
        
        return kline;
    }
    
    public StockKLine Merge(StockKLine other)
    {
        return new StockKLine
        {
            Date = other.Date,
            Open = Open,
            Close = other.Close,
            High = decimal.Max(High, other.High),
            Low = decimal.Min(Low, other.Low),
            Volume = Volume + other.Volume,
            Turnover = Turnover + other.Turnover,
            TurnoverRate = TurnoverRate + other.TurnoverRate
        };
    }

    public string ToMcpResult()
    {
        var date = Date.ToString("yyyy-MM-dd");
        return $"{date},{Open},{Close},{High},{Low},{Volume},{Turnover},{TurnoverRate}%";
    }
}
