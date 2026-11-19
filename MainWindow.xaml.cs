using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using SiamNetworkMonitor.Services;
using SiamNetworkMonitor.ViewModels;

namespace SiamNetworkMonitor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly TaskbarMeterWindow _meter;
    private bool _reallyExit;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _meter = new TaskbarMeterWindow();
        _vm.TrafficUpdated += (down, up) =>
        {
            _meter.Update(down, up);
            Title = $"Down {DisplayFormatter.Rate(down)} | Up {DisplayFormatter.Rate(up)} | Siam Network Monitor";
        };
        _meter.OpenRequested += ShowDashboard;
        _meter.ExitRequested += () => { _reallyExit = true; Close(); };
        Loaded += (_, _) =>
        {
            var view = CollectionViewSource.GetDefaultView(ProcessGrid.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("TotalKBps", ListSortDirection.Descending));
            _meter.Show();
            var startup = Environment.GetCommandLineArgs().Any(x => x.Equals("--startup", StringComparison.OrdinalIgnoreCase));
            var settings = new SiamNetworkMonitor.Services.AppSettingsService().Load();
            if (startup && settings.StartMinimized) Dispatcher.BeginInvoke(Hide);
        };
        Closing += OnClosing;
    }

    private void ShowDashboard() => Dispatcher.Invoke(() => { Show(); WindowState = WindowState.Normal; Activate(); });

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_reallyExit) { e.Cancel = true; Hide(); return; }
        _meter.Close(); _vm.Dispose();
    }
    private void PeriodButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button b && int.TryParse(b.Tag?.ToString(), out var days))
            _vm.SetPeriod(days, b.Content?.ToString() ?? "History");
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export application usage history",
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"SiamNetworkMonitor-{_vm.PeriodText.Replace(" ", "-")}-{DateTime.Now:yyyyMMdd}.csv"
        };
        if (dialog.ShowDialog(this) == true)
        {
            _vm.Export(dialog.FileName);
            System.Windows.MessageBox.Show(this, $"Usage history exported to:\n{dialog.FileName}", "Export complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    private SiamNetworkMonitor.ViewModels.ProcessRowViewModel? SelectedProcess => ProcessGrid.SelectedItem as SiamNetworkMonitor.ViewModels.ProcessRowViewModel;
    private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
    {
        var row=SelectedProcess;if(row is null||!row.HasPath)return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{row.Path}\""){UseShellExecute=true});
    }
    private void Properties_Click(object sender, RoutedEventArgs e)
    {
        var row=SelectedProcess;if(row is null||!row.HasPath)return;
        var info=new System.Diagnostics.ProcessStartInfo("rundll32.exe", $"shell32.dll,ShellExec_RunDLL properties \"{row.Path}\""){UseShellExecute=true};
        System.Diagnostics.Process.Start(info);
    }
    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        var row=SelectedProcess;if(row is null||!row.HasPath)return;System.Windows.Clipboard.SetText(row.Path);
    }

    private SiamNetworkMonitor.ViewModels.AlertRowViewModel? SelectedAlert => AlertsGrid.SelectedItem as SiamNetworkMonitor.ViewModels.AlertRowViewModel;
    private void AcknowledgeAlert_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAlert is not null) _vm.Acknowledge(SelectedAlert.Id);
    }
    private void ResolveAlert_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAlert is not null) _vm.Resolve(SelectedAlert.Id);
    }
    private void ClearAlerts_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this,"Clear all alert history?","Siam Network Monitor",System.Windows.MessageBoxButton.YesNo,System.Windows.MessageBoxImage.Question)==System.Windows.MessageBoxResult.Yes)
            _vm.ClearAlertHistory();
    }

    private void EditRules_Click(object sender, RoutedEventArgs e)
    {
        var store = new SiamNetworkMonitor.Services.AlertRuleStore();
        var dialog = new AlertRuleEditorWindow(store, _vm.GetRules()) { Owner = this };
        if (dialog.ShowDialog() == true) _vm.ReplaceRules(dialog.Rules);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog=new SettingsWindow(new SiamNetworkMonitor.Services.AppSettingsService()){Owner=this};
        dialog.ShowDialog();
    }

    private void ApplicationSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => RefreshProcessFilter();
    private void ActiveOnlyBox_Changed(object sender, RoutedEventArgs e) => RefreshProcessFilter();
    private void RefreshProcessFilter()
    {
        if (ProcessGrid.ItemsSource is null) return;
        var view=CollectionViewSource.GetDefaultView(ProcessGrid.ItemsSource);
        var query=ApplicationSearchBox?.Text?.Trim() ?? "";
        var activeOnly=ActiveOnlyBox?.IsChecked==true;
        view.Filter=item =>
        {
            if(item is not SiamNetworkMonitor.ViewModels.ProcessRowViewModel row)return false;
            if(activeOnly && row.DownloadKBps<=0 && row.UploadKBps<=0)return false;
            if(query.Length==0)return true;
            return row.Description.Contains(query,StringComparison.OrdinalIgnoreCase)
                || row.ProcessName.Contains(query,StringComparison.OrdinalIgnoreCase)
                || row.Publisher.Contains(query,StringComparison.OrdinalIgnoreCase)
                || row.Path.Contains(query,StringComparison.OrdinalIgnoreCase);
        };
        view.Refresh();
    }
    private void PauseButton_Click(object sender, RoutedEventArgs e) => _vm.TogglePause();

    private void ViewChart_Click(object sender, RoutedEventArgs e) => OpenSelectedChart();
    private void ProcessGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenSelectedChart();
    private void OpenSelectedChart()
    {
        if (SelectedProcess is null) return;
        new ApplicationChartWindow(SelectedProcess) { Owner = this }.Show();
    }

}
