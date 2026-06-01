// ============================================================
// Analytics/Models/AnalyticsDtos.cs
// ============================================================
namespace CashboxAnalyzer.Analytics.Models;

public record DailyProfitPoint(DateOnly Date, decimal Revenue, decimal Expense)
{
    public decimal NetProfit => Revenue - Expense;
}

public record DailyComparison(int Day, decimal TotalRevenue, decimal TotalExpense)
{
    public decimal NetProfit => TotalRevenue - TotalExpense;
    public string  DayLabel  => Day.ToString();
}

public record MonthlySummary(int Month, decimal TotalRevenue, decimal TotalExpense)
{
    public decimal NetProfit      => TotalRevenue - TotalExpense;
    public string  MonthLabel     => new DateTime(1, Month, 1).ToString("MMM");
    public string  ProfitIndicator => NetProfit >= 0 ? "▲" : "▼";
}

public record CategoryExpense(string Category, decimal Amount);

public record MonthKpi(decimal TotalRevenue, decimal TotalExpense)
{
    public decimal NetProfit => TotalRevenue - TotalExpense;
}

public record DataUpdatedMessage();
