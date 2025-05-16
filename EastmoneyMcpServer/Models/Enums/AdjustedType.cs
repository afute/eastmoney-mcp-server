using System.Text.Json.Serialization;
using EastmoneyMcpServer.Attributes;
using ModelContextProtocol;

namespace EastmoneyMcpServer.Models.Enums;

[JsonConverter(typeof(CustomizableJsonStringEnumConverter<AdjustedType>))]
public enum AdjustedType
{
    [JsonStringEnumMemberName("forward")]
    [Metadata<string>("code", "1")]
    Forward = 1,
    
    [JsonStringEnumMemberName("backward")]
    [Metadata<string>("code", "2")]
    Backward = 2,
    
    [JsonStringEnumMemberName("raw")]
    [Metadata<string>("code", "0")]
    Raw = 3
}
