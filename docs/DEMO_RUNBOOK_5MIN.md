# DEMO_RUNBOOK_5MIN

目标：5分钟内验证 MVP 端到端链路。

## 0) 前置
- Windows 10/11
- .NET 8 SDK
- 游戏窗口可见（窗口化/无边框更便于调试）

## 1) 一键启动
```powershell
./scripts/start-mvp.ps1
```

## 2) 校准 ROI（建议先做）
```powershell
cd src/Jinjiu.RoiCalibrator
dotnet run
```
检查 `preview/roi_preview.jpg`，若框位不准，调整：
- `src/Jinjiu.RoiCalibrator/appsettings.json`
- 并同步到 `src/Jinjiu.Orchestrator/appsettings.json`

## 3) 观察链路
- Capture: `src/Jinjiu.Capture/captures/*.jpg` 持续增长
- Orchestrator: `outbox/game_state.json` 更新，`action_queue.jsonl` 写入
- InputDriver: 控制台看到 `[execute]` / `[skip]`

## 4) 安全控制
- 暂停执行：删除 `src/Jinjiu.Orchestrator/outbox/driver.enabled`
- 紧急停止：创建 `src/Jinjiu.Orchestrator/outbox/driver.stop`

## 5) 通过标准（快速）
- 连续运行 5 分钟无崩溃
- 可观察到状态变化与动作输出
- 无明显动作刷屏（限频/去重生效）
