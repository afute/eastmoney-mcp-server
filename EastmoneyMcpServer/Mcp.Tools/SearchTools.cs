using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EastmoneyMcpServer.Models.Response;
using Microsoft.AspNetCore.Http.Extensions;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using UserAgentGenerator;

namespace EastmoneyMcpServer.Mcp.Tools;

/// <summary>
/// 搜索工具
/// </summary>
[McpServerToolType]
public sealed class SearchTools(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(Name = "SearchStockFromKeyword", Title = "搜索股票")]
    [Description("")]
    [return: Description("类型,股票代码,企业名称")]
    public async Task<IEnumerable<string>> SearchStockFromKeyword(
        [Description("关键词")]
        [Required]
        string keyword,
        
        [Description("查询数量 默认查询5个")]
        [Range(1, 10)]
        int size = 5)
    {
        var client = httpClientFactory.CreateClient("search-codetable.eastmoney.com");

        var query = new QueryBuilder
        {
            { "client", "web" },
            { "clientType", "webSuggest" },
            { "clientVersion", "lastest" },
            { "keyword", keyword },
            { "pageIndex", "1" },
            { "pageSize", size.ToString() },
            { "securityFilter", "" }
        };

        var uri = "/codetable/search/web" + query;
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var userAgent = UserAgent.Generate(Browser.Chrome, Platform.Desktop) ?? "";
        request.Headers.Add("User-Agent", userAgent);
        request.Headers.Add("Referer", "https://www.eastmoney.com/");
        
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var text = await response.Content.ReadAsStringAsync();
        var res = JsonConvert.DeserializeObject<Response2>(text);
        if (res is null) throw new Exception(text);
        return res.Result.Select(x => x.ToString());
    }
}
