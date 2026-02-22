using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;

internal sealed class InputOptions { public string ActionQueueFile { get; set; } = "../Jinjiu.Orchestrator/outbox/action_queue.jsonl"; public int PollMs { get; set; } = 300; }
internal sealed class DriverOptions
{
    public bool SimulationOnly { get; set; } = true;
    public bool RealInputEnabled { get; set; } = false;
    public string UnsafeEnableFlagFile { get; set; } = "../Jinjiu.Orchestrator/outbox/driver.unsafe.enabled";
    public int MinIntervalMs { get; set; } = 800;
    public int DedupWindowSec { get; set; } = 5;
    public string EnabledFlagFile { get; set; } = "../Jinjiu.Orchestrator/outbox/driver.enabled";
    public string StopFlagFile { get; set; } = "../Jinjiu.Orchestrator/outbox/driver.stop";
    public List<string> AllowedActions { get; set; } = new();
    public bool FocusGuardEnabled { get; set; } = false;
    public bool UseSystemForegroundTitle { get; set; } = true;
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

        var simulation = opt.SimulationOnly || !opt.RealInputEnabled;
        Console.WriteLine($"[execute] action={act} reason={reason} simulation={simulation}");

        if (!simulation)
        {
            if (!File.Exists(opt.UnsafeEnableFlagFile))
            {
                Console.WriteLine($"[skip] real-input blocked, missing unsafe flag: {opt.UnsafeEnableFlagFile}");
                return false;
            }

            if (!TryPerformRealInput(act))
            {
                Console.WriteLine($"[skip] real-input unsupported action={act}");
                return false;
            }
        }

        _lastExecAt = now;
        RecentActions[key] = now;
        return true;
    }

    private static bool IsFocusAllowed(DriverOptions opt)
    {
        var title = opt.UseSystemForegroundTitle ? GetForegroundWindowTitle() : ReadHintTitle(opt.FocusHintFile);
        if (string.IsNullOrWhiteSpace(title)) return false;
        if (opt.AllowedWindowKeywords.Count == 0) return true;
        return opt.AllowedWindowKeywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadHintTitle(string path)
        => File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;

    private static string GetForegroundWindowTitle()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return string.Empty;

        var sb = new StringBuilder(512);
        _ = GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static void CleanupDedup(int dedupWindowSec)
    {
        var now = DateTimeOffset.Now;
        var toRemove = RecentActions.Where(kv => (now - kv.Value).TotalSeconds > dedupWindowSec * 2).Select(kv => kv.Key).ToList();
        foreach (var k in toRemove) RecentActions.Remove(k);
    }

    private static bool TryPerformRealInput(string action)
    {
        // MVP v0.2 skeleton: strict, minimal action set
        return action switch
        {
            "cast_skill_1" => TapVirtualKey(0x31), // '1'
            "tab_target" => TapVirtualKey(0x09),  // TAB
            "use_potion" => TapVirtualKey(0x35),  // '5' (example)
            "unstuck_move" => TapVirtualKey(0x57), // 'W'
            _ => false
        };
    }

    private static bool TapVirtualKey(byte vk)
    {
        try
        {
            keybd_event(vk, 0, 0, 0);
            Thread.Sleep(30);
            keybd_event(vk, 0, 0x0002, 0); // KEYEVENTF_KEYUP
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    private static AppOptions Load()
    {
        var p = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(p)) return new AppOptions();
        var n = JsonNode.Parse(File.ReadAllText(p));
        var o = new AppOptions();

        o.Input.ActionQueueFile = n?["Input"]?["ActionQueueFile"]?.GetValue<string>() ?? o.Input.ActionQueueFile;
        o.Input.PollMs = n?["Input"]?["PollMs"]?.GetValue<int>() ?? o.Input.PollMs;

        o.Driver.SimulationOnly = n?["Driver"]?["SimulationOnly"]?.GetValue<bool>() ?? o.Driver.SimulationOnly;
        o.Driver.RealInputEnabled = n?["Driver"]?["RealInputEnabled"]?.GetValue<bool>() ?? o.Driver.RealInputEnabled;
        o.Driver.UnsafeEnableFlagFile = n?["Driver"]?["UnsafeEnableFlagFile"]?.GetValue<string>() ?? o.Driver.UnsafeEnableFlagFile;
        o.Driver.MinIntervalMs = n?["Driver"]?["MinIntervalMs"]?.GetValue<int>() ?? o.Driver.MinIntervalMs;
        o.Driver.DedupWindowSec = n?["Driver"]?["DedupWindowSec"]?.GetValue<int>() ?? o.Driver.DedupWindowSec;
        o.Driver.EnabledFlagFile = n?["Driver"]?["EnabledFlagFile"]?.GetValue<string>() ?? o.Driver.EnabledFlagFile;
        o.Driver.StopFlagFile = n?["Driver"]?["StopFlagFile"]?.GetValue<string>() ?? o.Driver.StopFlagFile;
        o.Driver.FocusGuardEnabled = n?["Driver"]?["FocusGuardEnabled"]?.GetValue<bool>() ?? o.Driver.FocusGuardEnabled;
        o.Driver.UseSystemForegroundTitle = n?["Driver"]?["UseSystemForegroundTitle"]?.GetValue<bool>() ?? o.Driver.UseSystemForegroundTitle;
        o.Driver.FocusHintFile = n?["Driver"]?["FocusHintFile"]?.GetValue<string>() ?? o.Driver.FocusHintFile;

        o.Driver.AllowedActions = n?["Driver"]?["AllowedActions"]?.AsArray().Select(x => x?.GetValue<string>() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? o.Driver.AllowedActions;
        o.Driver.AllowedWindowKeywords = n?["Driver"]?["AllowedWindowKeywords"]?.AsArray().Select(x => x?.GetValue<string>() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? o.Driver.AllowedWindowKeywords;

        return o;
    }
}
