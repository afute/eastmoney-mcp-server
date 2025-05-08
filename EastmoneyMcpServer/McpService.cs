using System.ComponentModel;
using System.Reflection;
using JetBrains.Annotations;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;

namespace EastmoneyMcpServer;

using Context1 = RequestContext<ListToolsRequestParams>;
using Context2 = RequestContext<CallToolRequestParams>;

public static class McpService
{
    [UsedImplicitly]
    private sealed record McpTool(MethodInfo Method, McpServerTool Tool);
    
    private static void WithTool(this IServiceCollection services, Type toolType, MethodInfo method)
    {
        if (method.GetCustomAttribute<McpServerToolAttribute>() is null) return;
        
        Func<IServiceProvider, McpTool> tool;
        
        if (method.IsStatic)
        {
            tool = service =>
            {
                var options = new McpServerToolCreateOptions
                {
                    Services = service,
                    SerializerOptions = null
                };
                var mcpTool = McpServerTool.Create(method, options: options);
                return new McpTool(method, mcpTool);
            };
        }
        else
        {
            tool = service =>
            {
                var options = new McpServerToolCreateOptions
                {
                    Services = service,
                    SerializerOptions = null
                };
                var mcpTool = McpServerTool.Create(method, toolType, options);
                return new McpTool(method, mcpTool);
            };
        }
        
        services.AddSingleton(tool);
    }
    
    public static void AddMcpService(this IServiceCollection services)
    {
        var toolAssembly = Assembly.GetCallingAssembly();
        
        var toolTypes = from type in toolAssembly.GetTypes()
            let attr = type.GetCustomAttribute<McpServerToolTypeAttribute>()
            where attr != null
            select type;
        
        foreach (var toolType in toolTypes)
        foreach (var method in toolType.GetMethods())
            services.WithTool(toolType, method);
        
        var mcpServerBuilder = services.AddMcpServer();
        mcpServerBuilder.WithHttpTransport();
        mcpServerBuilder.WithListToolsHandler(McpListToolsHandler);
        mcpServerBuilder.WithCallToolHandler(McpCallToolHandler);
    }
    
        private static ValueTask<ListToolsResult> McpListToolsHandler(Context1 context, CancellationToken _)
    {
        var services = context.Services ?? throw new NullReferenceException("services is null");
        var mcpTools = services.GetServices<McpTool>().ToList();
        var generator = new JSchemaGenerator();
        foreach (var mcpTool in mcpTools)
        {
            var rawDescription = mcpTool.Tool.ProtocolTool.Description ?? "";
            var returnParameter = mcpTool.Method.ReturnParameter;
            var descriptionAttr = returnParameter.GetCustomAttribute<DescriptionAttribute>();
            var returnType = mcpTool.Method.ReturnType;
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
            mcpTool.Tool.ProtocolTool.Description = string.Join("\r\n\r\n", rawDescription, schema);
        }

        var result = mcpTools.Select(t => t.Tool.ProtocolTool).ToList();
        return ValueTask.FromResult(new ListToolsResult { Tools = result });
    }
    
    private static async ValueTask<CallToolResponse> McpCallToolHandler(Context2 context, CancellationToken token)
    {
        var services = context.Services ?? throw new NullReferenceException("services is null");
        var targetToolName = context.Params?.Name ?? throw new Exception("tool name is empty");
        var method = (from tool in services.GetServices<McpTool>()
            where tool.Tool.ProtocolTool.Name == targetToolName
            select tool).FirstOrDefault() ?? throw new Exception($"{targetToolName} tool not found");
        return await method.Tool.InvokeAsync(context, token).ConfigureAwait(false);
    }
}
