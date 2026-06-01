using System.Windows;
using CashboxAnalyzer.Analytics.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CashboxAnalyzer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (App.ServiceProvider != null)
        {
            AnalyticsViewControl.DataContext = App.ServiceProvider.GetRequiredService<AnalyticsViewModel>();
            DataEntryViewControl.DataContext = App.ServiceProvider.GetRequiredService<DataEntryViewModel>();
        }
    }
}