using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Unicode;
using EastmoneyMcpServer.Models.Settings;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using MongoDB.Driver;
using UserAgentGenerator;

namespace EastmoneyMcpServer;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<HttpSettings>(builder.Configuration.GetSection("Http"));
        builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
        builder.Services.Configure<KLineSettings>(builder.Configuration.GetSection("KLine"));
        
        builder.Services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<DatabaseSettings>>();
            var client = new MongoClient(settings.Value.Connection);
            return client;
        });
        
        builder.Services.AddLazyCache();
        builder.Services.AddControllers();
        builder.Services.AddHttp();
        builder.Services.AddMcp();
        
        var app = builder.Build();

        app.UseDefaultFiles();
        app.MapStaticAssets();

        app.MapControllers();
        app.MapFallbackToFile("/index.html");

        app.MapMcp();

        await app.RunAsync();
    }
}

#region httpclient

public static partial class Program
{
    private static void AddHttp(this IServiceCollection service)
    {
        service.AddHttpClient("push2his.eastmoney.com", (provider, client) =>
        {
            var userAgent = UserAgent.Generate(Browser.Chrome, Platform.Desktop) ?? "";
            var settings = provider.GetRequiredService<IOptions<HttpSettings>>();
            client.BaseAddress = new Uri("https://push2his.eastmoney.com/");
            client.Timeout = settings.Value.Timeout;
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Referer", "https://quote.eastmoney.com/");
        }).ConfigurePrimaryHttpMessageHandler(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<HttpSettings>>();
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = !string.IsNullOrEmpty(settings.Value.RawProxy)
            };
            if (handler.UseProxy)
                handler.Proxy = new WebProxy(settings.Value.RawProxy);
            return handler;
        });
        
        // add cookie container
        // 后续交易工具需要
        service.AddSingleton<CookieContainer>(_ => new CookieContainer());
    }
}

#endregion

#region mcp
public static partial class Program
{
    /// <summary>
    /// 添加工具
    /// </summary>
    /// <param name="service"></param>
    /// <param name="toolType"></param>
    /// <param name="method"></param>
    private static void WithTool(this IServiceCollection service, Type toolType, MethodInfo method)
    {
        if (method.GetCustomAttribute<McpServerToolAttribute>() is null) return;
        
        Func<IServiceProvider, McpServerTool> tool;
        
        if (method.IsStatic)
        {
            tool = provider =>
            {
                var options = new McpServerToolCreateOptions
                {
                    Services = provider,
                    SerializerOptions = null
                };
                var mcpTool = McpServerTool.Create(method, options: options)
                    .AddMcpServerToolDescription(method);
                return mcpTool;
            };
        }
        else
        {
            tool = provider =>
            {
                var options = new McpServerToolCreateOptions
                {
                    Services = provider,
                    SerializerOptions = null
                };
                var mcpTool = McpServerTool.Create(method, toolType, options)
                    .AddMcpServerToolDescription(method);
                return mcpTool;
            };
        }
        
        service.AddSingleton(tool);
    }

    private static readonly JsonSerializerOptions SchemaSerializerOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private static McpServerTool AddMcpServerToolDescription(this McpServerTool tool, MethodInfo method)
    {
        var generator = JsonSerializerOptions.Default;
        var generatorOptions = new JsonSchemaExporterOptions { TransformSchemaNode = TransformSchemaFunc};

        var rawDescription = tool.ProtocolTool.Description ?? "";
        var returnParameter = method.ReturnParameter;
        var descriptionAttr = returnParameter.GetCustomAttribute<DescriptionAttribute>();
        var returnType = method.ReturnType;
        if (returnType.IsGenericType)
        {
            if (returnType.GetGenericTypeDefinition() != typeof(Task<>))
                return tool;
            returnType = returnType.GetGenericArguments()[0];
        }

        var schema = generator.GetJsonSchemaAsNode(returnType, generatorOptions);
        if (schema is not JsonObject jObj) return tool;
        if (!string.IsNullOrEmpty(descriptionAttr?.Description))
        {
            var description = descriptionAttr.Description;
            jObj.Insert(0, "description", description);
        }
        var result = "return schema:\r\n" + JsonSerializer.Serialize(schema, SchemaSerializerOptions);
        tool.ProtocolTool.Description = string.Join("\r\n\r\n", rawDescription, result);
        return tool;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="service"></param>
    /// <param name="assembly"></param>
    /// <returns></returns>
    private static IMcpServerBuilder AddMcp(this IServiceCollection service, Assembly? assembly = null)
    {
        var toolAssembly = assembly ?? Assembly.GetCallingAssembly();
        
        var toolTypes = from type in toolAssembly.GetTypes()
            let attr = type.GetCustomAttribute<McpServerToolTypeAttribute>()
            where attr != null
            select type;
        
        foreach (var toolType in toolTypes)
        foreach (var method in toolType.GetMethods())
            service.WithTool(toolType, method);
        
        var mcpServerBuilder = service.AddMcpServer();
        mcpServerBuilder.WithHttpTransport();
        mcpServerBuilder.WithListToolsHandler(McpListToolsHandler);
        mcpServerBuilder.WithCallToolHandler(McpCallToolHandler);
        return mcpServerBuilder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    private static JsonNode TransformSchemaFunc(JsonSchemaExporterContext context, JsonNode schema)
    {
        var attributeProvider = context.PropertyInfo is not null
            ? context.PropertyInfo.AttributeProvider
            : context.TypeInfo.Type;
        var descriptionAttr = attributeProvider?
            .GetCustomAttributes(inherit: true)
            .Select(attr => attr as DescriptionAttribute)
            .FirstOrDefault(attr => attr is not null);
        if (descriptionAttr == null || schema is not JsonObject jObj) return schema;
        jObj.Insert(0, "description", descriptionAttr.Description);
        return schema;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="_"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    private static ValueTask<ListToolsResult> McpListToolsHandler(
        RequestContext<ListToolsRequestParams> context, CancellationToken _)
    {
        var services = context.Services ?? throw new NullReferenceException("services is null");
        var mcpTools = services.GetServices<McpServerTool>().ToList();
        var toolsResult = mcpTools.Select(t => t.ProtocolTool).ToList();
        return ValueTask.FromResult(new ListToolsResult { Tools = toolsResult });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="Exception"></exception>
    private static async ValueTask<CallToolResponse> McpCallToolHandler(
        RequestContext<CallToolRequestParams> context, CancellationToken token)
    {
        var services = context.Services ?? throw new NullReferenceException("services is null");
        var targetToolName = context.Params?.Name ?? throw new Exception("tool name is empty");
        var method = (from tool in services.GetServices<McpServerTool>()
            where tool.ProtocolTool.Name == targetToolName
            select tool).FirstOrDefault() ?? throw new Exception($"{targetToolName} tool not found");
        return await method.InvokeAsync(context, token).ConfigureAwait(false);
    }
}
#endregion
