using System.Net;
using EastmoneyMcpServer.Interfaces;
using EastmoneyMcpServer.Models;
using EastmoneyMcpServer.Services.Instances;
using EastmoneyMcpServer.Services.Mcp.Tools;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UserAgentGenerator;

namespace EastmoneyMcpServer;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 添加配置文件
        builder.Services.Configure<AppSettings>(builder.Configuration);
        
        // 添加数据库单例
        builder.Services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>();
            var client = new MongoClient(settings.Value.Database.Connection);
            return client;
        });
        
        builder.Services.AddLazyCache();
        builder.Services.AddControllers();
        
        builder.Services.AddSingleton<IKLineInstance, KLineInstance>();
        
        builder.Services.AddHttpClientCollection();
        
        var mcpServerBuilder = builder.Services.AddMcpServer();
        mcpServerBuilder.WithHttpTransport();
        mcpServerBuilder.WithToolsFromAssembly();

        var app = builder.Build();

        app.UseDefaultFiles();
        app.MapStaticAssets();

        app.MapControllers();
        app.MapFallbackToFile("/index.html");

        app.MapMcp();

        await app.RunAsync();
    }
}

#region 添加所需HttpClient
public static partial class Program
{
    private static void AddHttpClientCollection(this IServiceCollection services)
    {
        services.AddHttpClient("push2his.eastmoney.com", (provider, client) =>
        {
            var userAgent = UserAgent.Generate(Browser.Chrome, Platform.Desktop) ?? "";
            var settings = provider.GetRequiredService<IOptions<AppSettings>>();
            client.BaseAddress = new Uri("https://push2his.eastmoney.com/");
            client.Timeout = settings.Value.Http.Timeout;
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Referer", "https://quote.eastmoney.com/");
        }).ConfigurePrimaryHttpMessageHandler(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>();
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = settings.Value.Http.Proxy is not null
            };
            if (handler.UseProxy)
                handler.Proxy = settings.Value.Http.Proxy;
            return handler;
        });
        
        // 以名搜股票代码 HttpClient
        services.AddHttpClient("search-codetable.eastmoney.com", (provider, client) =>
        {
            var userAgent = UserAgent.Generate(Browser.Chrome, Platform.Desktop) ?? "";
            var settings = provider.GetRequiredService<IOptions<AppSettings>>();
            client.BaseAddress = new Uri("https://search-codetable.eastmoney.com/");
            client.Timeout = settings.Value.Http.Timeout;
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Referer", "https://quote.eastmoney.com/");
        }).ConfigurePrimaryHttpMessageHandler(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>();
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = settings.Value.Http.Proxy is not null
            };
            if (handler.UseProxy)
                handler.Proxy = settings.Value.Http.Proxy;
            return handler;
        });
        
        // add cookie container
        // 后续交易工具需要
        services.AddSingleton<CookieContainer>(_ => new CookieContainer());
    }
}
#endregion
