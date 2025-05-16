using System.ComponentModel;
using System.Text.Json.Serialization;

namespace EastmoneyMcpServer.Models.Response;

/// <summary>
/// https://search-codetable.eastmoney.com/codetable/search/web
/// response
/// </summary>
public sealed class Response2
{
    public struct Item
    {
        [JsonPropertyName("code")]
        public string Code{ get; init; }
        
        [JsonPropertyName("shortName")]
        public string Name { get; init; }
        
        [JsonPropertyName("securityTypeName")]
        public string SecurityTypeName{ get; init; }

        public override string ToString()
        {
            return $"{SecurityTypeName},{Code},{Name}";
        }
    }

    [JsonPropertyName("result")] 
    public required Item[] Result { get; init; }
}
