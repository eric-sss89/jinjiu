using System.Text.Json.Nodes;

internal sealed class InputOptions { public string StateFile { get; set; } = "../Jinjiu.Orchestrator/outbox/game_state.json"; public int PollMs { get; set; } = 300; }
internal sealed class OutputOptions { public string ActionFile { get; set; } = "../Jinjiu.Orchestrator/outbox/agent_action.json"; public string HistoryFile { get; set; } = "../Jinjiu.Orchestrator/outbox/agent_decision_history.jsonl"; }
internal sealed class PolicyOptions { public List<string> AllowedActions { get; set; } = new(); public int CooldownMs { get; set; } = 1000; }
internal sealed class AppOptions { public InputOptions Input { get; set; } = new(); public OutputOptions Output { get; set; } = new(); public PolicyOptions Policy { get; set; } = new(); }

internal static class Program
{
    private static long _lastSeq = -1;
    private static DateTimeOffset _lastDecisionAt = DateTimeOffset.MinValue;

    private static async Task Main()
    {
        var opt = Load();
        Directory.CreateDirectory(Path.GetDirectoryName(opt.Output.ActionFile) ?? ".");
        Directory.CreateDirectory(Path.GetDirectoryName(opt.Output.HistoryFile) ?? ".");
        Console.WriteLine("[Jinjiu.DecisionAgent] started");

        while (true)
        {
            try
            {
                if (!File.Exists(opt.Input.StateFile))
                {
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }

                var state = JsonNode.Parse(File.ReadAllText(opt.Input.StateFile))?.AsObject();
                if (state is null)
                {
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }

                var seq = state["seq"]?.GetValue<long>() ?? -1;
                if (seq <= _lastSeq)
                {
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }
                _lastSeq = seq;

                if ((DateTimeOffset.Now - _lastDecisionAt).TotalMilliseconds < opt.Policy.CooldownMs)
                {
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }

                var mode = state["mode"]?.GetValue<string>() ?? "IDLE";
                var action = Decide(mode);
                if (action is null || (opt.Policy.AllowedActions.Count > 0 && !opt.Policy.AllowedActions.Contains(action)))
                {
                    await Task.Delay(opt.Input.PollMs);
                    continue;
                }

                var output = new JsonObject
                {
                    ["time"] = DateTimeOffset.Now.ToString("O"),
                    ["source"] = "decision-agent-v1",
                    ["stateSeq"] = seq,
                    ["action"] = action,
                    ["reason"] = $"agent_mode_{mode.ToLowerInvariant()}"
                };

                File.WriteAllText(opt.Output.ActionFile, output.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                File.AppendAllText(opt.Output.HistoryFile, output.ToJsonString() + Environment.NewLine);
                _lastDecisionAt = DateTimeOffset.Now;
                Console.WriteLine($"[decision] {output.ToJsonString()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[error] {ex.Message}");
            }

            await Task.Delay(opt.Input.PollMs);
        }
    }

    private static string? Decide(string mode) => mode switch
    {
        "RECOVER" => "use_potion",
        "COMBAT" => "cast_skill_1",
        "SEARCH" => "tab_target",
        "ERROR" => "unstuck_move",
        _ => null
    };

    private static AppOptions Load()
    {
        var p = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(p)) return new AppOptions();
        var n = JsonNode.Parse(File.ReadAllText(p));
        var o = new AppOptions();
        o.Input.StateFile = n?["Input"]?["StateFile"]?.GetValue<string>() ?? o.Input.StateFile;
        o.Input.PollMs = n?["Input"]?["PollMs"]?.GetValue<int>() ?? o.Input.PollMs;
        o.Output.ActionFile = n?["Output"]?["ActionFile"]?.GetValue<string>() ?? o.Output.ActionFile;
        o.Output.HistoryFile = n?["Output"]?["HistoryFile"]?.GetValue<string>() ?? o.Output.HistoryFile;
        o.Policy.CooldownMs = n?["Policy"]?["CooldownMs"]?.GetValue<int>() ?? o.Policy.CooldownMs;
        o.Policy.AllowedActions = n?["Policy"]?["AllowedActions"]?.AsArray().Select(x => x?.GetValue<string>() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? o.Policy.AllowedActions;
        return o;
    }
}
