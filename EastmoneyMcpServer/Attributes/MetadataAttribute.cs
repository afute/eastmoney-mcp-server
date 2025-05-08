using System.Reflection;

namespace EastmoneyMcpServer.Attributes;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class MetadataAttribute<T>(string key, T value) : Attribute
{
    public readonly string Key = key;
    public readonly T Value = value;
}

public static class MetadataEnumExtension
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

    public static T GetLastValue<T>(this Enum target, string key)
    {
        var result = target.GetValue<T>(key);
        return result[^1];
    }

    public static bool TryGetValue<T>(this Enum target, string key, out T[] result)
    {
        try
        {
            result = target.GetValue<T>(key);
            return result.Length != 0;
        }
        catch
        {
            result = [];
            return false;
        }
    }
}
