// ============================================================
// Analytics/ViewModels/AnalyticsViewModel.cs
// Dashboard + Date Range Filter + Export (Excel / PDF)
// ============================================================
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CashboxAnalyzer.Analytics.Models;
using CashboxAnalyzer.Analytics.Services;
using CashboxAnalyzer.Export;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;

namespace CashboxAnalyzer.Analytics.ViewModels;

public sealed partial class AnalyticsViewModel : ObservableObject
{
    private static readonly SKColor BlueVibrant = SKColor.Parse("#1E88E5");
    private static readonly SKColor RedSoft     = SKColor.Parse("#EF5350");
    private static readonly SKColor NavyDeep    = SKColor.Parse("#0A2540");
    private static readonly SKColor[] DonutPalette =
    [
        SKColor.Parse("#FF3B30"), // Red
        SKColor.Parse("#FF9500"), // Orange
        SKColor.Parse("#FFCC00"), // Yellow
        SKColor.Parse("#4CD964"), // Green
        SKColor.Parse("#5AC8FA"), // Light Blue
        SKColor.Parse("#007AFF"), // Blue
        SKColor.Parse("#5856D6"), // Purple
        SKColor.Parse("#FF2D55"), // Pink
    ];

    private readonly IAnalyticsService _svc;
    private readonly IExportService    _export;

    public AnalyticsViewModel(IAnalyticsService svc, IExportService export)
    {
        _svc    = svc;
        _export = export;

        LoadAllCommand = new AsyncRelayCommand(() => LoadAllAsync());
        ApplyFilterCommand = new AsyncRelayCommand(() => ApplyFilterAsync());
        ExportExcelCommand = new AsyncRelayCommand(() => ExportExcelAsync());

        WeakReferenceMessenger.Default.Register<DataUpdatedMessage>(this, (r, m) =>
        {
            var vm = (AnalyticsViewModel)r;
            _ = vm.LoadAllAsync();
        });

        _ = LoadAllAsync();
    }

    public IAsyncRelayCommand LoadAllCommand { get; }
    public IAsyncRelayCommand ApplyFilterCommand { get; }
    public IAsyncRelayCommand ExportExcelCommand { get; }


    // ── State ─────────────────────────────────────────
    private bool   _isLoading = true;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    private bool   _isError;
    public bool IsError { get => _isError; set => SetProperty(ref _isError, value); }
    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    private string _exportStatus  = string.Empty;
    public string ExportStatus { get => _exportStatus; set => SetProperty(ref _exportStatus, value); }

    // ── KPI ───────────────────────────────────────────
    private decimal _ytdRevenue;
    public decimal YtdRevenue { get => _ytdRevenue; set => SetProperty(ref _ytdRevenue, value); }
    private decimal _ytdExpense;
    public decimal YtdExpense { get => _ytdExpense; set => SetProperty(ref _ytdExpense, value); }
    private decimal _ytdProfit;
    public decimal YtdProfit { get => _ytdProfit; set => SetProperty(ref _ytdProfit, value); }

    // ── Date Filter ───────────────────────────────────
    private DateTime _filterFrom = DateTime.Today.AddDays(-29);
    public DateTime FilterFrom { get => _filterFrom; set => SetProperty(ref _filterFrom, value); }
    private DateTime _filterTo   = DateTime.Today;
    public DateTime FilterTo { get => _filterTo; set => SetProperty(ref _filterTo, value); }

    // ── Monthly grid ───────────────────────────────────
    private ObservableCollection<MonthlySummary> _monthlySummaries = [];
    public ObservableCollection<MonthlySummary> MonthlySummaries { get => _monthlySummaries; set => SetProperty(ref _monthlySummaries, value); }

    // ── Chart series ──────────────────────────────────
    private ISeries[] _trendSeries    = [];
    public ISeries[] TrendSeries { get => _trendSeries; set => SetProperty(ref _trendSeries, value); }
    private ISeries[] _monthlySeries  = [];
    public ISeries[] MonthlySeries { get => _monthlySeries; set => SetProperty(ref _monthlySeries, value); }
    private ISeries[] _categorySeries = [];
    public ISeries[] CategorySeries { get => _categorySeries; set => SetProperty(ref _categorySeries, value); }

    // ── Axes ─────────────────────────────────────────
    private Axis[] _trendXAxes  = [];
    public Axis[] TrendXAxes { get => _trendXAxes; set => SetProperty(ref _trendXAxes, value); }
    private Axis[] _monthXAxes  = [];
    public Axis[] MonthXAxes { get => _monthXAxes; set => SetProperty(ref _monthXAxes, value); }
    private Axis[] _sharedYAxes =
    [
        new Axis
        {
            Labeler        = v => v.ToString("N0"),
            TextSize       = 11,
            LabelsPaint    = new SolidColorPaint(SKColor.Parse("#0A2540")),
            SeparatorsPaint= new SolidColorPaint(SKColor.Parse("#BFDBFE")) { StrokeThickness = 1 },
        }
    ];
    public Axis[] SharedYAxes { get => _sharedYAxes; set => SetProperty(ref _sharedYAxes, value); }

    public SolidColorPaint LegendTextPaint { get; } = new(NavyDeep);

    // ==================================================
    // LOAD ALL DATA
    // ==================================================
    public async Task LoadAllAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        IsError   = false;
        try
        {
            var kpiTask      = _svc.GetCurrentMonthKpiAsync(ct);
            var dailyTask    = _svc.GetDailyComparisonCurrentMonthAsync(ct);
            var monthlyTask  = _svc.GetMonthlySummariesAsync(ct);
            var categoryTask = _svc.GetCurrentMonthCategoryExpensesAsync(ct);
            await Task.WhenAll(kpiTask, dailyTask, monthlyTask, categoryTask);

            ApplyKpi(await kpiTask);
            ApplyDaily(await dailyTask);
            ApplyMonthlySummary(await monthlyTask);
            ApplyCategory(await categoryTask);

            // Load trend with current filter range
            await ApplyTrendFilterAsync(ct);
        }
        catch (Exception ex) { IsError = true; ErrorMessage = $"โหลดข้อมูลล้มเหลว: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    // ==================================================
    // APPLY DATE FILTER  (Trend chart)
    // ==================================================
    public async Task ApplyFilterAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try { await ApplyTrendFilterAsync(ct); }
        catch (Exception ex) { IsError = true; ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    private async Task ApplyTrendFilterAsync(CancellationToken ct)
    {
        var from   = DateOnly.FromDateTime(FilterFrom);
        var to     = DateOnly.FromDateTime(FilterTo);
        var points = await _svc.GetProfitByRangeAsync(from, to, ct);
        ApplyTrend(points);
    }

    // ==================================================
    // EXPORT EXCEL
    // ==================================================
    private async Task ExportExcelAsync(CancellationToken ct = default)
    {
        ExportStatus = "กำลัง Export Excel…";
        try
        {
            var from = DateOnly.FromDateTime(FilterFrom);
            var to   = DateOnly.FromDateTime(FilterTo);
            var path = await _export.ExportToExcelAsync(from, to, ct);
            ExportStatus = $"บันทึก Excel แล้ว: {path}";
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) { ExportStatus = $"Export ล้มเหลว: {ex.Message}"; }
    }


    // ── Chart appliers ───────────────────────────────
    private void ApplyKpi(MonthKpi kpi)
    {
        YtdRevenue = kpi.TotalRevenue;
        YtdExpense = kpi.TotalExpense;
        YtdProfit  = kpi.NetProfit;
    }

    private void ApplyTrend(IReadOnlyList<DailyProfitPoint> points)
    {
        TrendSeries =
        [
            new LineSeries<double>
            {
                Name           = "กำไรสุทธิ",
                Values         = points.Select(p => (double)p.NetProfit).ToArray(),
                Stroke         = new SolidColorPaint(BlueVibrant) { StrokeThickness = 4 },
                Fill           = new SolidColorPaint(BlueVibrant.WithAlpha(40)),
                GeometrySize   = 12,
                GeometryFill   = new SolidColorPaint(SKColors.White),
                GeometryStroke = new SolidColorPaint(BlueVibrant) { StrokeThickness = 3 },
                LineSmoothness = 0.2, // Simple smooth line
                YToolTipLabelFormatter = point => $"กำไรสุทธิ: ฿{point.Model:N0}"
            }
        ];

        // แสดง label ทุก N วัน ถ้า range ยาว
        int step   = Math.Max(1, points.Count / 12);
        var labels = points.Select((p, i) => i % step == 0 ? p.Date.ToString("dd/MM") : "").ToArray();

        TrendXAxes =
        [
            new Axis
            {
                Labels      = labels,
                TextSize    = 10,
                LabelsPaint = new SolidColorPaint(NavyDeep),
                SeparatorsPaint = null,
            }
        ];
    }

    private void ApplyDaily(IReadOnlyList<DailyComparison> days)
    {
        MonthlySeries =
        [
            new ColumnSeries<double>
            {
                Name   = "รายรับ",
                Values = days.Select(m => (double)m.TotalRevenue).ToArray(),
                Fill   = new SolidColorPaint(BlueVibrant),
                Rx = 4, Ry = 4,

            },
            new ColumnSeries<double>
            {
                Name   = "รายจ่าย",
                Values = days.Select(m => (double)m.TotalExpense).ToArray(),
                Fill   = new SolidColorPaint(RedSoft),
                Rx = 4, Ry = 4,

            },
        ];

        MonthXAxes =
        [
            new Axis
            {
                Labels      = days.Select(m => m.DayLabel).ToArray(),
                TextSize    = 10,
                LabelsPaint = new SolidColorPaint(NavyDeep),
                SeparatorsPaint = null,
            }
        ];
    }

    private void ApplyMonthlySummary(IReadOnlyList<MonthlySummary> summaries)
        => MonthlySummaries = new ObservableCollection<MonthlySummary>(summaries);

    private void ApplyCategory(IReadOnlyList<CategoryExpense> categories)
    {
        CategorySeries = categories.Count == 0
            ? [new PieSeries<double> { Name = "ไม่มีข้อมูล", Values = [1d], Fill = new SolidColorPaint(SKColor.Parse("#E0F2FE")) }]
            : categories.Select((c, i) => (ISeries)new PieSeries<double>
            {
                Name             = c.Category,
                Values           = [(double)c.Amount],
                Fill             = new SolidColorPaint(DonutPalette[i % DonutPalette.Length]),
                InnerRadius      = 55,
                MaxRadialColumnWidth = 26,

            }).ToArray();
    }
}
