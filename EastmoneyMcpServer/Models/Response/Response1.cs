using System.Text.Json.Serialization;

namespace EastmoneyMcpServer.Models.Response;

/// <summary>
/// https://push2his.eastmoney.com/api/qt/stock/kline/get
/// response
/// </summary>
public class Response1
{
    public struct ResponseData
    {
        [JsonPropertyName("code")]
        public string Code { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; }
        
        // ReSharper disable once UnassignedField.Global
        [JsonPropertyName("dktotal")]
        public int DkTotal { get; init; }
        
        // ReSharper disable once UnassignedField.Global
        [JsonPropertyName("klines")]
        public string[] Lines { get; init; }
    }
    
    [JsonPropertyName("data")]
    public ResponseData? Data { get; init; }
}
