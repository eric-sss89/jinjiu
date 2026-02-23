# INPUT_AUDIT（v0.2）

本文档定义 `outbox/input_audit.jsonl` 的日志格式、使用方式与统计口径，供排障与回放工具使用。

## 文件位置
- 默认：`src/Jinjiu.Orchestrator/outbox/input_audit.jsonl`
- 由 `src/Jinjiu.InputDriver/appsettings.json` 的 `Driver.AuditLogFile` 配置

## 记录格式（JSONL）
每行一条 JSON：

```json
{
  "time": "2026-02-22T22:40:10.1234567+08:00",
  "action": "cast_skill_1",
  "reason": "target_alive",
  "simulation": true,
  "key": "SIM"
}
```

字段说明：
- `time`：ISO-8601 时间戳
- `action`：动作名（来自 action_queue）
- `reason`：动作原因（来自 action_queue）
- `simulation`：是否为模拟执行
- `key`：实际按键（真实输入）或 `SIM`（模拟）

## 与安全策略的关系
- 被以下条件拦截的动作不会写入该日志：
  - `driver.enabled` 缺失
  - `driver.stop` 存在
  - 不在 `AllowedActions`
  - 焦点防护不通过
  - 速率限制 / 去重命中
  - 真实输入模式下缺失 `driver.unsafe.enabled`

## 快速统计（PowerShell）
按动作计数：

```powershell
Get-Content src/Jinjiu.Orchestrator/outbox/input_audit.jsonl |
  ConvertFrom-Json |
  Group-Object action |
  Sort-Object Count -Descending |
  Select-Object Name,Count
```

按 simulation 分组：

```powershell
Get-Content src/Jinjiu.Orchestrator/outbox/input_audit.jsonl |
  ConvertFrom-Json |
  Group-Object simulation |
  Select-Object Name,Count
```

## 建议实践
- 保留最近一轮测试日志，避免长期无限增长
- 每次改动输入策略后，先在 `simulation=true` 观察审计分布
- 仅在受控场景下开启 `RealInputEnabled=true` + `driver.unsafe.enabled`
