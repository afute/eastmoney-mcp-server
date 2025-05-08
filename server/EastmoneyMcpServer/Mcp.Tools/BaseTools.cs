using System.ComponentModel;
using ModelContextProtocol.Server;

namespace EastmoneyMcpServer.Mcp.Tools;

[McpServerToolType]
public sealed class BaseTools
{
    [McpServerTool(Name = "GetNowDateTimeFoo", Title = "获取当前时间")]
    [Description("获取当前时间")]
    [return: Description("yyyy-MM-dd HH:mm:ss")]
    public static string GetNowDateTime()
    {
        var date = DateTime.Now;
        return date.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
