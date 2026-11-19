using System.Collections.ObjectModel;using System.Windows;using SiamNetworkMonitor.Alerts;using SiamNetworkMonitor.Services;
namespace SiamNetworkMonitor;
public partial class AlertRuleEditorWindow:Window
{
 readonly AlertRuleStore _store;readonly ObservableCollection<AlertRule> _rules;AlertRule? _selected;public IReadOnlyList<AlertRule> Rules=>_rules;
 public AlertRuleEditorWindow(AlertRuleStore store,IEnumerable<AlertRule> rules){InitializeComponent();_store=store;_rules=new(rules.Select(Clone));RulesList.ItemsSource=_rules;FillCombo(TypeBox, Enum.GetValues<AlertRuleType>().Select(x => ((object)x, x.ToString())));
        FillCombo(ScopeBox, Enum.GetValues<AlertScope>().Select(x => ((object)x, x.ToString())));if(_rules.Count>0)RulesList.SelectedIndex=0;}
 static AlertRule Clone(AlertRule x)=>new(){Id=x.Id,Name=x.Name,Enabled=x.Enabled,Type=x.Type,Scope=x.Scope,ProcessName=x.ProcessName,ExecutablePath=x.ExecutablePath,ThresholdKB=x.ThresholdKB,ThresholdKBps=x.ThresholdKBps,SustainedSeconds=x.SustainedSeconds,MinimumTrafficKB=x.MinimumTrafficKB,CooldownMinutes=x.CooldownMinutes,Severity=x.Severity,Notification=x.Notification,RequireUnsignedApplication=x.RequireUnsignedApplication,IncludeUnknownSignature=x.IncludeUnknownSignature,CreatedUtc=x.CreatedUtc,ModifiedUtc=x.ModifiedUtc};
 void RulesList_SelectionChanged(object s,System.Windows.Controls.SelectionChangedEventArgs e){_selected=RulesList.SelectedItem as AlertRule;if(_selected is null)return;EnabledBox.IsChecked=_selected.Enabled;NameBox.Text=_selected.Name;SelectCombo(TypeBox,_selected.Type);SelectCombo(ScopeBox,_selected.Scope);ProcessBox.Text=_selected.ProcessName??"";UsageBox.Text=(_selected.ThresholdKB/1024d).ToString("0.##");SpeedBox.Text=(_selected.ThresholdKBps/1024d).ToString("0.##");DurationBox.Text=_selected.SustainedSeconds.ToString();MinimumBox.Text=(_selected.MinimumTrafficKB/1024d).ToString("0.##");CooldownBox.Text=_selected.CooldownMinutes.ToString();ValidationText.Text="";}
 void New_Click(object s,RoutedEventArgs e){var r=new AlertRule{Name="New alert rule",Type=AlertRuleType.ApplicationSessionUsage,ThresholdKB=100*1024,MinimumTrafficKB=1024,CooldownMinutes=15};_rules.Add(r);RulesList.SelectedItem=r;}
 void Delete_Click(object s,RoutedEventArgs e){if(_selected is null)return;var i=RulesList.SelectedIndex;_rules.Remove(_selected);RulesList.SelectedIndex=Math.Min(i,_rules.Count-1);}
 bool Apply(){if(_selected is null)return false;if(string.IsNullOrWhiteSpace(NameBox.Text)){ValidationText.Text="Name is required.";return false;}if(!double.TryParse(UsageBox.Text,out var mb)||mb<0||!double.TryParse(SpeedBox.Text,out var speed)||speed<0||!double.TryParse(MinimumBox.Text,out var min)||min<0||!int.TryParse(DurationBox.Text,out var seconds)||seconds<0||!int.TryParse(CooldownBox.Text,out var cool)||cool<0){ValidationText.Text="Use valid non-negative numbers.";return false;}_selected.Name=NameBox.Text.Trim();_selected.Enabled=EnabledBox.IsChecked==true;_selected.Type=SelectedValue<AlertRuleType>(TypeBox,AlertRuleType.ApplicationSessionUsage);_selected.Scope=SelectedValue<AlertScope>(ScopeBox,AlertScope.AllApplications);_selected.ProcessName=string.IsNullOrWhiteSpace(ProcessBox.Text)?null:ProcessBox.Text.Trim();_selected.ThresholdKB=mb*1024;_selected.ThresholdKBps=speed*1024;_selected.MinimumTrafficKB=min*1024;_selected.SustainedSeconds=seconds;_selected.CooldownMinutes=cool;_selected.ModifiedUtc=DateTime.UtcNow;RulesList.Items.Refresh();ValidationText.Text="Applied";return true;}
 void SaveRule_Click(object s,RoutedEventArgs e)=>Apply();void SaveAll_Click(object s,RoutedEventArgs e){if(_selected is not null&&!Apply())return;_store.Save(_rules);DialogResult=true;Close();}
 static void FillCombo(System.Windows.Controls.ComboBox box, IEnumerable<(object Value,string Label)> values)
 {
  box.Items.Clear();
  foreach(var entry in values)
  {
   var text=new System.Windows.Controls.TextBlock{Text=entry.Label,Foreground=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(17,17,17))};
   box.Items.Add(new System.Windows.Controls.ComboBoxItem{Content=text,Tag=entry.Value,Foreground=System.Windows.Media.Brushes.Black,Background=System.Windows.Media.Brushes.White});
  }
 }
 static void SelectCombo(System.Windows.Controls.ComboBox box,object value)
 {
  foreach(System.Windows.Controls.ComboBoxItem item in box.Items)if(Equals(item.Tag,value)){box.SelectedItem=item;break;}
 }
 static T SelectedValue<T>(System.Windows.Controls.ComboBox box,T fallback)=>box.SelectedItem is System.Windows.Controls.ComboBoxItem item&&item.Tag is T value?value:fallback;


}
