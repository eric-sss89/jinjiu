# Release Notes - v0.2.0-next（阶段性）

> 状态：in progress（阶段里程碑，不是最终 GA 版本）

## 本阶段目标
将 InputDriver 从“模拟打印”推进到“可控真实输入骨架 + 可审计”，并保持默认安全关闭。

## 已完成
- 真实输入注入骨架（默认 OFF）
- 双重安全开关：`driver.enabled` + `driver.unsafe.enabled`
- 动作到按键的配置化映射：`ActionKeyMap`
- 输入审计日志：`input_audit.jsonl`
- 相关文档更新：
  - `docs/V0_2_NEXT.md`
  - `docs/SAFETY_GUARDS.md`
  - `docs/INPUT_AUDIT.md`
  - `docs/V0_2_REAL_INPUT_REGRESSION.md`

## 兼容性与风险
- 平台：Windows（依赖 Win32 + `keybd_event`）
- 默认配置不会发送真实按键
- 开启真实输入需显式配置 + flag 文件双重确认

## 升级建议（从 v0.1.0）
1. 合并 `src/Jinjiu.InputDriver/appsettings.json` 新字段：
   - `RealInputEnabled`
   - `UnsafeEnableFlagFile`
   - `ActionKeyMap`
   - `AuditLogFile`
2. 保持 `SimulationOnly=true` 完成一轮回归
3. 按 `docs/V0_2_REAL_INPUT_REGRESSION.md` 执行安全验收

## 下一阶段
- 按键按下时长与随机抖动参数化
- 审计聚合/回放工具
- 真实输入安全回归脚本化
