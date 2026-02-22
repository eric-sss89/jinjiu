# STATE_SCHEMA（MVP）

`outbox/game_state.json` 当前字段说明：

- `stateVersion`: 状态结构版本（当前 `mvp-v1`）
- `seq`: 单调递增序号（便于 UI/日志跟踪）
- `time`: 状态生成时间
- `rawMode`: 原始判定模式
- `mode`: 抗抖后的稳定模式
- `modeChanged`: 本次是否发生稳定状态切换
- `modeSince`: 当前稳定模式起始时间
- `hpPct`: 玩家血条估计值（0~1）
- `targetHpPct`: 目标血条估计值（0~1）
- `frameDiff`: 帧差比例
- `stalledSeconds`: 低变化持续时长
- `lastAction`: 最近一次写入动作队列的动作对象（可能为 null）
