namespace EastmoneyMcpServer.Models.Settings;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class KLineSettings
{
    [ConfigurationKeyName("CacheTime")]
    public required int RawCacheTime { get; init; }
    
    public TimeSpan CacheTime => TimeSpan.FromMilliseconds(RawCacheTime);
}
