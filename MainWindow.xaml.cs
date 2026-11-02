using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using BvhViewer.Bvh;

namespace BvhViewer;

public partial class MainWindow : Window
{
    private readonly System.Windows.Threading.DispatcherTimer timer = new();
    private BvhAnimation? animation;
    private string? currentFile;
    private double speed = 1;

    public MainWindow()
    {
        InitializeComponent();
        timer.Tick += (_, _) =>
        {
            if (animation is null) return;
            Timeline.Value = (Timeline.Value + animation.FrameTime * speed) % Math.Max(animation.Duration, animation.FrameTime);
        };
        UpdateUi();
    }

    private void OpenClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "BVH files (*.bvh)|*.bvh|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            animation = BvhParser.Parse(File.ReadAllText(dialog.FileName));
            currentFile = dialog.FileName;
            Timeline.Maximum = animation.Duration;
            Timeline.Value = 0;
            globalScale = null;
            UpdateUi();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or OverflowException)
        {
            MessageBox.Show(this, $"無法讀取 BVH 檔案：{ex.Message}", "BVH 錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PlayClick(object sender, RoutedEventArgs e)
    {
        if (animation is null) return;
        if (timer.IsEnabled) { timer.Stop(); PlayButton.Content = "播放"; }
        else { timer.Interval = TimeSpan.FromSeconds(Math.Max(animation.FrameTime, 1.0 / 120)); timer.Start(); PlayButton.Content = "暫停"; }
    }

    private void ResetClick(object sender, RoutedEventArgs e) => Timeline.Value = 0;
    private void ExitClick(object sender, RoutedEventArgs e) => Close();
    private void SpeedChanged(object sender, SelectionChangedEventArgs e) => speed = SpeedBox.SelectedIndex switch { 0 => .25, 1 => .5, 3 => 2, _ => 1 };
    private void TimelineChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Render(e.NewValue);
    
    private void ViewportChanged(object sender, SizeChangedEventArgs e) 
    { 
        globalScale = null;
        Render(Timeline.Value); 
    }

    private void UpdateUi()
    {
        FileLabel.Text = currentFile is null ? "尚未開啟 BVH 檔案" : System.IO.Path.GetFileName(currentFile);
        InfoLabel.Text = animation is null ? "請從「檔案 > 開啟 BVH」載入動畫。" : $"{animation.FrameCount} frames | {animation.FrameTime:0.0000}s/frame | {animation.ChannelCount} channels";
        FrameLabel.Text = animation is null ? "Frame 0 / 0" : $"Frame {Math.Min(animation.FrameCount - 1, (int)(Timeline.Value / animation.FrameTime))} / {animation.FrameCount}";
        Render(Timeline.Value);
    }

    private double? globalCx;
    private double? globalCy;
    private double? globalScale;

    private void Render(double time)
    {
        Viewport.Children.Clear();
        if (animation is null || Viewport.ActualWidth < 1 || Viewport.ActualHeight < 1) return;

        if (globalScale is null)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int f = 0; f < animation.FrameCount; f += Math.Max(1, animation.FrameCount / 100)) 
            {
                var fBones = animation.GetBones(f);
                foreach (var bone in fBones)
                {
                    minX = Math.Min(minX, Math.Min(bone.From.X, bone.To.X));
                    minY = Math.Min(minY, Math.Min(bone.From.Y, bone.To.Y));
                    maxX = Math.Max(maxX, Math.Max(bone.From.X, bone.To.X));
                    maxY = Math.Max(maxY, Math.Max(bone.From.Y, bone.To.Y));
                }
            }
            var width = Math.Max(maxX - minX, 1f);
            var height = Math.Max(maxY - minY, 1f);
            globalScale = Math.Min(Viewport.ActualWidth / width, Viewport.ActualHeight / height) * 0.9;
            globalCx = (minX + maxX) / 2;
            globalCy = (minY + maxY) / 2;
        }

        var frame = Math.Clamp((int)(time / animation.FrameTime), 0, animation.FrameCount - 1);
        var bones = animation.GetBones(frame);
        
        if (bones.Count == 0) return;

        var center = new Point(Viewport.ActualWidth / 2, Viewport.ActualHeight / 2);
        var scale = globalScale!.Value;
        var cx = globalCx!.Value;
        var cy = globalCy!.Value;
        Point Project(System.Numerics.Vector3 p) => new(center.X + (p.X - cx) * scale, center.Y - (p.Y - cy) * scale);

        foreach (var bone in bones)
        {
            var from = Project(bone.From);
            var to = Project(bone.To);
            Viewport.Children.Add(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.DeepSkyBlue, StrokeThickness = 3 });
            
            var dot = new Ellipse { Width = 8, Height = 8, Fill = Brushes.White };
            Canvas.SetLeft(dot, from.X - 4); Canvas.SetTop(dot, from.Y - 4); Viewport.Children.Add(dot);
            
            var dotEnd = new Ellipse { Width = 8, Height = 8, Fill = Brushes.White };
            Canvas.SetLeft(dotEnd, to.X - 4); Canvas.SetTop(dotEnd, to.Y - 4); Viewport.Children.Add(dotEnd);
        }
        FrameLabel.Text = $"Frame {frame} / {animation.FrameCount}";
    }
}
