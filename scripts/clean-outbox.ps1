# Clean runtime artifacts in outbox/captures (safe for MVP reset)

$paths = @(
  "src/Jinjiu.Orchestrator/outbox/game_state.json",
  "src/Jinjiu.Orchestrator/outbox/action_queue.jsonl",
  "src/Jinjiu.Orchestrator/outbox/agent_action.json",
  "src/Jinjiu.Orchestrator/outbox/agent_decision_history.jsonl",
  "src/Jinjiu.Orchestrator/outbox/driver.stop"
)

foreach ($p in $paths) {
  if (Test-Path $p) {
    Remove-Item $p -Force -ErrorAction SilentlyContinue
    Write-Host "Removed: $p"
  }
}

# keep driver.enabled by default for convenience
Write-Host "Outbox cleaned. driver.enabled preserved."
