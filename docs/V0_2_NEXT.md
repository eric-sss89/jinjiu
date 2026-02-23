# v0.2 Next Steps

已完成 v0.2 第一块：InputDriver 真实输入注入骨架（默认关闭）。

## 当前行为
- 默认 `SimulationOnly=true` 或 `RealInputEnabled=false`，不会发真实按键
- 开启真实输入还需满足：
  1. `RealInputEnabled=true`
  2. `driver.enabled` 存在
  3. `driver.unsafe.enabled` 存在
  4. （可选）焦点防护通过

## 已支持动作映射（配置化）
- 通过 `ActionKeyMap` 配置动作到按键映射
- 默认示例：
  - `cast_skill_1 -> 1`
  - `tab_target -> TAB`
  - `use_potion -> 5`
  - `unstuck_move -> W`

## 输入审计日志
- `AuditLogFile` 默认输出：`outbox/input_audit.jsonl`
- 记录时间、动作、原因、是否模拟、按键

## 下一步
1. 加入按键按下时长/随机抖动参数
2. 加入动作审计聚合统计（按分钟/动作类型）与回放工具
3. 将真实输入回归检查脚本化并纳入发布前验收

## 新增文档（本轮）
- `docs/INPUT_AUDIT.md`：输入审计日志格式、统计口径、排障用法
- `docs/AUDIT_STATS_SCRIPT.md`：审计统计脚本说明
- `docs/REPLAY_TOOL_GUIDE.md`：回放工具使用手册
- `docs/V0_2_REAL_INPUT_REGRESSION.md`：真实输入回归清单
- `docs/RELEASE_v0.2.0-next.md`：v0.2 阶段发布说明
