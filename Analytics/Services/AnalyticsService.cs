// ============================================================
// Analytics/Services/AnalyticsService.cs
// ============================================================
using CashboxAnalyzer.Analytics.Models;
using CashboxAnalyzer.Data;
using Microsoft.EntityFrameworkCore;

namespace CashboxAnalyzer.Analytics.Services;

public interface IAnalyticsService
{
    Task<MonthKpi>                         GetCurrentMonthKpiAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DailyProfitPoint>> GetLast7DaysProfitAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DailyComparison>>  GetDailyComparisonCurrentMonthAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlySummary>>   GetMonthlySummariesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CategoryExpense>>  GetCurrentMonthCategoryExpensesAsync(CancellationToken ct = default);
    // Filter by date range
    Task<IReadOnlyList<DailyProfitPoint>> GetProfitByRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

public sealed class AnalyticsService(IDbContextFactory<AppDbContext> dbFactory) : IAnalyticsService
{
    public async Task<MonthKpi> GetCurrentMonthKpiAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var start = new DateOnly(today.Year, today.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var revs = await db.DailyRevenues.Where(r => r.Date >= start && r.Date <= end).ToListAsync(ct);
        var exps = await db.DailyExpenses.Where(e => e.Date >= start && e.Date <= end).ToListAsync(ct);
        
        var revenue = revs.Sum(r => (decimal?)r.TotalCashAmount ?? 0m);
        var expense = exps.Sum(e => (decimal?)e.Amount ?? 0m);
        return new MonthKpi(revenue, expense);
    }

    public async Task<IReadOnlyList<DailyProfitPoint>> GetLast7DaysProfitAsync(CancellationToken ct = default)
    {
        var today       = DateOnly.FromDateTime(DateTime.Today);
        var sevenDaysAgo = today.AddDays(-6);
        return await GetProfitByRangeAsync(sevenDaysAgo, today, ct);
    }

    public async Task<IReadOnlyList<DailyProfitPoint>> GetProfitByRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var revenues = await db.DailyRevenues
            .Where(r => r.Date >= from && r.Date <= to)
            .GroupBy(r => r.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(r => r.TotalCashAmount) })
            .ToDictionaryAsync(x => x.Date, x => (decimal)x.Total, ct);

        var expenses = await db.DailyExpenses
            .Where(e => e.Date >= from && e.Date <= to)
            .GroupBy(e => e.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.Date, x => (decimal)x.Total, ct);

        int days = to.DayNumber - from.DayNumber + 1;
        return Enumerable.Range(0, days).Select(offset =>
        {
            var date = from.AddDays(offset);
            revenues.TryGetValue(date, out var rev);
            expenses.TryGetValue(date, out var exp);
            return new DailyProfitPoint(date, rev, exp);
        }).ToList();
    }

    public async Task<IReadOnlyList<DailyComparison>> GetDailyComparisonCurrentMonthAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var start = new DateOnly(today.Year, today.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        
        var revs = await db.DailyRevenues.Where(r => r.Date >= start && r.Date <= end).ToListAsync(ct);
        var exps = await db.DailyExpenses.Where(e => e.Date >= start && e.Date <= end).ToListAsync(ct);

        var revenueByDay = revs.GroupBy(r => r.Date.Day).ToDictionary(g => g.Key, g => g.Sum(r => (decimal)r.TotalCashAmount));
        var expenseByDay = exps.GroupBy(e => e.Date.Day).ToDictionary(g => g.Key, g => g.Sum(e => (decimal)e.Amount));
            
        return Enumerable.Range(1, daysInMonth).Select(d =>
        {
            revenueByDay.TryGetValue(d, out var rev);
            expenseByDay.TryGetValue(d, out var exp);
            return new DailyComparison(d, rev, exp);
        }).ToList();
    }

    public async Task<IReadOnlyList<MonthlySummary>> GetMonthlySummariesAsync(CancellationToken ct = default)
    {
        var year = DateTime.Today.Year;
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        
        var revs = await db.DailyRevenues.Where(r => r.Date >= start && r.Date <= end).ToListAsync(ct);
        var exps = await db.DailyExpenses.Where(e => e.Date >= start && e.Date <= end).ToListAsync(ct);

        var revenueByMonth = revs.GroupBy(r => r.Date.Month).ToDictionary(g => g.Key, g => g.Sum(r => (decimal)r.TotalCashAmount));
        var expenseByMonth = exps.GroupBy(e => e.Date.Month).ToDictionary(g => g.Key, g => g.Sum(e => (decimal)e.Amount));
            
        return revenueByMonth.Keys.Union(expenseByMonth.Keys).OrderByDescending(m => m).Select(m =>
        {
            revenueByMonth.TryGetValue(m, out var rev);
            expenseByMonth.TryGetValue(m, out var exp);
            return new MonthlySummary(m, rev, exp);
        }).ToList();
    }

    public async Task<IReadOnlyList<CategoryExpense>> GetCurrentMonthCategoryExpensesAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var start = new DateOnly(today.Year, today.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var exps = await db.DailyExpenses.Where(e => e.Date >= start && e.Date <= end).ToListAsync(ct);
        
        return exps.GroupBy(e => e.Category)
            .Select(g => new CategoryExpense(g.Key, g.Sum(e => e.Amount)))
            .OrderByDescending(c => c.Amount)
            .ToList();
    }
}
