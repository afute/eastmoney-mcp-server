using System.Net;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using EastmoneyMcpServer.Models;

namespace EastmoneyMcpServer;


public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // http client
        builder.Services.AddHttpClient("push2his.eastmoney.com", client =>
        {
            client.BaseAddress = new Uri("https://push2his.eastmoney.com/");
            client.Timeout = TimeSpan.FromSeconds(5);
        }).ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        });
        
        // mongodb kline database
        builder.Services.Configure<AppSettings>(builder.Configuration);
        builder.Services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>();
            var client = new MongoClient(settings.Value.DatabaseConnection);
            return client;
        });
        
        builder.Services.AddMcpService();
        
        var app = builder.Build();
        app.MapMcp();
        
        await app.RunAsync();
    }
}
