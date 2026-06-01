using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CashboxAnalyzer.Data;
using CashboxAnalyzer.Analytics.Services;
using CashboxAnalyzer.Export;
using CashboxAnalyzer.Analytics.ViewModels;

namespace CashboxAnalyzer;

public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbFolder = System.IO.Path.Combine(appData, "CashboxAnalyzer");
        System.IO.Directory.CreateDirectory(dbFolder);
        var dbPath = System.IO.Path.Combine(dbFolder, "cashbox.db");

        // Database Setup
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Services
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<IExportService, ExportService>();

        // ViewModels
        services.AddTransient<DataEntryViewModel>();
        services.AddTransient<AnalyticsViewModel>();

        // Main Window
        services.AddTransient<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();

        // Ensure database is created
        var dbFactory = ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using (var db = dbFactory.CreateDbContext())
        {
            db.Database.EnsureCreated();
        }

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
