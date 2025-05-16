namespace EastmoneyMcpServer.Attributes;

/// <summary>
/// mcp工具返回如果为类转字符串, 用于标记哪些字段和属性需要被格式化
/// </summary>
/// <param name="alias"></param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class McpToolCallResultAttribute(string? alias = null) : Attribute
{
    public string? Alias { get; } = alias;
}
