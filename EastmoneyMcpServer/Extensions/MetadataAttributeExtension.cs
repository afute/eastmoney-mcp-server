using EastmoneyMcpServer.Attributes;
using System.Reflection;
using ModelContextProtocol;

namespace EastmoneyMcpServer.Extensions;

public static class MetadataAttributeExtension
{
    public static T[] GetValue<T>(this Enum target, string key)
    {
        var field = target.GetType().GetField(target.ToString());
        if (field is null) throw new ArgumentNullException(nameof(target), "field is null");
        var result = (from attr in field.GetCustomAttributes<MetadataAttribute<T>>()
            where attr.Key == key
            select attr.Value).ToArray();
        return result;
    }

    public static T GetRequiredValue<T>(this Enum target, string key)
    {
        var result = target.GetValue<T>(key);
        if (result.Length == 0)
            throw new McpException("metadata attribute not found");
        return result[^1];
    }
}
