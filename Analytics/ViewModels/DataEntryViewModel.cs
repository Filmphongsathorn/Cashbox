// ============================================================
// Analytics/ViewModels/DataEntryViewModel.cs
// กรอกรายรับ / รายจ่าย  +  จัดการหมวดหมู่
// ============================================================
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CashboxAnalyzer.Data;
using CashboxAnalyzer.Models;
using CashboxAnalyzer.Analytics.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;

namespace CashboxAnalyzer.Analytics.ViewModels;

public sealed partial class DataEntryViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DataEntryViewModel(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;

        LoadCategoriesCommand = new AsyncRelayCommand(() => LoadCategoriesAsync());
        SaveRevenueCommand = new AsyncRelayCommand(() => SaveRevenueAsync());
        SaveExpenseCommand = new AsyncRelayCommand(() => SaveExpenseAsync());
        AddRevenueCategoryCommand = new AsyncRelayCommand(() => AddRevenueCategoryAsync());
        AddExpenseCategoryCommand = new AsyncRelayCommand(() => AddExpenseCategoryAsync());
        DeleteExpenseCategoryCommand = new AsyncRelayCommand(() => DeleteExpenseCategoryAsync());
        DeleteRevenueCategoryCommand = new AsyncRelayCommand(() => DeleteRevenueCategoryAsync());
        SearchCommand = new AsyncRelayCommand(() => LoadRecentEntriesAsync());
        DeleteRevenueCommand = new AsyncRelayCommand<DailyRevenue>(DeleteRevenueAsync);
        DeleteExpenseCommand = new AsyncRelayCommand<DailyExpense>(DeleteExpenseAsync);
        BackupDatabaseCommand = new AsyncRelayCommand(BackupDatabaseAsync);
        RestoreDatabaseCommand = new AsyncRelayCommand(RestoreDatabaseAsync);

        _ = LoadCategoriesAsync();
    }

    public IAsyncRelayCommand LoadCategoriesCommand { get; }
    public IAsyncRelayCommand SaveRevenueCommand { get; }
    public IAsyncRelayCommand SaveExpenseCommand { get; }
    public IAsyncRelayCommand AddRevenueCategoryCommand { get; }
    public IAsyncRelayCommand AddExpenseCategoryCommand { get; }
    public IAsyncRelayCommand DeleteExpenseCategoryCommand { get; }
    public IAsyncRelayCommand DeleteRevenueCategoryCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand<DailyRevenue> DeleteRevenueCommand { get; }
    public IAsyncRelayCommand<DailyExpense> DeleteExpenseCommand { get; }
    public IAsyncRelayCommand BackupDatabaseCommand { get; }
    public IAsyncRelayCommand RestoreDatabaseCommand { get; }

    // ── Shared state ──────────────────────────────────
    private bool   _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    private bool   _isSuccess;
    public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }

    // ── Tab selection: Revenue=0, Expense=1, Manage=2 ─
    private int _selectedTab;
    public int SelectedTab { get => _selectedTab; set => SetProperty(ref _selectedTab, value); }

    // ── Categories ────────────────────────────────────
    public ObservableCollection<string> RevenueCategories { get; } = [];
    public ObservableCollection<string> ExpenseCategories { get; } = [];

    // ── Revenue form ──────────────────────────────────
    private DateTime _revenueDate    = DateTime.Today;
    public DateTime RevenueDate { get => _revenueDate; set => SetProperty(ref _revenueDate, value); }
    private string   _revenueSource  = string.Empty;
    public string RevenueSource { get => _revenueSource; set => SetProperty(ref _revenueSource, value); }
    private decimal  _revenueAmount;
    public decimal RevenueAmount { get => _revenueAmount; set => SetProperty(ref _revenueAmount, value); }
    private string   _revenueNote    = string.Empty;
    public string RevenueNote { get => _revenueNote; set => SetProperty(ref _revenueNote, value); }

    // ── Expense form ──────────────────────────────────
    private DateTime _expenseDate     = DateTime.Today;
    public DateTime ExpenseDate { get => _expenseDate; set => SetProperty(ref _expenseDate, value); }
    private string   _expenseCategory = string.Empty;
    public string ExpenseCategory { get => _expenseCategory; set => SetProperty(ref _expenseCategory, value); }
    private decimal  _expenseAmount;
    public decimal ExpenseAmount { get => _expenseAmount; set => SetProperty(ref _expenseAmount, value); }
    private string   _expenseNote     = string.Empty;
    public string ExpenseNote { get => _expenseNote; set => SetProperty(ref _expenseNote, value); }

    // ── Category management form ──────────────────────
    private string _newRevenueCategoryName = string.Empty;
    public string NewRevenueCategoryName { get => _newRevenueCategoryName; set => SetProperty(ref _newRevenueCategoryName, value); }
    private string _newExpenseCategoryName = string.Empty;
    public string NewExpenseCategoryName { get => _newExpenseCategoryName; set => SetProperty(ref _newExpenseCategoryName, value); }
    private string _selectedRevCatToDelete = string.Empty;
    public string SelectedRevCatToDelete { get => _selectedRevCatToDelete; set => SetProperty(ref _selectedRevCatToDelete, value); }
    private string _selectedExpCatToDelete = string.Empty;
    public string SelectedExpCatToDelete { get => _selectedExpCatToDelete; set => SetProperty(ref _selectedExpCatToDelete, value); }

    // ── Recent entries (last 10) ──────────────────────
    public ObservableCollection<DailyRevenue> RecentRevenues { get; } = [];
    public ObservableCollection<DailyExpense> RecentExpenses { get; } = [];

    private string _searchKeyword = string.Empty;
    public string SearchKeyword { get => _searchKeyword; set { SetProperty(ref _searchKeyword, value); _ = LoadRecentEntriesAsync(); } }

    // ==================================================
    // LOAD CATEGORIES
    // ==================================================
    public async Task LoadCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var revCats = await db.RevenueCategories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync(ct);
        var expCats = await db.ExpenseCategories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync(ct);

        RevenueCategories.Clear();
        foreach (var c in revCats) RevenueCategories.Add(c);

        ExpenseCategories.Clear();
        foreach (var c in expCats) ExpenseCategories.Add(c);

        await LoadRecentEntriesAsync(ct);
    }

    private async Task LoadRecentEntriesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var revQuery = db.DailyRevenues.AsQueryable();
        var expQuery = db.DailyExpenses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var k = SearchKeyword.Trim();
            revQuery = revQuery.Where(r => r.Note.Contains(k) || r.RevenueSource.Contains(k));
            expQuery = expQuery.Where(e => e.Note.Contains(k) || e.Category.Contains(k));
        }

        var revs = await revQuery.OrderByDescending(r => r.Date).ThenByDescending(r => r.Id).Take(10).ToListAsync(ct);
        var exps = await expQuery.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).Take(10).ToListAsync(ct);

        RecentRevenues.Clear();
        foreach (var r in revs) RecentRevenues.Add(r);

        RecentExpenses.Clear();
        foreach (var e in exps) RecentExpenses.Add(e);
    }

    // ==================================================
    // SAVE REVENUE
    // ==================================================
    private async Task SaveRevenueAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(RevenueSource)) { ShowError("กรุณาเลือกหมวดรายรับ"); return; }
        if (RevenueAmount <= 0)                        { ShowError("จำนวนเงินต้องมากกว่า 0"); return; }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.DailyRevenues.Add(new DailyRevenue
            {
                Date            = DateOnly.FromDateTime(RevenueDate),
                RevenueSource   = RevenueSource,
                TotalCashAmount = RevenueAmount,
                Note            = RevenueNote,
            });
            await db.SaveChangesAsync(ct);
            WeakReferenceMessenger.Default.Send(new DataUpdatedMessage());
            ShowSuccess($"บันทึกรายรับ ฿{RevenueAmount:N2} เรียบร้อย");
            ClearRevenueForm();
            await LoadRecentEntriesAsync(ct);
        }
        catch (Exception ex) { ShowError($"บันทึกไม่สำเร็จ: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    // ==================================================
    // SAVE EXPENSE
    // ==================================================
    private async Task SaveExpenseAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ExpenseCategory)) { ShowError("กรุณาเลือกหมวดรายจ่าย"); return; }
        if (ExpenseAmount <= 0)                          { ShowError("จำนวนเงินต้องมากกว่า 0"); return; }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.DailyExpenses.Add(new DailyExpense
            {
                Date     = DateOnly.FromDateTime(ExpenseDate),
                Category = ExpenseCategory,
                Amount   = ExpenseAmount,
                Note     = ExpenseNote,
            });
            await db.SaveChangesAsync(ct);
            WeakReferenceMessenger.Default.Send(new DataUpdatedMessage());
            ShowSuccess($"บันทึกรายจ่าย ฿{ExpenseAmount:N2} เรียบร้อย");
            ClearExpenseForm();
            await LoadRecentEntriesAsync(ct);
        }
        catch (Exception ex) { ShowError($"บันทึกไม่สำเร็จ: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    // ==================================================
    // ADD REVENUE CATEGORY
    // ==================================================
    private async Task AddRevenueCategoryAsync(CancellationToken ct = default)
    {
        var name = NewRevenueCategoryName.Trim();
        if (string.IsNullOrEmpty(name)) { ShowError("กรุณากรอกชื่อหมวด"); return; }
        if (RevenueCategories.Contains(name)) { ShowError("หมวดนี้มีอยู่แล้ว"); return; }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.RevenueCategories.Add(new RevenueCategory { Name = name });
            await db.SaveChangesAsync(ct);
            RevenueCategories.Add(name);
            NewRevenueCategoryName = string.Empty;
            ShowSuccess($"เพิ่มหมวด \"{name}\" สำเร็จ");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    // ==================================================
    // ADD EXPENSE CATEGORY
    // ==================================================
    private async Task AddExpenseCategoryAsync(CancellationToken ct = default)
    {
        var name = NewExpenseCategoryName.Trim();
        if (string.IsNullOrEmpty(name)) { ShowError("กรุณากรอกชื่อหมวด"); return; }
        if (ExpenseCategories.Contains(name)) { ShowError("หมวดนี้มีอยู่แล้ว"); return; }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.ExpenseCategories.Add(new ExpenseCategory { Name = name });
            await db.SaveChangesAsync(ct);
            ExpenseCategories.Add(name);
            NewExpenseCategoryName = string.Empty;
            ShowSuccess($"เพิ่มหมวด \"{name}\" สำเร็จ");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    // ==================================================
    // DELETE EXPENSE CATEGORY
    // ==================================================
    private async Task DeleteExpenseCategoryAsync(CancellationToken ct = default)
    {
        var name = SelectedExpCatToDelete;
        if (string.IsNullOrEmpty(name)) return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var cat = await db.ExpenseCategories.FirstOrDefaultAsync(c => c.Name == name, ct);
            if (cat != null) { db.ExpenseCategories.Remove(cat); await db.SaveChangesAsync(ct); }
            ExpenseCategories.Remove(name);
            SelectedExpCatToDelete = string.Empty;
            ShowSuccess($"ลบหมวด \"{name}\" สำเร็จ");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    // ==================================================
    // DELETE REVENUE CATEGORY
    // ==================================================
    private async Task DeleteRevenueCategoryAsync(CancellationToken ct = default)
    {
        var name = SelectedRevCatToDelete;
        if (string.IsNullOrEmpty(name)) return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var cat = await db.RevenueCategories.FirstOrDefaultAsync(c => c.Name == name, ct);
            if (cat != null) { db.RevenueCategories.Remove(cat); await db.SaveChangesAsync(ct); }
            RevenueCategories.Remove(name);
            SelectedRevCatToDelete = string.Empty;
            ShowSuccess($"ลบหมวด \"{name}\" สำเร็จ");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    // ── helpers ──────────────────────────────────────
    private void ClearRevenueForm()
    {
        RevenueDate   = DateTime.Today;
        RevenueSource = string.Empty;
        RevenueAmount = 0;
        RevenueNote   = string.Empty;
    }

    private void ClearExpenseForm()
    {
        ExpenseDate     = DateTime.Today;
        ExpenseCategory = string.Empty;
        ExpenseAmount   = 0;
        ExpenseNote     = string.Empty;
    }

    private void ShowSuccess(string msg) { StatusMessage = msg; IsSuccess = true; }
    private void ShowError(string msg)   { StatusMessage = msg; IsSuccess = false; }

    // ==================================================
    // BACKUP / RESTORE DB
    // ==================================================
    private async Task BackupDatabaseAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "สำรองข้อมูลฐานข้อมูล",
            Filter = "Database Files (*.db)|*.db",
            FileName = $"CashboxBackup_{DateTime.Today:yyyyMMdd}.db"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var sourcePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cashbox.db");
                if (System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Copy(sourcePath, dialog.FileName, true);
                    ShowSuccess("สำรองข้อมูลเรียบร้อยแล้ว");
                }
                else
                {
                    ShowError("ไม่พบไฟล์ฐานข้อมูล");
                }
            }
            catch (Exception ex) { ShowError($"สำรองข้อมูลล้มเหลว: {ex.Message}"); }
        }
    }

    private async Task RestoreDatabaseAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "กู้คืนฐานข้อมูล",
            Filter = "Database Files (*.db)|*.db"
        };

        if (dialog.ShowDialog() == true)
        {
            var result = System.Windows.MessageBox.Show("การกู้คืนจะเขียนทับข้อมูลปัจจุบันทั้งหมด คุณแน่ใจหรือไม่?", "ยืนยันการกู้คืน", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    var targetPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cashbox.db");
                    System.IO.File.Copy(dialog.FileName, targetPath, true);
                    
                    WeakReferenceMessenger.Default.Send(new DataUpdatedMessage());
                    await LoadCategoriesAsync();
                    ShowSuccess("กู้คืนข้อมูลเรียบร้อยแล้ว");
                }
                catch (Exception ex) { ShowError($"กู้คืนล้มเหลว: {ex.Message}"); }
            }
        }
    }

    // ==================================================
    // DELETE REVENUE/EXPENSE
    // ==================================================
    private async Task DeleteRevenueAsync(DailyRevenue? r)
    {
        if (r == null) return;
        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.DailyRevenues.Remove(r);
            await db.SaveChangesAsync();
            WeakReferenceMessenger.Default.Send(new DataUpdatedMessage());
            ShowSuccess("ลบรายการรายรับเรียบร้อย");
            await LoadRecentEntriesAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private async Task DeleteExpenseAsync(DailyExpense? e)
    {
        if (e == null) return;
        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.DailyExpenses.Remove(e);
            await db.SaveChangesAsync();
            WeakReferenceMessenger.Default.Send(new DataUpdatedMessage());
            ShowSuccess("ลบรายการรายจ่ายเรียบร้อย");
            await LoadRecentEntriesAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }
}
