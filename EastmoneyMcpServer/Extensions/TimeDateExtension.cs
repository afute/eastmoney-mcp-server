using System.Globalization;

namespace EastmoneyMcpServer.Extensions;

public static class TimeDateExtension
{
    /// <summary>
    /// 获取ISO周的年份和周数
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static (int, int) GetIsoYearAndWeek(this DateTime date)
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
}
