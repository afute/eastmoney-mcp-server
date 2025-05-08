using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

namespace EastmoneyMcpServer.Test;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var transport = new SseClientTransport(new SseClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:5067/sse"),
        });
        var mcpClient = await McpClientFactory.CreateAsync(transport);
        var tools = await mcpClient.ListToolsAsync();
        Console.WriteLine(tools[1].JsonSchema);
    }
}
