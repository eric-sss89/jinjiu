using System.Text.Json.Nodes;

internal sealed class InputOptions { public string ActionQueueFile { get; set; } = "../Jinjiu.Orchestrator/outbox/action_queue.jsonl"; public int PollMs { get; set; } = 300; }
internal sealed class DriverOptions
{
    public bool SimulationOnly { get; set; } = true;
    public int MinIntervalMs { get; set; } = 800;
    public int DedupWindowSec { get; set; } = 5;
    public string EnabledFlagFile { get; set; } = "../Jinjiu.Orchestrator/outbox/driver.enabled";
    public string StopFlagFile { get; set; } = "../Jinjiu.Orchestrator/outbox/driver.stop";
    public List<string> AllowedActions { get; set; } = new();
    public bool FocusGuardEnabled { get; set; } = false;
    public string FocusHintFile { get; set; } = "../Jinjiu.Orchestrator/outbox/focus_window.txt";
    public List<string> AllowedWindowKeywords { get; set; } = new();
}
internal sealed class AppOptions { public InputOptions Input { get; set; } = new(); public DriverOptions Driver { get; set; } = new(); }

internal static class Program
{
    private static long _offset;
    private static DateTimeOffset _lastExecAt = DateTimeOffset.MinValue;
    private static readonly Dictionary<string, DateTimeOffset> RecentActions = new();

    private static async Task Main()
    {
        var opt = Load();
        Console.WriteLine("[Jinjiu.InputDriver] started (simulation mode)");

        while (true)
        {
            try
            {
                if (File.Exists(opt.Driver.StopFlagFile))
                {
                    Console.WriteLine($"[stop] stop flag detected: {Path.GetFullPath(opt.Driver.StopFlagFile)}");
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }

                if (!File.Exists(opt.Driver.EnabledFlagFile))
                {
                    Console.WriteLine("[paused] driver.enabled missing, waiting...");
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }

                if (!File.Exists(opt.Input.ActionQueueFile))
                {
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }

                using var fs = new FileStream(opt.Input.ActionQueueFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs.Seek(_offset, SeekOrigin.Begin);
                using var sr = new StreamReader(fs);

                string? line;
                while ((line = await sr.ReadLineAsync()) is not null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var node = JsonNode.Parse(line)?.AsObject();
                    if (node is null) continue;

                    if (!TryExecute(node, opt.Driver)) continue;
                }

                _offset = fs.Position;
                CleanupDedup(opt.Driver.DedupWindowSec);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[error] {ex.Message}");
            }

            await Task.Delay(opt.Input.PollMs);
        }
    }

    private static bool TryExecute(JsonObject action, DriverOptions opt)
    {
        var act = action["action"]?.GetValue<string>() ?? "unknown";
        var reason = action["reason"]?.GetValue<string>() ?? "na";
        var key = $"{act}|{reason}";
        var now = DateTimeOffset.Now;

        if (opt.AllowedActions.Count > 0 && !opt.AllowedActions.Contains(act, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[skip] action not allowed: {act}");
            return false;
        }

        if (opt.FocusGuardEnabled && !IsFocusAllowed(opt))
        {
            Console.WriteLine("[skip] focus guard blocked action");
            return false;
        }

        if ((now - _lastExecAt).TotalMilliseconds < opt.MinIntervalMs)
        {
            Console.WriteLine($"[skip] rate-limit action={act}");
            return false;
        }

        if (RecentActions.TryGetValue(key, out var last) && (now - last).TotalSeconds < opt.DedupWindowSec)
        {
            Console.WriteLine($"[skip] dedup action={act} reason={reason}");
            return false;
        }

        Console.WriteLine($"[execute] action={act} reason={reason} simulation={opt.SimulationOnly}");
        // TODO: 后续替换为真实键鼠注入

        _lastExecAt = now;
        RecentActions[key] = now;
        return true;
    }

    private static bool IsFocusAllowed(DriverOptions opt)
    {
        if (!File.Exists(opt.FocusHintFile)) return false;
        var title = File.ReadAllText(opt.FocusHintFile).Trim();
        if (string.IsNullOrWhiteSpace(title)) return false;
        if (opt.AllowedWindowKeywords.Count == 0) return true;
        return opt.AllowedWindowKeywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static void CleanupDedup(int dedupWindowSec)
    {
        var now = DateTimeOffset.Now;
        var toRemove = RecentActions.Where(kv => (now - kv.Value).TotalSeconds > dedupWindowSec * 2).Select(kv => kv.Key).ToList();
        foreach (var k in toRemove) RecentActions.Remove(k);
    }

    private static AppOptions Load()
    {
        var p = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(p)) return new AppOptions();
        var n = JsonNode.Parse(File.ReadAllText(p));
        var o = new AppOptions();

        o.Input.ActionQueueFile = n?["Input"]?["ActionQueueFile"]?.GetValue<string>() ?? o.Input.ActionQueueFile;
        o.Input.PollMs = n?["Input"]?["PollMs"]?.GetValue<int>() ?? o.Input.PollMs;

        o.Driver.SimulationOnly = n?["Driver"]?["SimulationOnly"]?.GetValue<bool>() ?? o.Driver.SimulationOnly;
        o.Driver.MinIntervalMs = n?["Driver"]?["MinIntervalMs"]?.GetValue<int>() ?? o.Driver.MinIntervalMs;
        o.Driver.DedupWindowSec = n?["Driver"]?["DedupWindowSec"]?.GetValue<int>() ?? o.Driver.DedupWindowSec;
        o.Driver.EnabledFlagFile = n?["Driver"]?["EnabledFlagFile"]?.GetValue<string>() ?? o.Driver.EnabledFlagFile;
        o.Driver.StopFlagFile = n?["Driver"]?["StopFlagFile"]?.GetValue<string>() ?? o.Driver.StopFlagFile;
        o.Driver.FocusGuardEnabled = n?["Driver"]?["FocusGuardEnabled"]?.GetValue<bool>() ?? o.Driver.FocusGuardEnabled;
        o.Driver.FocusHintFile = n?["Driver"]?["FocusHintFile"]?.GetValue<string>() ?? o.Driver.FocusHintFile;

        o.Driver.AllowedActions = n?["Driver"]?["AllowedActions"]?.AsArray().Select(x => x?.GetValue<string>() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? o.Driver.AllowedActions;
        o.Driver.AllowedWindowKeywords = n?["Driver"]?["AllowedWindowKeywords"]?.AsArray().Select(x => x?.GetValue<string>() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? o.Driver.AllowedWindowKeywords;

        return o;
    }
}
