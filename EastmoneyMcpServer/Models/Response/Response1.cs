using System.ComponentModel;
using Newtonsoft.Json;

namespace EastmoneyMcpServer.Models.Response;

[Description("https://push2his.eastmoney.com/api/qt/stock/kline/get")]
public class Response1
{
    public struct ResponseData
    {
        [JsonProperty("code")]
        public string Code;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("klines")]
        public string[]Lines;
    }
    
    [JsonProperty("data")]
    public ResponseData? Data;
}
