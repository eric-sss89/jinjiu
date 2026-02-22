# Stop Jinjiu MVP related processes (best effort)

$names = @("Jinjiu.Capture", "Jinjiu.Orchestrator", "Jinjiu.DecisionAgent", "Jinjiu.InputDriver")

Get-CimInstance Win32_Process |
  Where-Object { $_.Name -eq "dotnet.exe" -and ($names | ForEach-Object { $_ -and $_ }) -and ($_.CommandLine -match "Jinjiu\\.(Capture|Orchestrator|DecisionAgent|InputDriver)") } |
  ForEach-Object {
    Write-Host "Stopping PID=$($_.ProcessId) :: $($_.CommandLine)"
    Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
  }

Write-Host "Done. If any process remains, stop it manually from Task Manager."
