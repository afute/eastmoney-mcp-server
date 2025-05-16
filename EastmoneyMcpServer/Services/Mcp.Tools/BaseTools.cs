using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace EastmoneyMcpServer.Services.Mcp.Tools;

[McpServerToolType]
public sealed class BaseTools
{
    [McpServerTool(Name = "get_now_datetime", Title = "获取当前时间")]
    [Description("获取当前北京时间 返回格式为 yyyy-MM-dd HH:mm:ss")]
    public static string GetNowDateTime()
    {
        var date = DateTime.UtcNow;
        date = date.AddHours(8);
        return date.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
