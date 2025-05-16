using EastmoneyMcpServer.Models.Settings;

namespace EastmoneyMcpServer.Models;

public sealed class AppSettings
{
    [ConfigurationKeyName("Http")]
    public required HttpSettings Http { get; init; }
    
    [ConfigurationKeyName("Database")]
    public required DatabaseSettings Database { get; init; }
}
