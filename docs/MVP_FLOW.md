# MVP_FLOW（先跑通）

目标：先跑通从屏幕到动作的完整流程，不追求高精度。

## 流程
1. `Jinjiu.Capture` 持续截图
2. `Jinjiu.Orchestrator` 读取最新截图，做最小识别并输出：
   - `outbox/game_state.json`（当前状态）
   - `outbox/action_queue.jsonl`（待执行动作）
3. `Jinjiu.InputDriver` 消费动作队列并执行（当前为模拟执行 + 日志，含去重与限频）

## MVP 识别能力（最简）
- ROI 红色占比估算：
  - 玩家血条填充比 `hpPct`
  - 目标血条填充比 `targetHpPct`
- 帧差：判断画面是否有明显变化

## MVP 状态稳定策略
- 使用 `rawMode` 与 `mode` 双层状态
- 仅当候选状态连续满足 `StateConfirmFrames` 且超过 `MinStateDwellMs` 才切换 `mode`

## MVP 策略
- `hpPct < 25%`：下发 `use_potion`
- `targetHpPct > 5%`：下发 `cast_skill_1`
- 无目标且画面有变化：下发 `tab_target`
- 长时间无变化：下发 `unstuck_move`

## 动作意图抑制（Orchestrator）
- 增加 `ActionDedupWindowSec`，同类动作意图在窗口期内只写一次队列
- 与 InputDriver 的去重/限频形成双重保险

## 说明
这是“能跑通”的版本。后续再逐步升级识别精度（OCR/模板匹配/多ROI）。
