using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Shapes;using SiamNetworkMonitor.Services;
namespace SiamNetworkMonitor;
public partial class HistoricalChartControl:System.Windows.Controls.UserControl
{
 IReadOnlyList<(DateTime Time,double Down,double Up)> _points=[];
 public HistoricalChartControl()=>InitializeComponent();
 public void SetPoints(IReadOnlyList<(DateTime Time,double Down,double Up)> points){_points=points.TakeLast(240).ToList();Draw();}
 void Draw(){if(ChartCanvas.ActualWidth<30||ChartCanvas.ActualHeight<30)return;ChartCanvas.Children.Clear();double w=ChartCanvas.ActualWidth,h=ChartCanvas.ActualHeight,max=Math.Max(1,_points.SelectMany(x=>new[]{x.Down,x.Up}).DefaultIfEmpty().Max());for(int i=0;i<=4;i++){double y=h*i/4;ChartCanvas.Children.Add(new Line{X1=0,X2=w,Y1=y,Y2=y,Stroke=new SolidColorBrush(System.Windows.Media.Color.FromArgb(55,110,140,170)),StrokeThickness=1});var label=new TextBlock{Text=DisplayFormatter.Data(max*(4-i)/4),Foreground=new SolidColorBrush(System.Windows.Media.Color.FromRgb(170,190,210)),FontSize=10};Canvas.SetLeft(label,5);Canvas.SetTop(label,Math.Max(0,y-15));ChartCanvas.Children.Add(label);}Series(_points.Select(x=>x.Down),max,w,h,(System.Windows.Media.Brush)FindResource("DownloadBrush"));Series(_points.Select(x=>x.Up),max,w,h,(System.Windows.Media.Brush)FindResource("UploadBrush"));}
 void Series(IEnumerable<double> values,double max,double w,double h,System.Windows.Media.Brush brush){var a=values.ToArray();if(a.Length<2)return;var line=new Polyline{Stroke=brush,StrokeThickness=2.5,StrokeLineJoin=PenLineJoin.Round};for(int i=0;i<a.Length;i++)line.Points.Add(new System.Windows.Point(i*w/Math.Max(1,a.Length-1),h-Math.Max(0,a[i])/max*h));ChartCanvas.Children.Add(line);}
 void ChartCanvas_SizeChanged(object s,SizeChangedEventArgs e)=>Draw();
}
