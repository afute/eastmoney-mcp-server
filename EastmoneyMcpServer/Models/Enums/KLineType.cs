using System.Text.Json.Serialization;
using EastmoneyMcpServer.Models.Attributes;
using ModelContextProtocol.Utils.Json;

namespace EastmoneyMcpServer.Models.Enums;

[JsonConverter(typeof(CustomizableJsonStringEnumConverter<KLineType>))]
public enum KLineType
{
    [JsonStringEnumMemberName("day")]
    [Metadata<string>("klt", "101")]
    Day = 1,
    
    [JsonStringEnumMemberName("week")]
    [Metadata<string>("klt", "102")]
    Week = 2,
    
    [JsonStringEnumMemberName("month")]
    [Metadata<string>("klt", "103")]
    Month = 3
}
