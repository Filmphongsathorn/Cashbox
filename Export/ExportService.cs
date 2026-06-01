using CashboxAnalyzer.Analytics.Models;
using CashboxAnalyzer.Data;
using CashboxAnalyzer.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CashboxAnalyzer.Export;

public interface IExportService
{
    Task<string> ExportToExcelAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

public sealed class ExportService(IDbContextFactory<AppDbContext> dbFactory) : IExportService
{
    private static readonly string[] ThaiMonths = ["ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."];

    public async Task<string> ExportToExcelAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var revenues = await db.DailyRevenues.OrderBy(r => r.Date).ToListAsync(ct);
        var expenses = await db.DailyExpenses.OrderBy(e => e.Date).ToListAsync(ct);

        using var wb = new XLWorkbook();

        var revGroup = revenues.GroupBy(r => new { r.Date.Year, r.Date.Month }).ToDictionary(g => g.Key, g => g.ToList());
        var expGroup = expenses.GroupBy(e => new { e.Date.Year, e.Date.Month }).ToDictionary(g => g.Key, g => g.ToList());
        var allKeys = revGroup.Keys.Union(expGroup.Keys).OrderByDescending(k => k.Year).ThenByDescending(k => k.Month).ToList();

        if (allKeys.Count == 0)
        {
            var ws = wb.Worksheets.Add("ไม่มีข้อมูล");
            ws.Cell(1,1).Value = "ไม่มีข้อมูลในระบบ";
        }
        else
        {
            foreach (var key in allKeys)
            {
                string monthName = ThaiMonths[key.Month - 1];
                string sheetName = $"{monthName} {key.Year}";
                var ws = wb.Worksheets.Add(sheetName);
                
                revGroup.TryGetValue(key, out var monthRevs);
                expGroup.TryGetValue(key, out var monthExps);
                monthRevs ??= [];
                monthExps ??= [];

                decimal totalRev = monthRevs.Sum(r => r.TotalCashAmount);
                decimal totalExp = monthExps.Sum(e => e.Amount);
                decimal profit = totalRev - totalExp;

                // 1. สรุปรายเดือน
                ws.Cell(1, 1).Value = $"สรุปรายเดือน {sheetName}";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 14;
                ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0A2540");
                ws.Range(1, 1, 1, 4).Merge();

                string[] summaryHeaders = ["เดือน", "รายรับ", "รายจ่าย", "กำไร"];
                for (int c = 0; c < summaryHeaders.Length; c++)
                {
                    var cell = ws.Cell(2, c + 1);
                    cell.Value = summaryHeaders[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F2FE");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#0A2540");
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                }

                ws.Cell(3, 1).Value = monthName;
                ws.Cell(3, 2).Value = totalRev;
                ws.Cell(3, 3).Value = totalExp;
                ws.Cell(3, 4).Value = profit;

                ws.Cell(3, 2).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(3, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(3, 4).Style.NumberFormat.Format = "#,##0.00";
                if (profit < 0) ws.Cell(3, 4).Style.Font.FontColor = XLColor.Red;
                else ws.Cell(3, 4).Style.Font.FontColor = XLColor.FromHtml("#16A34A");

                int currentRow = 5;

                // 2. รายรับ
                ws.Cell(currentRow, 1).Value = "รายรับทั้งหมด";
                ws.Cell(currentRow, 1).Style.Font.Bold = true;
                ws.Cell(currentRow, 1).Style.Font.FontSize = 12;
                ws.Range(currentRow, 1, currentRow, 4).Merge();
                currentRow++;

                string[] detailHeaders = ["วันที่", "หมวด", "จำนวน (฿)", "คำอธิบาย"];
                for (int c = 0; c < detailHeaders.Length; c++)
                {
                    var cell = ws.Cell(currentRow, c + 1);
                    cell.Value = detailHeaders[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#0A2540");
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                }
                currentRow++;

                foreach (var r in monthRevs.OrderBy(r => r.Date))
                {
                    ws.Cell(currentRow, 1).Value = r.Date.ToString("d/M/yyyy");
                    ws.Cell(currentRow, 2).Value = r.RevenueSource;
                    ws.Cell(currentRow, 3).Value = r.TotalCashAmount;
                    ws.Cell(currentRow, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(currentRow, 3).Style.Font.FontColor = XLColor.FromHtml("#16A34A"); // Green
                    ws.Cell(currentRow, 4).Value = r.Note;
                    currentRow++;
                }
                if (monthRevs.Count == 0) { ws.Cell(currentRow, 1).Value = "- ไม่มีรายรับ -"; currentRow++; }
                currentRow++;

                // 3. รายจ่าย
                ws.Cell(currentRow, 1).Value = "รายจ่ายทั้งหมด";
                ws.Cell(currentRow, 1).Style.Font.Bold = true;
                ws.Cell(currentRow, 1).Style.Font.FontSize = 12;
                ws.Range(currentRow, 1, currentRow, 4).Merge();
                currentRow++;

                for (int c = 0; c < detailHeaders.Length; c++)
                {
                    var cell = ws.Cell(currentRow, c + 1);
                    cell.Value = detailHeaders[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#0A2540");
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                }
                currentRow++;

                foreach (var e in monthExps.OrderBy(e => e.Date))
                {
                    ws.Cell(currentRow, 1).Value = e.Date.ToString("d/M/yyyy");
                    ws.Cell(currentRow, 2).Value = e.Category;
                    ws.Cell(currentRow, 3).Value = e.Amount;
                    ws.Cell(currentRow, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(currentRow, 3).Style.Font.FontColor = XLColor.FromHtml("#DC2626"); // Red
                    ws.Cell(currentRow, 4).Value = e.Note;
                    currentRow++;
                }
                if (monthExps.Count == 0) { ws.Cell(currentRow, 1).Value = "- ไม่มีรายจ่าย -"; currentRow++; }

                ws.Columns().AdjustToContents();
            }
        }

        var dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var path = System.IO.Path.Combine(dir, $"Cashbox_Export_{DateTime.Today:yyyyMMdd}.xlsx");
        wb.SaveAs(path);
        return path;
    }
}
