using System.Text.Json.Nodes;

internal sealed class InputOptions { public string ActionQueueFile { get; set; } = "../Jinjiu.Orchestrator/outbox/action_queue.jsonl"; public int PollMs { get; set; } = 300; }
internal sealed class DriverOptions
{
    public bool SimulationOnly { get; set; } = true;
    public int MinIntervalMs { get; set; } = 800;
    public int DedupWindowSec { get; set; } = 5;
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

                    if (!TryExecute(node, opt.Driver))
                    {
                        continue;
                    }
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
        // 后续这里替换为真实键鼠注入

        _lastExecAt = now;
        RecentActions[key] = now;
        return true;
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
        return o;
    }
}
