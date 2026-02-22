using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

internal sealed class CaptureOptions
{
    public int IntervalMs { get; set; } = 1000;
    public string OutputDir { get; set; } = "captures";
    public long JpegQuality { get; set; } = 80;
    public int MaxFrames { get; set; } = 0;
}

internal static class Program
{
    [STAThread]
    private static async Task Main()
    {
        var options = LoadOptions();

        Directory.CreateDirectory(options.OutputDir);

        Console.WriteLine("[Jinjiu.Capture] started");
        Console.WriteLine($"[Jinjiu.Capture] interval={options.IntervalMs}ms output={Path.GetFullPath(options.OutputDir)} quality={options.JpegQuality} maxFrames={options.MaxFrames}");

        int frameCount = 0;
        while (options.MaxFrames <= 0 || frameCount < options.MaxFrames)
        {
            var now = DateTimeOffset.Now;
            var bounds = Screen.PrimaryScreen?.Bounds ?? throw new InvalidOperationException("No primary screen found.");

            var filename = $"frame_{now:yyyyMMdd_HHmmss_fff}.jpg";
            var path = Path.Combine(options.OutputDir, filename);

            CapturePrimaryScreen(bounds, path, options.JpegQuality);

            frameCount++;

            var log = new JsonObject
            {
                ["time"] = now.ToString("O"),
                ["frameIndex"] = frameCount,
                ["width"] = bounds.Width,
                ["height"] = bounds.Height,
                ["path"] = Path.GetFullPath(path)
            };

            Console.WriteLine(log.ToJsonString());
            await Task.Delay(options.IntervalMs);
        }

        Console.WriteLine("[Jinjiu.Capture] finished");
    }

    private static CaptureOptions LoadOptions()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return new CaptureOptions();
        }

        var json = File.ReadAllText(path);
        var root = JsonNode.Parse(json);
        var node = root?["Capture"];
        if (node is null)
        {
            return new CaptureOptions();
        }

        return new CaptureOptions
        {
            IntervalMs = node["IntervalMs"]?.GetValue<int>() ?? 1000,
            OutputDir = node["OutputDir"]?.GetValue<string>() ?? "captures",
            JpegQuality = node["JpegQuality"]?.GetValue<long>() ?? 80,
            MaxFrames = node["MaxFrames"]?.GetValue<int>() ?? 0
        };
    }

    private static void CapturePrimaryScreen(Rectangle bounds, string outputPath, long quality)
    {
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }

        var jpegEncoder = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
        using var encParams = new EncoderParameters(1);
        encParams.Param[0] = new EncoderParameter(Encoder.Quality, Math.Clamp(quality, 1, 100));
        bmp.Save(outputPath, jpegEncoder, encParams);
    }
}
