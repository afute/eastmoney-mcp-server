using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EastmoneyMcpServer.Models;

public readonly struct KLine
{
    [BsonId]
    public ObjectId Id { get; init; }
    
    [BsonElement("update-date")]
    public DateTime UpdateDate { get; init; }
    
    /// <summary>
    /// 直接把本地时间标记为utc时间
    /// </summary>
    [BsonElement("date")]
    public required DateTime Date { get; init; }
    
    /// <summary>
    /// 开盘价
    /// </summary>
    [BsonElement("open")]
    public required double Open { get; init; }
    
    /// <summary>
    /// 收盘价
    /// </summary>
    [BsonElement("close")]
    public required double Close { get; init; }
    
    /// <summary>
    /// 最高价
    /// </summary>
    [BsonElement("high")]
    public required double High { get; init; }
    
    /// <summary>
    /// 最低价
    /// </summary>
    [BsonElement("low")]
    public required double Low { get; init; }
    
    /// <summary>
    /// 成交量
    /// </summary>
    [BsonElement("volume")]
    public required long Volume { get; init; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static KLine FromEastmoney(string data)
    {
        var splits = data.Split(",");
        
        var info = CultureInfo.InvariantCulture;
        const string format = "yyyy-MM-dd";
        var date = DateTime.ParseExact(splits[0], format, info);
        date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        var nowDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        
        var kline = new KLine
        {
            Date = date,
            Open = double.Parse(splits[1]),
            Close = double.Parse(splits[2]),
            High = double.Parse(splits[3]),
            Low = double.Parse(splits[4]),
            Volume = long.Parse(splits[5]),
            
            UpdateDate = nowDate,
            Id = ObjectId.GenerateNewId()
        };
        
        return kline;
    }
    
    public static explicit operator KLine(string date) => FromEastmoney(date);

    public override string ToString()
    {
        var date = Date.ToString("yyyy-MM-dd");
        return $"{date},{Open},{Close},{High},{Low},{Volume}";
    }

    public KLine Merge(KLine other)
    {
        return new KLine
        {
            Date = other.Date,
            Open = Open,
            Close = other.Close,
            High = double.Max(High, other.High),
            Low = double.Min(Low, other.Low),
            Volume = Volume + other.Volume
        };
    }
}
