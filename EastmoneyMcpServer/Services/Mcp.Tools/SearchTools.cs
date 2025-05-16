using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EastmoneyMcpServer.Models.Response;
using Microsoft.AspNetCore.Http.Extensions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace EastmoneyMcpServer.Services.Mcp.Tools;

[McpServerToolType]
public sealed class SearchTools(IHttpClientFactory httpClientFactory, ILogger<SearchTools> logger)
{
    [McpServerTool(Name = "search_stock", Title = "搜索股票")]
    [Description("搜索股票 返回格式: 类型,股票代码,企业名称")]
    public async Task<IEnumerable<string>> SearchStock(
        [Description("关键词")]
        [Required]
        string keyword, 
        
        [Description("查询数量 默认查询5个")]
        [Range(1, 10)]
        int size = 5,
        
        CancellationToken token = default)
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
        try
        {
            using var response = await client.GetAsync("/codetable/search/web" + query, token);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync(token);
            var res = JsonSerializer.Deserialize<Response2>(text);
            if (res is null) throw new Exception(text);
            return res.Result.Select(x => x.ToString());
        }
        catch (Exception e)
        {
            logger.LogError(e, "查询股票时出错");
            throw new McpException("查询股票时出错", McpErrorCode.InternalError);
        }
    }
}
