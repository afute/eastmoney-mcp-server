using System.Net;

namespace EastmoneyMcpServer.Models.Settings;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class HttpSettings
{
    [ConfigurationKeyName("Timeout")]
    public required int RawTimeout { get; init; }
    
    [ConfigurationKeyName("Proxy")]
    public required string? RawProxy { get; init; }

    public TimeSpan Timeout => TimeSpan.FromMilliseconds(RawTimeout);
}
