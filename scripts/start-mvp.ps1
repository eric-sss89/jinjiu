# Jinjiu MVP one-click starter (Windows PowerShell)

New-Item -ItemType Directory -Path src/Jinjiu.Orchestrator/outbox -Force | Out-Null
New-Item -ItemType File -Path src/Jinjiu.Orchestrator/outbox/driver.enabled -Force | Out-Null

Start-Process powershell -ArgumentList '-NoExit','-Command','cd src/Jinjiu.Capture; dotnet run'
Start-Sleep -Seconds 1
Start-Process powershell -ArgumentList '-NoExit','-Command','cd src/Jinjiu.Orchestrator; dotnet run'
Start-Sleep -Seconds 1
Start-Process powershell -ArgumentList '-NoExit','-Command','cd src/Jinjiu.InputDriver; dotnet run'

Write-Host "Jinjiu MVP started: Capture + Orchestrator + InputDriver"
Write-Host "Safety: remove outbox/driver.enabled to pause, create outbox/driver.stop to emergency stop"
