using System.Reflection;

namespace EastmoneyMcpServer.Attributes;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class MetadataAttribute<T>(string key, T value) : Attribute
{
    public readonly string Key = key;
    public readonly T Value = value;
}
