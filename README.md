# Jinjiu

MVP 目标：先跑通 **屏幕采集 -> 状态估计 -> 动作队列 -> 执行器**。

## 目录
- `docs/`：规划与架构文档
- `src/Jinjiu.Capture/`：屏幕采集
- `src/Jinjiu.Orchestrator/`：状态估计 + 策略 + 动作队列
- `src/Jinjiu.InputDriver/`：动作消费执行（当前为模拟执行）
- `src/Jinjiu.RoiCalibrator/`：ROI 可视化校准工具
- `src/Jinjiu.DecisionAgent/`：AI 决策代理（读取状态，输出动作）

## 文档
- `docs/PROJECT_PLAN.md`
- `docs/MMO_AUTOFARM_ARCHITECTURE.md`
- `docs/MVP_FLOW.md`
- `docs/PHASE_1_CAPTURE.md`
- `docs/PHASE_2_RULE_ENGINE.md`
- `docs/COMMAND_PROTOCOL.md`
- `docs/ROI_TUNING.md`
- `docs/MVP_ACCEPTANCE.md`
- `docs/SAFETY_GUARDS.md`
- `docs/DEMO_RUNBOOK_5MIN.md`
- `docs/STATE_SCHEMA.md`
- `docs/AI_DECISION_INTERFACE_V1.md`
- `docs/RELEASE_v0.1.0.md`
- `docs/V0_2_NEXT.md`
- `CHANGELOG.md`

## 运行要求
- Windows 10/11
- .NET 8 SDK

## 启动顺序（3个终端）

### 1) 采集
```powershell
cd src/Jinjiu.Capture
dotnet run
```

### 2) 策略编排
```powershell
cd src/Jinjiu.Orchestrator
dotnet run
```

输出：
- `outbox/game_state.json`
- `outbox/action_queue.jsonl`

### 3) AI 决策代理（可选）
```powershell
cd src/Jinjiu.DecisionAgent
dotnet run
```
如果启用 Agent 决策，请在 `src/Jinjiu.Orchestrator/appsettings.json` 设置：
`Decision.Mode = "agent"`

### 4) 动作执行器（模拟）
```powershell
cd src/Jinjiu.InputDriver
dotnet run
```

会持续读取 `action_queue.jsonl` 并打印执行日志。

> 注意：InputDriver 需要 `src/Jinjiu.Orchestrator/outbox/driver.enabled` 存在才会执行动作。
> 若启用真实输入（`RealInputEnabled=true`），还需额外创建 `src/Jinjiu.Orchestrator/outbox/driver.unsafe.enabled`。

## 一键启动（Windows）
```powershell
./scripts/start-mvp.ps1
```

启用 Agent 决策链路：
```powershell
./scripts/start-mvp-agent.ps1
```

停止运行：
```powershell
./scripts/stop-mvp.ps1
```

清理运行产物：
```powershell
./scripts/clean-outbox.ps1
```

## 当前 MVP 规则（可跑通）
- 玩家血条低于阈值 -> `use_potion`
- 目标血条存在 -> `cast_skill_1`
- 无目标但画面变化明显 -> `tab_target`
- 长时间无变化 -> `unstuck_move`

> 说明：当前识别是最小版（红色ROI+帧差），优先跑通流程。后续再逐步提升精度。
