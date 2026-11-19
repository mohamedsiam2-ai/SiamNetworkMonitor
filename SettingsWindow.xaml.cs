using System.Windows;using SiamNetworkMonitor.Services;
namespace SiamNetworkMonitor;
public partial class SettingsWindow:Window
{
 readonly AppSettingsService _service;readonly AppSettings _settings;public SettingsWindow(AppSettingsService service){InitializeComponent();_service=service;_settings=service.Load();StartWindowsBox.IsChecked=service.IsStartupEnabled();StartMinimizedBox.IsChecked=_settings.StartMinimized;NotificationsBox.IsChecked=_settings.NotificationsEnabled;}
 void Save_Click(object s,RoutedEventArgs e){_settings.StartWithWindows=StartWindowsBox.IsChecked==true;_settings.StartMinimized=StartMinimizedBox.IsChecked==true;_settings.NotificationsEnabled=NotificationsBox.IsChecked==true;_service.Save(_settings);DialogResult=true;Close();}void Cancel_Click(object s,RoutedEventArgs e){DialogResult=false;Close();}
}
