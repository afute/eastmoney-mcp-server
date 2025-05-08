using System.Text.Json.Serialization;

namespace EastmoneyMcpServer.Models;

public sealed class AppSettings
{
    [JsonPropertyName("DatabaseConnection")]
    public required string DatabaseConnection { get; init; }
}
