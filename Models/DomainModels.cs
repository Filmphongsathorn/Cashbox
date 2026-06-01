// ============================================================
// Models/DomainModels.cs  —  EF Core entity definitions
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace CashboxAnalyzer.Models;

// ── รายรับ ────────────────────────────────────────────────
public class DailyRevenue
{
    public int     Id              { get; set; }
    public DateOnly Date           { get; set; }
    public decimal TotalCashAmount { get; set; }
    public string  Note            { get; set; } = string.Empty;   // เหตุผล/รายละเอียด
    public string  RevenueSource   { get; set; } = string.Empty;   // หมวดรายรับ
}

// ── รายจ่าย ───────────────────────────────────────────────
public class DailyExpense
{
    public int     Id       { get; set; }
    public DateOnly Date    { get; set; }
    public string  Category { get; set; } = string.Empty;   // หมวดค่าใช้จ่าย
    public decimal Amount   { get; set; }
    public string  Note     { get; set; } = string.Empty;   // รายละเอียด เช่น "ค่าหมู"
}

// ── หมวดหมู่รายจ่าย (ผู้ใช้สร้างเองได้) ───────────────────
public class ExpenseCategory
{
    public int    Id   { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

// ── หมวดหมู่รายรับ (ผู้ใช้สร้างเองได้) ───────────────────
public class RevenueCategory
{
    public int    Id   { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
