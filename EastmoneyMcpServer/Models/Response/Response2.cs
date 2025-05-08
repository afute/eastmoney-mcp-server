using System.ComponentModel;
using Newtonsoft.Json;

namespace EastmoneyMcpServer.Models.Response;

[Description("https://search-codetable.eastmoney.com/codetable/search/web")]
public class Response2
{
    public struct Item
    {
        [JsonProperty("code")]
        public string Code;
        
        [JsonProperty("shortName")]
        public string Name;
        
        [JsonProperty("securityTypeName")]
        public string SecurityTypeName;

        public override string ToString()
        {
            return $"{SecurityTypeName},{Code},{Name}";
        }
    }

    [JsonProperty("result")] 
    public required Item[] Result;
}
