# MVP_ACCEPTANCE

## 验收目标
确认 MVP 流程跑通：

`Capture -> Orchestrator -> ActionQueue -> InputDriver`

## 验收步骤
1. 运行 `scripts/start-mvp.ps1`
2. 在游戏中做几个变化动作（移动镜头、切换目标、低血量场景）
3. 检查输出：
   - `src/Jinjiu.Capture/captures/*.jpg` 持续增长
   - `src/Jinjiu.Orchestrator/outbox/game_state.json` 持续更新时间
   - `src/Jinjiu.Orchestrator/outbox/action_queue.jsonl` 有动作写入
   - InputDriver 控制台出现 `[execute]` 或 `[skip]` 日志

## 通过标准
- 3 个模块都持续运行 >= 10 分钟无崩溃
- 状态切换不抖动（有多帧确认效果）
- 动作无明显刷屏（去重 + 限频生效）

## 常见问题
- 无截图：检查 Capture 是否有权限
- 无动作：先用 ROI 校准工具修正血条区域
- 动作太少/太多：调整 `Orchestrator` 和 `InputDriver` 的阈值
