using System.Reflection;

namespace EastmoneyMcpServer.Models.Attributes;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class MetadataAttribute<T>(string key, T value) : Attribute
{
    public readonly string Key = key;
    public readonly T Value = value;
}

public static class MetadataEnumExtension
{
    public static T[] GetValues<T>(this Enum target, string key)
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
        var values = target.GetValues<T>(key);
        if (values.Length == 0) throw new ArgumentNullException(key, "value not found");
        return values[0];
    }
}
