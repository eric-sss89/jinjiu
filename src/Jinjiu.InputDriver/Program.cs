using System.Text.Json.Nodes;

internal sealed class InputOptions { public string ActionQueueFile { get; set; } = "../Jinjiu.Orchestrator/outbox/action_queue.jsonl"; public int PollMs { get; set; } = 300; }
internal sealed class DriverOptions { public bool SimulationOnly { get; set; } = true; }
internal sealed class AppOptions { public InputOptions Input { get; set; } = new(); public DriverOptions Driver { get; set; } = new(); }

internal static class Program
{
    private static long _offset;

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
                    Execute(node, opt.Driver.SimulationOnly);
                }

                _offset = fs.Position;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[error] {ex.Message}");
            }

            await Task.Delay(opt.Input.PollMs);
        }
    }

    private static void Execute(JsonObject action, bool simulation)
    {
        var act = action["action"]?.GetValue<string>() ?? "unknown";
        var reason = action["reason"]?.GetValue<string>() ?? "na";
        Console.WriteLine($"[execute] action={act} reason={reason} simulation={simulation}");
        // 后续这里替换为真实键鼠注入
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
        return o;
    }
}
