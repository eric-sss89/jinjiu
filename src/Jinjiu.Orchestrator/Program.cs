using System.Drawing;
using System.Text.Json;
using System.Text.Json.Nodes;

internal sealed class InputOptions { public string CaptureDir { get; set; } = "../Jinjiu.Capture/captures"; public int PollMs { get; set; } = 500; }
internal sealed class RuleOptions
{
    public double SceneChangeThreshold { get; set; } = 0.18;
    public double StalledDiffThreshold { get; set; } = 0.01;
    public int StalledSeconds { get; set; } = 12;
    public double HpLowThreshold { get; set; } = 0.25;
    public double TargetAliveThreshold { get; set; } = 0.05;
    public int ActionCooldownMs { get; set; } = 1200;
    public int StateConfirmFrames { get; set; } = 3;
    public int MinStateDwellMs { get; set; } = 800;
    public int ActionDedupWindowSec { get; set; } = 4;
}
internal sealed class RoiRect { public double X { get; set; } public double Y { get; set; } public double W { get; set; } public double H { get; set; } }
internal sealed class RoiOptions { public RoiRect PlayerHp { get; set; } = new(); public RoiRect TargetHp { get; set; } = new(); }
internal sealed class OutputOptions { public string OutboxDir { get; set; } = "outbox"; public string StateFile { get; set; } = "game_state.json"; public string ActionQueueFile { get; set; } = "action_queue.jsonl"; }
internal sealed class AppOptions { public InputOptions Input { get; set; } = new(); public RuleOptions Rules { get; set; } = new(); public RoiOptions Roi { get; set; } = new(); public OutputOptions Output { get; set; } = new(); }

internal static class Program
{
    private static string? _lastFramePath;
    private static DateTimeOffset? _stalledSince;
    private static DateTimeOffset _lastActionAt = DateTimeOffset.MinValue;
    private static readonly Dictionary<string, DateTimeOffset> RecentIntents = new();

    private static string _stableMode = "IDLE";
    private static DateTimeOffset _stableModeSince = DateTimeOffset.MinValue;
    private static string _candidateMode = "IDLE";
    private static int _candidateCount = 0;

    private static async Task Main()
    {
        var opt = LoadOptions();
        Directory.CreateDirectory(opt.Output.OutboxDir);
        var statePath = Path.Combine(opt.Output.OutboxDir, opt.Output.StateFile);
        var actionPath = Path.Combine(opt.Output.OutboxDir, opt.Output.ActionQueueFile);

        _stableModeSince = DateTimeOffset.Now;
        _candidateMode = _stableMode;

        Console.WriteLine("[Jinjiu.Orchestrator] MVP started");

        while (true)
        {
            try
            {
                var latest = GetLatestFrame(opt.Input.CaptureDir);
                if (latest is null || latest == _lastFramePath)
                {
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }

                using var cur = new Bitmap(latest);
                var hpPct = EstimateRedFill(cur, opt.Roi.PlayerHp);
                var targetHpPct = EstimateRedFill(cur, opt.Roi.TargetHp);

                double diff = 0;
                if (!string.IsNullOrEmpty(_lastFramePath) && File.Exists(_lastFramePath))
                {
                    using var prev = new Bitmap(_lastFramePath);
                    diff = ComputeDiffRatio(prev, cur, 10);
                }

                var isStalledNow = diff < opt.Rules.StalledDiffThreshold;
                if (isStalledNow) _stalledSince ??= DateTimeOffset.Now;
                else _stalledSince = null;

                var stalledSeconds = _stalledSince is null ? 0 : (DateTimeOffset.Now - _stalledSince.Value).TotalSeconds;

                var rawMode = InferRawMode(hpPct, targetHpPct, diff, stalledSeconds, opt);
                var modeChanged = UpdateStableMode(rawMode, opt);

                var state = BuildState(hpPct, targetHpPct, diff, stalledSeconds, rawMode, _stableMode, modeChanged);
                File.WriteAllText(statePath, state.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                var action = DecideAction(_stableMode);
                if (action is not null
                    && (DateTimeOffset.Now - _lastActionAt).TotalMilliseconds >= opt.Rules.ActionCooldownMs
                    && !IsDuplicateIntent(action, opt.Rules.ActionDedupWindowSec))
                {
                    AppendJsonLine(actionPath, action);
                    _lastActionAt = DateTimeOffset.Now;
                    RememberIntent(action);
                    Console.WriteLine($"[action] {action.ToJsonString()}");
                }

                CleanupIntentCache(opt.Rules.ActionDedupWindowSec);

                _lastFramePath = latest;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[error] {ex.Message}");
            }

            await Task.Delay(opt.Input.PollMs);
        }
    }

    private static string InferRawMode(double hpPct, double targetHpPct, double diff, double stalledSeconds, AppOptions opt)
    {
        var mode = "IDLE";
        if (hpPct < opt.Rules.HpLowThreshold) mode = "RECOVER";
        else if (targetHpPct > opt.Rules.TargetAliveThreshold) mode = "COMBAT";
        else if (diff > opt.Rules.SceneChangeThreshold) mode = "SEARCH";
        if (stalledSeconds >= opt.Rules.StalledSeconds) mode = "ERROR";
        return mode;
    }

    private static bool UpdateStableMode(string rawMode, AppOptions opt)
    {
        if (rawMode == _stableMode)
        {
            _candidateMode = rawMode;
            _candidateCount = 0;
            return false;
        }

        if (_candidateMode != rawMode)
        {
            _candidateMode = rawMode;
            _candidateCount = 1;
            return false;
        }

        _candidateCount++;

        var dwellOk = (DateTimeOffset.Now - _stableModeSince).TotalMilliseconds >= opt.Rules.MinStateDwellMs;
        var confirmOk = _candidateCount >= opt.Rules.StateConfirmFrames;

        if (dwellOk && confirmOk)
        {
            var from = _stableMode;
            _stableMode = _candidateMode;
            _stableModeSince = DateTimeOffset.Now;
            _candidateCount = 0;
            Console.WriteLine($"[state] {from} -> {_stableMode}");
            return true;
        }

        return false;
    }

    private static JsonObject BuildState(double hpPct, double targetHpPct, double diff, double stalledSeconds, string rawMode, string stableMode, bool modeChanged)
    {
        return new JsonObject
        {
            ["time"] = DateTimeOffset.Now.ToString("O"),
            ["rawMode"] = rawMode,
            ["mode"] = stableMode,
            ["modeChanged"] = modeChanged,
            ["hpPct"] = Math.Round(hpPct, 3),
            ["targetHpPct"] = Math.Round(targetHpPct, 3),
            ["frameDiff"] = Math.Round(diff, 4),
            ["stalledSeconds"] = Math.Round(stalledSeconds, 1)
        };
    }

    private static JsonObject? DecideAction(string mode)
    {
        return mode switch
        {
            "RECOVER" => Action("use_potion", "hp_low"),
            "COMBAT" => Action("cast_skill_1", "target_alive"),
            "SEARCH" => Action("tab_target", "no_target_search"),
            "ERROR" => Action("unstuck_move", "screen_stalled"),
            _ => null
        };
    }

    private static JsonObject Action(string action, string reason) => new()
    {
        ["time"] = DateTimeOffset.Now.ToString("O"),
        ["action"] = action,
        ["reason"] = reason,
        ["target"] = "input_driver"
    };

    private static double EstimateRedFill(Bitmap bmp, RoiRect r)
    {
        var rect = ToRect(bmp.Width, bmp.Height, r);
        if (rect.Width <= 1 || rect.Height <= 1) return 0;

        long redCount = 0, total = 0;
        for (int y = rect.Top; y < rect.Bottom; y += 2)
        for (int x = rect.Left; x < rect.Right; x += 2)
        {
            var c = bmp.GetPixel(x, y);
            if (c.R > 120 && c.R > c.G * 1.2 && c.R > c.B * 1.2) redCount++;
            total++;
        }

        return total == 0 ? 0 : (double)redCount / total;
    }

    private static Rectangle ToRect(int w, int h, RoiRect r)
    {
        var x = Math.Clamp((int)(r.X * w), 0, w - 1);
        var y = Math.Clamp((int)(r.Y * h), 0, h - 1);
        var rw = Math.Clamp((int)(r.W * w), 1, w - x);
        var rh = Math.Clamp((int)(r.H * h), 1, h - y);
        return new Rectangle(x, y, rw, rh);
    }

    private static double ComputeDiffRatio(Bitmap a, Bitmap b, int step)
    {
        var w = Math.Min(a.Width, b.Width);
        var h = Math.Min(a.Height, b.Height);
        long changed = 0, total = 0;
        for (int y = 0; y < h; y += step)
        for (int x = 0; x < w; x += step)
        {
            var c1 = a.GetPixel(x, y);
            var c2 = b.GetPixel(x, y);
            var d = (Math.Abs(c1.R - c2.R) + Math.Abs(c1.G - c2.G) + Math.Abs(c1.B - c2.B)) / 3.0;
            if (d > 20) changed++;
            total++;
        }
        return total == 0 ? 0 : (double)changed / total;
    }

    private static string? GetLatestFrame(string dir)
        => Directory.Exists(dir)
            ? Directory.GetFiles(dir, "*.jpg").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;

    private static void AppendJsonLine(string path, JsonObject node)
        => File.AppendAllText(path, node.ToJsonString() + Environment.NewLine);

    private static bool IsDuplicateIntent(JsonObject action, int dedupWindowSec)
    {
        var act = action["action"]?.GetValue<string>() ?? "unknown";
        var reason = action["reason"]?.GetValue<string>() ?? "na";
        var key = $"{act}|{reason}";
        if (!RecentIntents.TryGetValue(key, out var last)) return false;
        return (DateTimeOffset.Now - last).TotalSeconds < dedupWindowSec;
    }

    private static void RememberIntent(JsonObject action)
    {
        var act = action["action"]?.GetValue<string>() ?? "unknown";
        var reason = action["reason"]?.GetValue<string>() ?? "na";
        var key = $"{act}|{reason}";
        RecentIntents[key] = DateTimeOffset.Now;
    }

    private static void CleanupIntentCache(int dedupWindowSec)
    {
        var now = DateTimeOffset.Now;
        var dead = RecentIntents.Where(kv => (now - kv.Value).TotalSeconds > dedupWindowSec * 2).Select(kv => kv.Key).ToList();
        foreach (var key in dead) RecentIntents.Remove(key);
    }

    private static AppOptions LoadOptions()
    {
        var p = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(p)) return new AppOptions();
        var root = JsonNode.Parse(File.ReadAllText(p));
        var opt = new AppOptions();

        var input = root?["Input"]; var rules = root?["Rules"]; var roi = root?["Roi"]; var output = root?["Output"];
        if (input is not null)
        {
            opt.Input.CaptureDir = input["CaptureDir"]?.GetValue<string>() ?? opt.Input.CaptureDir;
            opt.Input.PollMs = input["PollMs"]?.GetValue<int>() ?? opt.Input.PollMs;
        }
        if (rules is not null)
        {
            opt.Rules.SceneChangeThreshold = rules["SceneChangeThreshold"]?.GetValue<double>() ?? opt.Rules.SceneChangeThreshold;
            opt.Rules.StalledDiffThreshold = rules["StalledDiffThreshold"]?.GetValue<double>() ?? opt.Rules.StalledDiffThreshold;
            opt.Rules.StalledSeconds = rules["StalledSeconds"]?.GetValue<int>() ?? opt.Rules.StalledSeconds;
            opt.Rules.HpLowThreshold = rules["HpLowThreshold"]?.GetValue<double>() ?? opt.Rules.HpLowThreshold;
            opt.Rules.TargetAliveThreshold = rules["TargetAliveThreshold"]?.GetValue<double>() ?? opt.Rules.TargetAliveThreshold;
            opt.Rules.ActionCooldownMs = rules["ActionCooldownMs"]?.GetValue<int>() ?? opt.Rules.ActionCooldownMs;
            opt.Rules.StateConfirmFrames = rules["StateConfirmFrames"]?.GetValue<int>() ?? opt.Rules.StateConfirmFrames;
            opt.Rules.MinStateDwellMs = rules["MinStateDwellMs"]?.GetValue<int>() ?? opt.Rules.MinStateDwellMs;
            opt.Rules.ActionDedupWindowSec = rules["ActionDedupWindowSec"]?.GetValue<int>() ?? opt.Rules.ActionDedupWindowSec;
        }
        if (roi is not null)
        {
            opt.Roi.PlayerHp = ReadRoi(roi["PlayerHp"]) ?? opt.Roi.PlayerHp;
            opt.Roi.TargetHp = ReadRoi(roi["TargetHp"]) ?? opt.Roi.TargetHp;
        }
        if (output is not null)
        {
            opt.Output.OutboxDir = output["OutboxDir"]?.GetValue<string>() ?? opt.Output.OutboxDir;
            opt.Output.StateFile = output["StateFile"]?.GetValue<string>() ?? opt.Output.StateFile;
            opt.Output.ActionQueueFile = output["ActionQueueFile"]?.GetValue<string>() ?? opt.Output.ActionQueueFile;
        }

        return opt;
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
