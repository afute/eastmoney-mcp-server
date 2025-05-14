using EastmoneyMcpServer.Models.Attributes;

namespace EastmoneyMcpServer.Models.Enums;

public enum StockExchange
{
    [Metadata<string>("code", "1")]
    Shanghai = 1,
    
    [Metadata<string>("code", "0")]
    Shenzhen = 2,
    
    [Metadata<string>("code", "0")]
    Beijing = 3,
    
    [Metadata<string>("code", "116")]
    HongKong = 6
}
