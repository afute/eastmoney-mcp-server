using EastmoneyMcpServer.Models;
using EastmoneyMcpServer.Models.Enums;
using MongoDB.Driver;

namespace EastmoneyMcpServer.Interfaces;

public interface IKLineInstance
{
    public Task<IMongoCollection<StockKLine>> GetCollectionFromCheck(string code, AdjustedType adjustedType, CancellationToken token);
}
