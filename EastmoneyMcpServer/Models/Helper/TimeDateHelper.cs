using System.Globalization;

namespace EastmoneyMcpServer.Models.Helper;

internal static class TimeDateHelper
{
    internal static (int, int) GetIsoYearAndWeek(this DateTime date)
    {
        // 调整到周四（ISO周计算的核心）
        var thursday = date.AddDays(3 - ((int)date.DayOfWeek + 6) % 7);
        
        // 计算年份（可能和日期的年份不同）
        var year = thursday.Year;
        
        const CalendarWeekRule rule = CalendarWeekRule.FirstFourDayWeek;
        var calendar = CultureInfo.InvariantCulture.Calendar;
        
        // 计算这是该年的第几周
        var week = calendar.GetWeekOfYear(thursday, rule, DayOfWeek.Monday);
        return (year, week);
    }
    
    /// <summary>
    /// 日期时间整数 [2025-01-01] -> [20250101] kind = Unspecified
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    internal static int ToInt(this DateTime date)
    {
        var year = date.Year;
        var month = date.Month;
        var day = date.Day;
        return year * 10000 + month * 100 + day * 1;
    }
}
