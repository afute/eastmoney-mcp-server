using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;

namespace EastmoneyMcpServer.Mcp;

using Context1 = RequestContext<ListToolsRequestParams>;
using Context2 = RequestContext<CallToolRequestParams>;

public sealed class McpServiceCollection : ServiceCollection
{
    private readonly IServiceProvider _provider;

    public McpServiceCollection()
    {
        var toolAssembly = Assembly.GetCallingAssembly();

        var toolTypes = from type in toolAssembly.GetTypes()
            let attr = type.GetCustomAttribute<McpServerToolTypeAttribute>()
            where attr != null && (object)type is not null
            select type;

        foreach (var toolType in toolTypes)
        foreach (var method in toolType.GetMethods())
            WithTool(toolType, method);
        _provider = this.BuildServiceProvider();
    }

    private void WithTool(Type toolType, MethodInfo method)
    {
        if (method.GetCustomAttribute<McpServerToolAttribute>() is null) return;
        var target = method.IsStatic ? null : toolType;

        this.AddSingleton((Func<IServiceProvider, McpTool>)Tool);
        return;

        McpTool Tool(IServiceProvider services)
        {
            var options = new McpServerToolCreateOptions { Services = services, SerializerOptions = null };
            return new McpTool(method, options, target);
        }
    }

    private ValueTask<ListToolsResult> ListToolsHandler(Context1 context, CancellationToken _)
    {
        var tools = _provider.GetServices<McpTool>().ToList();
        var generator = new JSchemaGenerator();
        foreach (var tool in tools)
        {
            var rawDescription = tool.Tool.ProtocolTool.Description ?? "";
            var returnParameter = tool.Info.ReturnParameter;
            var descriptionAttr = returnParameter.GetCustomAttribute<DescriptionAttribute>();
            var returnType = tool.Info.ReturnType;
            if (returnType.IsGenericType)
            {
                if (returnType.GetGenericTypeDefinition() != typeof(Task<>))
                    continue;
                returnType = returnType.GetGenericArguments()[0];
            }
            var jsonSchema = generator.Generate(returnType);
            jsonSchema.Description = descriptionAttr?.Description;
            var resultSchemaString = JsonConvert.SerializeObject(jsonSchema, Formatting.Indented);
            var schema = "return schema:\r\n" + resultSchemaString;
            tool.Tool.ProtocolTool.Description = string.Join("\r\n\r\n", rawDescription, schema);
        }
        var result = tools.Select(t => t.Tool.ProtocolTool).ToList();
        return ValueTask.FromResult(new ListToolsResult { Tools = result });
    }
    
    private async ValueTask<CallToolResponse> CallToolHandler(Context2 context, CancellationToken token)
    {
        var targetToolName = context.Params?.Name ?? throw new Exception("tool name is empty");
        var method = (from tool in _provider.GetServices<McpTool>()
            where tool.Tool.ProtocolTool.Name == targetToolName
            select tool).FirstOrDefault() ?? throw new Exception($"{targetToolName} tool not found");
        return await method.Tool.InvokeAsync(context, token);
    }

    public void RegisterAspService(IServiceCollection service)
    {
        var mcpServerBuilder = service.AddMcpServer();
        mcpServerBuilder.WithHttpTransport();
        mcpServerBuilder.WithListToolsHandler(ListToolsHandler);
        mcpServerBuilder.WithCallToolHandler(CallToolHandler);
    }
}
