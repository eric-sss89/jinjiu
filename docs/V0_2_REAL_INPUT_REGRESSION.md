# v0.2 真实输入回归清单

目标：在开启真实输入前后，验证安全栅栏与基础动作链路行为一致、可控、可回滚。

## 0. 前置条件
- Windows 10/11
- `.NET 8 SDK`
- 采集、编排、输入驱动均可启动
- 已知可识别场景（可稳定触发 `tab_target` / `cast_skill_1`）

## 1. 默认安全态验证（必须通过）
1) `Driver.SimulationOnly=true`，`Driver.RealInputEnabled=false`
2) 启动全部进程
3) 观察 InputDriver 输出

期望：
- 只出现 `simulation=true` 审计记录
- 不发生真实键盘注入

## 2. 双重开关验证
### 2.1 缺失 `driver.enabled`
- 删除 `src/Jinjiu.Orchestrator/outbox/driver.enabled`
- 期望：InputDriver 输出 `[paused]`，无动作执行

### 2.2 仅开启真实输入配置，不放行 unsafe flag
- 设置 `SimulationOnly=false` 且 `RealInputEnabled=true`
- 不创建 `driver.unsafe.enabled`
- 期望：动作被拦截，提示 missing unsafe flag；无真实输入

### 2.3 双开关齐全
- 创建 `driver.enabled` + `driver.unsafe.enabled`
- 期望：支持映射的动作可产生真实输入，审计 `simulation=false`

## 3. 动作白名单与映射验证
1) 发送一个不在 `AllowedActions` 的动作
- 期望：`[skip] action not allowed`

2) 发送在白名单但不在 `ActionKeyMap` 的动作（真实输入模式）
- 期望：`[skip] real-input unsupported action`

3) 发送映射有效动作（如 `tab_target -> TAB`）
- 期望：执行成功并记录 key

## 4. 焦点防护验证（可选但建议）
1) 打开 `FocusGuardEnabled=true`
2) `UseSystemForegroundTitle=true`
3) 前台窗口切到非目标程序
- 期望：`[skip] focus guard blocked action`

4) 前台切回目标程序
- 期望：动作恢复执行

## 5. 限流/去重验证
- 连续注入同一动作与原因
- 期望：出现 `rate-limit` / `dedup` 跳过日志

## 6. 紧急停止验证
- 创建 `driver.stop`
- 期望：立即阻断动作执行
- 删除 `driver.stop` 后恢复

## 7. 回滚验证
- 将 `SimulationOnly=true` 或 `RealInputEnabled=false`
- 删除 `driver.unsafe.enabled`
- 期望：系统恢复到纯模拟执行

## 8. 验收准入标准
- 默认安全态通过
- 双开关逻辑通过
- 白名单/映射/焦点/限流/去重/急停均通过
- 审计日志可追溯（time/action/reason/simulation/key）
