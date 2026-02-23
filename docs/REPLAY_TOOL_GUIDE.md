# 回放工具使用手册（v0.2）

> 目标：基于历史日志进行“行为复盘”，帮助定位策略问题与误触发。

当前仓库已具备：
- `action_queue.jsonl`（编排器输出）
- `input_audit.jsonl`（输入执行审计）

本手册定义回放工具的最小使用方式与比对方法（即使工具尚在开发，也可按该规范实现）。

## 回放输入
1. `action_queue.jsonl`：动作意图（action/reason/time）
2. `input_audit.jsonl`：动作执行结果（simulation/key/time）

## 回放目标
- 比对“意图 -> 执行”是否一致
- 识别被拦截动作（白名单/焦点/限流/去重/安全开关）
- 输出时间轴，便于复盘

## 推荐输出结构
- `replay_summary.json`
  - totalIntents
  - totalExecuted
  - totalBlocked
  - byAction
  - blockedReasons
- `replay_timeline.txt`
  - 按时间列出关键事件（intent / executed / blocked）

## 手工回放流程（当前可执行）
1. 取同一轮测试的 `action_queue.jsonl` 和 `input_audit.jsonl`
2. 先看审计统计（参考 `docs/AUDIT_STATS_SCRIPT.md`）
3. 抽样核对动作：
   - action_queue 中动作是否在 audit 中出现
   - 若未出现，查看 InputDriver 控制台日志对应拦截原因
4. 整理复盘结论：
   - 哪些动作命中率低
   - 哪些规则拦截最常见
   - 是否有误判或配置不一致

## 工具开发建议（下一步）
- 新增 `scripts/replay-compare.ps1`：
  - 输入：queue + audit 文件
  - 输出：summary JSON + timeline TXT
- 支持 `--from/--to` 时间窗口筛选
- 支持按 `action` 过滤

## 验收标准
- 回放结果可重复（同输入同输出）
- 输出可读，5 分钟内能定位主要异常
- 至少覆盖：`tab_target` / `cast_skill_1` / `use_potion` 三类动作
