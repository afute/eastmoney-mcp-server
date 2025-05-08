using System.Text.Json.Serialization;
using EastmoneyMcpServer.Attributes;
using ModelContextProtocol.Utils.Json;

namespace EastmoneyMcpServer.Models.Enums;

[JsonConverter(typeof(CustomizableJsonStringEnumConverter<KLineMetricsType>))]
public enum KLineMetricsType
{
    [JsonStringEnumMemberName("cci")]
    [Metadata<string>("format", "cci")]
    Cci = 1,
    
    [JsonStringEnumMemberName("kdj")]
    [Metadata<string>("format", "K,D,J")]
    Kdj = 2,
    
    [JsonStringEnumMemberName("macd")]
    [Metadata<string>("format", "DIF,DEA,MACD")]
    Macd = 3,
    
    [JsonStringEnumMemberName("roc")]
    [Metadata<string>("format", "ROC,MAROC")]
    Roc = 4,
    
    [JsonStringEnumMemberName("rsi")]
    [Metadata<string>("format", "RSI1,RSI2,RSI3")]
    Rsi = 5
}
