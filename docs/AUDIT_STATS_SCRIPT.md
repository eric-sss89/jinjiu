# 审计统计脚本说明（v0.2）

本文档说明如何对 `input_audit.jsonl` 做聚合统计，并给出可直接运行的 PowerShell 脚本示例。

## 输入文件
- 默认：`src/Jinjiu.Orchestrator/outbox/input_audit.jsonl`

## 统计目标
- 总动作数
- 动作类型分布
- 模拟/真实输入占比
- 按分钟聚合（用于观察节奏/突发）

## 一次性命令（PowerShell）
```powershell
$logs = Get-Content src/Jinjiu.Orchestrator/outbox/input_audit.jsonl | ConvertFrom-Json

"=== total ==="
$logs.Count

"=== by action ==="
$logs | Group-Object action | Sort-Object Count -Descending | Select-Object Name,Count

"=== by simulation ==="
$logs | Group-Object simulation | Select-Object Name,Count

"=== per minute ==="
$logs |
  Group-Object { (Get-Date $_.time).ToString('yyyy-MM-dd HH:mm') } |
  Sort-Object Name |
  Select-Object Name,Count
```

## 脚本化建议
建议在仓库新增 `scripts/audit-stats.ps1`，支持参数：
- `-AuditFile`：日志文件路径
- `-TopN`：动作 Top N
- `-OutCsv`：可选导出 CSV

输出建议：
1. 控制台摘要（总数、按动作、按 simulation）
2. 可选 CSV（按分钟计数）

## 验收标准
- 能处理空文件/不存在文件（友好报错）
- 能处理单行坏数据（跳过并统计错误数）
- 输出中明确日志时间范围（min/max time）

## 与发布流程集成
- 每次回归后运行一次统计
- 将摘要粘贴到发布说明（或 PR 描述）
- 异常阈值（示例）：
  - 单分钟动作激增 > 平均值 3 倍时需要人工复核
