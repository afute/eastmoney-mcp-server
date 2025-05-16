using System.Net;

namespace EastmoneyMcpServer.Models.Settings;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class HttpSettings
{
    [ConfigurationKeyName("Timeout")]
    public required int RawTimeout { get; init; }
    
    [ConfigurationKeyName("Proxy")]
    public required string? RawProxy { get; init; }

    /// <summary>
    /// 超时时间 单位为毫秒
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromMilliseconds(RawTimeout);
    
    /// <summary>
    /// 代理
    /// </summary>
    public IWebProxy? Proxy => RawProxy is null ? null : new WebProxy(RawProxy);
}
