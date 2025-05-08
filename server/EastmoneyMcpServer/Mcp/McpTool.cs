using System.Reflection;
using ModelContextProtocol.Server;

namespace EastmoneyMcpServer.Mcp;

public sealed class McpTool
{
    public MethodInfo Info { get; init; }
    public McpServerTool Tool { get; init; }
    
    public McpTool(MethodInfo method, McpServerToolCreateOptions options, Type? type)
    {
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (type is null) Tool = McpServerTool.Create(method, options: options);
        else Tool = McpServerTool.Create(method, type, options);
        Info = method;
    }
}
