# Jinjiu MVP one-click starter (Windows PowerShell)

Start-Process powershell -ArgumentList '-NoExit','-Command','cd src/Jinjiu.Capture; dotnet run'
Start-Sleep -Seconds 1
Start-Process powershell -ArgumentList '-NoExit','-Command','cd src/Jinjiu.Orchestrator; dotnet run'
Start-Sleep -Seconds 1
Start-Process powershell -ArgumentList '-NoExit','-Command','cd src/Jinjiu.InputDriver; dotnet run'

Write-Host "Jinjiu MVP started: Capture + Orchestrator + InputDriver"
