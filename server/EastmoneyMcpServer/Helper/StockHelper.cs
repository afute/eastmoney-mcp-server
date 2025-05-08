using EastmoneyMcpServer.Models.Enums;

namespace EastmoneyMcpServer.Helper;

public static class StockHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static StockBourse GetStockBourse(string code)
    {
        if (code.Length == 5) return StockBourse.HongKong;
        if (code.Length != 6 || !int.TryParse(code, out _))
            throw new FormatException("不支持A股和港股以外的股票");

        return code[0].ToString() switch
        {
            "5" or "6" => StockBourse.Shanghai,
            "1" or "0" or "3" => StockBourse.Shenzhen,
            "8" => StockBourse.Beijing,
            _ => throw new FormatException("不支持A股和港股以外的股票")
        };
    } 
}
