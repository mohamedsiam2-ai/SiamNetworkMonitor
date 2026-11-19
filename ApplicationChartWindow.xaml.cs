using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using SiamNetworkMonitor.Services;
using SiamNetworkMonitor.ViewModels;

namespace SiamNetworkMonitor;

public partial class ApplicationChartWindow : Window
{
    private const int Capacity = 120;
    private readonly ProcessRowViewModel _row;
    private readonly Queue<(double Down, double Up)> _samples = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public ApplicationChartWindow(ProcessRowViewModel row)
    {
        InitializeComponent();
        _row = row;
        AppNameText.Text = row.Description;
        ProcessText.Text = $"{row.ProcessName} • PID {row.ProcessId} • {row.Publisher}";
        _timer.Tick += OnTick;
        Loaded += (_, _) =>
        {
            AddSample();
            _timer.Start();
        };
        Closed += (_, _) => _timer.Stop();
    }

    private void OnTick(object? sender, EventArgs e) => AddSample();

    private void AddSample()
    {
        _samples.Enqueue((_row.DownloadKBps, _row.UploadKBps));
        while (_samples.Count > Capacity)
            _samples.Dequeue();

        DownloadText.Text = DisplayFormatter.Rate(_row.DownloadKBps);
        UploadText.Text = DisplayFormatter.Rate(_row.UploadKBps);
        Draw();
    }

    private void Draw()
    {
        if (ChartCanvas.ActualWidth < 20 || ChartCanvas.ActualHeight < 20)
            return;

        ChartCanvas.Children.Clear();
        double width = ChartCanvas.ActualWidth;
        double height = ChartCanvas.ActualHeight;
        double maximum = Math.Max(
            1,
            _samples.SelectMany(sample => new[] { sample.Down, sample.Up })
                .DefaultIfEmpty(0)
                .Max());

        maximum = NiceMaximum(maximum);

        for (int index = 0; index <= 4; index++)
        {
            double y = height * index / 4;
            var line = new Line
            {
                X1 = 0,
                X2 = width,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(70, 80, 110, 140)),
                StrokeThickness = 1
            };
            ChartCanvas.Children.Add(line);

            var label = new TextBlock
            {
                Text = DisplayFormatter.Rate(maximum * (4 - index) / 4),
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(154, 174, 200)),
                FontSize = 10
            };
            Canvas.SetLeft(label, 5);
            Canvas.SetTop(label, Math.Max(0, y - 15));
            ChartCanvas.Children.Add(label);
        }

        DrawSeries(
            _samples.Select(sample => sample.Down),
            maximum,
            width,
            height,
            (System.Windows.Media.Brush)FindResource("DownloadBrush"));

        DrawSeries(
            _samples.Select(sample => sample.Up),
            maximum,
            width,
            height,
            (System.Windows.Media.Brush)FindResource("UploadBrush"));

        ScaleText.Text = $"Peak scale: {DisplayFormatter.Rate(maximum)} • Samples: {_samples.Count}/{Capacity}";
    }

    private void DrawSeries(
        IEnumerable<double> values,
        double maximum,
        double width,
        double height,
        System.Windows.Media.Brush brush)
    {
        double[] samples = values.ToArray();
        if (samples.Length < 2)
            return;

        var polyline = new Polyline
        {
            Stroke = brush,
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        };

        for (int index = 0; index < samples.Length; index++)
        {
            polyline.Points.Add(new System.Windows.Point(
                index * width / (Capacity - 1),
                height - Math.Max(0, samples[index]) / maximum * height));
        }

        ChartCanvas.Children.Add(polyline);
    }

    private static double NiceMaximum(double value)
    {
        double power = Math.Pow(10, Math.Floor(Math.Log10(value)));
        double normalized = value / power;
        double nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return nice * power;
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _samples.Clear();
        AddSample();
    }

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Draw();
}
