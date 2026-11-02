using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BvhViewer.Bvh;

namespace BvhViewer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length >= 1 && e.Args[0] == "--export")
        {
            string bvhPath = e.Args.Length > 1 ? e.Args[1] : "sample.bvh";
            string outputDir = e.Args.Length > 2 ? e.Args[2] : "frames";
            ExportFrames(bvhPath, outputDir);
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private static void ExportFrames(string bvhPath, string outputDir)
    {
        if (!File.Exists(bvhPath))
        {
            Console.WriteLine($"Error: BVH file not found: {bvhPath}");
            return;
        }

        Directory.CreateDirectory(outputDir);
        var animation = BvhParser.Parse(File.ReadAllText(bvhPath));
        Console.WriteLine($"Exporting {animation.FrameCount} frames for {bvhPath}...");

        int width = 960;
        int height = 640;

        // 計算整體邊界
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int f = 0; f < animation.FrameCount; f++)
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

        var bWidth = Math.Max(maxX - minX, 1f);
        var bHeight = Math.Max(maxY - minY, 1f);
        double scale = Math.Min(width / bWidth, height / bHeight) * 0.75;
        double cx = (minX + maxX) / 2.0;
        double cy = (minY + maxY) / 2.0;
        var center = new Point(width / 2.0, height / 2.0 + 30);

        Point Project(Vector3 p) => new(
            center.X + (p.X - cx) * scale,
            center.Y - (p.Y - cy) * scale
        );

        var bgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121418"));
        var bonePen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")), 3.5);
        bonePen.Freeze();
        var jointBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
        jointBrush.Freeze();
        var jointStroke = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7")), 1.5);
        jointStroke.Freeze();
        var headBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F472B6"));
        headBrush.Freeze();

        for (int f = 0; f < animation.FrameCount; f++)
        {
            var bones = animation.GetBones(f);
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(bgBrush, null, new Rect(0, 0, width, height));

                // 繪製骨架骨骼線
                foreach (var bone in bones)
                {
                    var from = Project(bone.From);
                    var to = Project(bone.To);
                    dc.DrawLine(bonePen, from, to);
                    dc.DrawEllipse(jointBrush, jointStroke, from, 4.5, 4.5);
                    dc.DrawEllipse(jointBrush, jointStroke, to, 4.5, 4.5);
                }

                // 標題與資訊
                var typeface = new Typeface("Segoe UI Semibold");
                var titleText = new FormattedText(
                    "BVH Animation Viewer",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    20,
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                    1.25);
                dc.DrawText(titleText, new Point(30, 25));

                var subText = new FormattedText(
                    $"File: {Path.GetFileName(bvhPath)} | Frame: {f + 1:D2} / {animation.FrameCount:D2} | FPS: {Math.Round(1.0 / animation.FrameTime)}",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    14,
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    1.25);
                dc.DrawText(subText, new Point(30, 55));
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            string framePath = Path.Combine(outputDir, $"frame_{f:D3}.png");
            using var fs = File.Create(framePath);
            encoder.Save(fs);
        }

        Console.WriteLine($"Successfully exported {animation.FrameCount} frames to {outputDir}");
    }
}
