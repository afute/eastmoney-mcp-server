namespace EastmoneyMcpServer.Models.Settings;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DatabaseSettings
{
    /// <summary>
    /// 数据库连接地址
    /// </summary>
    [ConfigurationKeyName("Connection")]
    public required string Connection { get; init; }
    
    [ConfigurationKeyName("KLineCacheTime")]
    public required int RawKLineCacheTime { get; init; }
    
    /// <summary>
    /// K线缓存数据库名
    /// </summary>
    [ConfigurationKeyName("KLineCacheDatabaseName")]
    public required string KLineCacheDatabaseName { get; init; }
    
    /// <summary>
    /// K线缓存时间
    /// </summary>
    public TimeSpan KLineCacheTime => TimeSpan.FromMilliseconds(RawKLineCacheTime);
}
