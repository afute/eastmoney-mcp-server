using System.Reflection;
using EastmoneyMcpServer.Attributes;
using EastmoneyMcpServer.Interfaces;

namespace EastmoneyMcpServer.Models;

public abstract class McpIndicators : IMcpToolCallResult
{
    public required DateTime Date { get; init; }

    public string ToMcpResult()
    {
        var date = Date.ToString("yyyy-MM-dd");
        var properties = from propertyInfo in GetType().GetProperties()
            let attr = propertyInfo.GetCustomAttribute<McpToolCallResultAttribute>()
            where attr != null
            select (attr.Alias ?? propertyInfo.Name, (decimal)propertyInfo.GetValue(this));
        var result = from property in properties
            select property.Item1 + "=" + Math.Round(property.Item2, 3);
        return $"{date},{string.Join(" ", result)}";
    }
    
    public override string ToString() => ToMcpResult();
}
