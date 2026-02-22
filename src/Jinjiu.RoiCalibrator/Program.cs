using System.Drawing;
using System.Text.Json.Nodes;

internal sealed class RoiRect { public double X { get; set; } public double Y { get; set; } public double W { get; set; } public double H { get; set; } }
internal sealed class AppOptions
{
    public string CaptureDir { get; set; } = "../Jinjiu.Capture/captures";
    public RoiRect PlayerHp { get; set; } = new() { X = 0.44, Y = 0.95, W = 0.12, H = 0.02 };
    public RoiRect TargetHp { get; set; } = new() { X = 0.40, Y = 0.07, W = 0.20, H = 0.02 };
    public string PreviewDir { get; set; } = "preview";
    public string FileName { get; set; } = "roi_preview.jpg";
}

internal static class Program
{
    private static int Main()
    {
        var opt = Load();
        var frame = GetLatestFrame(opt.CaptureDir);
        if (frame is null)
        {
            Console.WriteLine($"[error] no frame found in {Path.GetFullPath(opt.CaptureDir)}");
            return 1;
        }

        Directory.CreateDirectory(opt.PreviewDir);
        var outPath = Path.Combine(opt.PreviewDir, opt.FileName);

        using var bmp = new Bitmap(frame);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        DrawRoi(g, bmp.Size, opt.PlayerHp, Color.LimeGreen, "PlayerHp");
        DrawRoi(g, bmp.Size, opt.TargetHp, Color.OrangeRed, "TargetHp");

        bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Jpeg);

        Console.WriteLine("[Jinjiu.RoiCalibrator] preview generated");
        Console.WriteLine($"frame   : {Path.GetFullPath(frame)}");
        Console.WriteLine($"preview : {Path.GetFullPath(outPath)}");
        return 0;
    }

    private static void DrawRoi(Graphics g, Size size, RoiRect roi, Color color, string label)
    {
        var rect = ToRect(size.Width, size.Height, roi);
        using var pen = new Pen(color, 3f);
        using var brush = new SolidBrush(Color.FromArgb(120, color));
        using var textBrush = new SolidBrush(color);
        using var font = new Font("Segoe UI", 16, FontStyle.Bold);

        g.DrawRectangle(pen, rect);
        g.FillRectangle(brush, rect);
        g.DrawString(label, font, textBrush, rect.Left + 4, Math.Max(0, rect.Top - 28));
    }

    private static Rectangle ToRect(int w, int h, RoiRect r)
    {
        var x = Math.Clamp((int)(r.X * w), 0, w - 1);
        var y = Math.Clamp((int)(r.Y * h), 0, h - 1);
        var rw = Math.Clamp((int)(r.W * w), 1, w - x);
        var rh = Math.Clamp((int)(r.H * h), 1, h - y);
        return new Rectangle(x, y, rw, rh);
    }

    private static string? GetLatestFrame(string dir)
        => Directory.Exists(dir)
            ? Directory.GetFiles(dir, "*.jpg").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;

    private static AppOptions Load()
    {
        var p = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(p)) return new AppOptions();

        var root = JsonNode.Parse(File.ReadAllText(p));
        var o = new AppOptions();
        o.CaptureDir = root?["Input"]?["CaptureDir"]?.GetValue<string>() ?? o.CaptureDir;

        o.PlayerHp = ReadRoi(root?["Roi"]?["PlayerHp"]) ?? o.PlayerHp;
        o.TargetHp = ReadRoi(root?["Roi"]?["TargetHp"]) ?? o.TargetHp;

        o.PreviewDir = root?["Output"]?["PreviewDir"]?.GetValue<string>() ?? o.PreviewDir;
        o.FileName = root?["Output"]?["FileName"]?.GetValue<string>() ?? o.FileName;
        return o;
    }

    private static RoiRect? ReadRoi(JsonNode? n)
        => n is null ? null : new RoiRect
        {
            X = n["X"]?.GetValue<double>() ?? 0,
            Y = n["Y"]?.GetValue<double>() ?? 0,
            W = n["W"]?.GetValue<double>() ?? 0,
            H = n["H"]?.GetValue<double>() ?? 0
        };
}
