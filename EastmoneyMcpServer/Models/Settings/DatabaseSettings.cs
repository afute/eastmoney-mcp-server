namespace EastmoneyMcpServer.Models.Settings;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DatabaseSettings
{
    [ConfigurationKeyName("Connection")]
    public required string Connection { get; init; }
}
