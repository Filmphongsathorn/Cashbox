// ============================================================
// Data/AppDbContext.cs
// ============================================================
using CashboxAnalyzer.Models;
using Microsoft.EntityFrameworkCore;

namespace CashboxAnalyzer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DailyRevenue>    DailyRevenues    { get; set; }
    public DbSet<DailyExpense>    DailyExpenses    { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<RevenueCategory> RevenueCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed หมวดหมู่เริ่มต้น
        modelBuilder.Entity<ExpenseCategory>().HasData(
            new ExpenseCategory { Id = 1, Name = "วัตถุดิบ/อาหาร" },
            new ExpenseCategory { Id = 2, Name = "ค่าแรง" },
            new ExpenseCategory { Id = 3, Name = "ค่าน้ำ/ไฟ" },
            new ExpenseCategory { Id = 4, Name = "ค่าขนส่ง" },
            new ExpenseCategory { Id = 5, Name = "อุปกรณ์" },
            new ExpenseCategory { Id = 6, Name = "อื่นๆ" }
        );

        modelBuilder.Entity<RevenueCategory>().HasData(
            new RevenueCategory { Id = 1, Name = "ขายสินค้า" },
            new RevenueCategory { Id = 2, Name = "บริการ" },
            new RevenueCategory { Id = 3, Name = "ส่วนต่างกำไร" },
            new RevenueCategory { Id = 4, Name = "อื่นๆ" }
        );

        // Decimal precision
        modelBuilder.Entity<DailyRevenue>()
            .Property(r => r.TotalCashAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DailyExpense>()
            .Property(e => e.Amount)
            .HasPrecision(18, 2);
    }
}
