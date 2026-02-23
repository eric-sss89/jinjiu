# Windows 上的 Jinjiu MVP 测试手册（一步一步）

> 目标：让你在 Windows 设备上从 0 到 1 跑通 MVP，并且每一步都知道“预期是什么、看到什么算成功、异常怎么判断”。

---

## 1. 当前 MVP 能实现哪些功能

当前仓库（v0.1 + v0.2-next）可以实现：

1) **屏幕采集（Capture）**
- 周期采集屏幕并输出感知输入

2) **状态估计 + 动作决策（Orchestrator）**
- 依据简化规则（血量/目标/画面变化）生成动作队列
- 输出文件：`outbox/game_state.json`、`outbox/action_queue.jsonl`

3) **动作执行器（InputDriver）**
- 默认模拟执行（打印日志，不发真实按键）
- 支持安全栅栏：`driver.enabled`、`driver.stop`、白名单、去重、限流、焦点防护
- v0.2-next 已支持：真实输入骨架（默认关闭）、动作到按键映射配置、输入审计日志 `input_audit.jsonl`

4) **可选 AI 决策代理（DecisionAgent）**
- 可切换为 agent 决策模式

---

## 2. 测试环境准备

## 2.1 系统与软件
- Windows 10/11
- .NET 8 SDK（`dotnet --version` 能输出版本）
- PowerShell 5+ 或 PowerShell 7

## 2.2 获取代码
```powershell
git clone https://github.com/eric-sss89/jinjiu.git
cd jinjiu
```

## 2.3 目录确认
确保存在以下关键目录：
- `src/Jinjiu.Capture`
- `src/Jinjiu.Orchestrator`
- `src/Jinjiu.InputDriver`
- `scripts`

---

## 3. 测试总览（建议顺序）

按这个顺序做，风险最小、定位最清晰：

1. 仅启动 Capture，确认采集进程正常
2. 启动 Orchestrator，确认产生状态与动作文件
3. 启动 InputDriver（模拟模式），确认动作被消费
4. 验证安全栅栏（enable/stop/白名单）
5. 验证审计日志（`input_audit.jsonl`）
6. （可选）验证真实输入双开关

---

## 4. 步骤实操（详细）

## Step 0：清理旧产物（强烈建议）

```powershell
./scripts/clean-outbox.ps1
```

**预期**
- 清理旧的 outbox 文件，避免“旧日志干扰新测试”

**你能观察到什么**
- 脚本输出清理信息；若部分文件不存在也属正常

---

## Step 1：启动采集模块（Capture）

打开终端 A：
```powershell
cd src/Jinjiu.Capture
dotnet run
```

**预期**
- 进程持续运行，无崩溃
- 周期打印采集相关日志

**你能观察到什么**
- 控制台有连续输出（帧采样/状态）
- 若立刻退出或报依赖错误，说明环境未就绪

---

## Step 2：启动编排模块（Orchestrator）

打开终端 B：
```powershell
cd src/Jinjiu.Orchestrator
dotnet run
```

**预期**
- 进程持续运行
- 生成输出文件：
  - `src/Jinjiu.Orchestrator/outbox/game_state.json`
  - `src/Jinjiu.Orchestrator/outbox/action_queue.jsonl`

**你能观察到什么**
- 控制台持续输出状态评估与动作意图
- `action_queue.jsonl` 行数增长（可用 `Get-Content -Tail 20 -Wait` 观察）

快速观察命令（终端 C）：
```powershell
Get-Content src/Jinjiu.Orchestrator/outbox/action_queue.jsonl -Tail 20 -Wait
```

---

## Step 3：启动 InputDriver（默认模拟执行）

打开终端 D：
```powershell
cd src/Jinjiu.InputDriver
dotnet run
```

> 注意：InputDriver 需要 `driver.enabled` 存在才执行动作。

创建 enable 文件（终端 E，仓库根目录）：
```powershell
New-Item -ItemType File -Force src/Jinjiu.Orchestrator/outbox/driver.enabled
```

**预期**
- InputDriver 从 `[paused]` 进入执行状态
- 看到类似：`[execute] action=... simulation=True`
- 写入：`src/Jinjiu.Orchestrator/outbox/input_audit.jsonl`

**你能观察到什么**
- 控制台动作日志持续出现
- 审计文件逐行增长

观察审计：
```powershell
Get-Content src/Jinjiu.Orchestrator/outbox/input_audit.jsonl -Tail 20 -Wait
```

---

## Step 4：验证安全栅栏（必须做）

## 4.1 Enable 开关验证
删除 enable 文件：
```powershell
Remove-Item src/Jinjiu.Orchestrator/outbox/driver.enabled -ErrorAction SilentlyContinue
```

**预期**
- InputDriver 输出 `[paused] driver.enabled missing`
- 不再执行动作

## 4.2 紧急停止验证
恢复 enable 后创建 stop 文件：
```powershell
New-Item -ItemType File -Force src/Jinjiu.Orchestrator/outbox/driver.enabled
New-Item -ItemType File -Force src/Jinjiu.Orchestrator/outbox/driver.stop
```

**预期**
- InputDriver 输出 stop 相关日志
- 动作被阻断

删除 stop 恢复：
```powershell
Remove-Item src/Jinjiu.Orchestrator/outbox/driver.stop -ErrorAction SilentlyContinue
```

## 4.3 白名单验证（可选）
- 在 `src/Jinjiu.InputDriver/appsettings.json` 修改 `AllowedActions`
- 去掉某个常见动作（如 `tab_target`），保存后重启 InputDriver

**预期**
- 该动作出现 `[skip] action not allowed`

---

## Step 5：验证审计日志是否可用

统计总数与动作分布：
```powershell
$logs = Get-Content src/Jinjiu.Orchestrator/outbox/input_audit.jsonl | ConvertFrom-Json
$logs.Count
$logs | Group-Object action | Sort-Object Count -Descending | Select-Object Name,Count
$logs | Group-Object simulation | Select-Object Name,Count
```

**预期**
- 有稳定数据量（非 0）
- `simulation` 在默认配置下应主要是 `True`

**你能观察到什么**
- 哪些动作最频繁
- 输入节奏是否异常（可进一步按分钟聚合）

---

## Step 6（可选）：真实输入双开关验证（谨慎）

> 警告：此步骤可能发送真实按键，仅在受控环境测试。

编辑 `src/Jinjiu.InputDriver/appsettings.json`：
- `SimulationOnly=false`
- `RealInputEnabled=true`

### 6.1 未放行 unsafe flag
不创建 `driver.unsafe.enabled`，仅保留 `driver.enabled`。

**预期**
- 真实输入被拦截，出现 missing unsafe flag 提示
- 不应有真实按键注入

### 6.2 放行双开关
创建 unsafe flag：
```powershell
New-Item -ItemType File -Force src/Jinjiu.Orchestrator/outbox/driver.unsafe.enabled
```

**预期**
- 支持映射的动作可以真实注入
- `input_audit.jsonl` 中 `simulation` 出现 `False`
- `key` 字段显示实际按键（如 TAB/1/W）

### 6.3 回滚到安全态
```powershell
Remove-Item src/Jinjiu.Orchestrator/outbox/driver.unsafe.enabled -ErrorAction SilentlyContinue
```
并把配置改回：
- `SimulationOnly=true`
- `RealInputEnabled=false`

**预期**
- 恢复纯模拟执行

---

## 5. 一键启动（可选快测）

仓库根目录：
```powershell
./scripts/start-mvp.ps1
```

如果要 agent 模式：
```powershell
./scripts/start-mvp-agent.ps1
```

停止：
```powershell
./scripts/stop-mvp.ps1
```

**建议**：第一次测试先按“4个终端手动启动”走，定位更清楚；跑通后再用一键脚本。

---

## 6. 测试通过标准（你可直接打勾）

- [ ] Capture 连续运行，无崩溃
- [ ] Orchestrator 持续产出 `game_state.json` 与 `action_queue.jsonl`
- [ ] InputDriver 在有 `driver.enabled` 时能消费动作
- [ ] 删除 `driver.enabled` 后进入 paused
- [ ] 创建 `driver.stop` 后立即阻断动作
- [ ] `input_audit.jsonl` 持续写入，字段完整
- [ ] 默认配置下 `simulation=true`
- [ ] （可选）真实输入双开关行为符合预期

---

## 7. 常见问题排查

1) `dotnet run` 失败
- 检查 .NET 8 SDK：`dotnet --list-sdks`

2) 没有动作输出
- 检查采集是否正常
- 检查 Orchestrator 是否在运行
- 检查 outbox 文件是否增长

3) InputDriver 一直 paused
- 检查 `driver.enabled` 是否创建在正确路径

4) 没有审计日志
- 检查 `AuditLogFile` 路径
- 检查是否实际上没有动作执行（都被拦截）

5) 真实输入不生效
- 确认 `SimulationOnly=false` + `RealInputEnabled=true`
- 确认 `driver.unsafe.enabled` 存在
- 确认动作在 `ActionKeyMap` 有映射

---

## 8. 建议你给我的反馈（测试后）

请按下面模板发我，我能最快定位问题：

```text
[环境]
Windows版本:
.NET版本:

[步骤结果]
Step1:
Step2:
Step3:
Step4:
Step5:
Step6(可选):

[关键日志片段]
(贴 20~50 行)

[你观察到的异常]
```

我会基于你的实测结果，给出下一轮最小修复清单（含风险和回滚）。
