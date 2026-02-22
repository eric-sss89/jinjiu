# MMO_AUTOFARM_ARCHITECTURE

## A. 数据流（从像素到动作）

`Frame -> ROI Extract -> Feature/OCR -> State Estimation -> Policy -> Action Queue -> Input Driver`

### 1) Frame
- 来源：Capture 模块
- 建议频率：2~5 FPS（初期）

### 2) ROI Extract
- 按分辨率适配提取关键区域
- ROI 示例：
  - `player_hp_mp`
  - `target_hp`
  - `skill_bar`
  - `system_prompt`

### 3) Feature / OCR
- 颜色比例（血条长度）
- 模板匹配（技能可用图标）
- OCR（错误提示、背包满、任务提示）

### 4) State Estimation
输出统一状态快照：
```json
{
  "inCombat": true,
  "hasTarget": true,
  "targetAlive": true,
  "hpPct": 72,
  "mpPct": 41,
  "danger": "low",
  "stuck": false
}
```

### 5) Policy
- 输入：状态快照
- 输出：动作意图（而非直接键鼠）

### 6) Action Queue
动作队列项示例：
```json
{
  "time": "2026-02-22T19:00:00+08:00",
  "action": "cast_skill",
  "key": "3",
  "reason": "target_alive_and_skill_ready",
  "cooldownMs": 800
}
```

### 7) Input Driver
- 负责最终键鼠注入
- 统一限速、随机扰动、窗口焦点校验

---

## B. 状态机建议

- `IDLE`：未战斗
- `SEARCH`：寻找目标
- `ENGAGE`：接近目标
- `COMBAT`：输出循环
- `LOOT`：拾取战利品
- `RECOVER`：回血回蓝/补给
- `ESCAPE`：危险脱离
- `ERROR`：异常恢复

### 状态切换关键规则
- 连续 3 帧确认再切换（抗抖）
- 每个状态最短驻留 500~1500ms
- `ESCAPE` 优先级最高，可抢占其他状态

---

## C. 动作策略建议（最小实用）

1. **保命优先**
   - `hpPct < 30` -> `use_potion` 或 `escape`
2. **输出循环**
   - 优先高收益技能，CD 空窗填普攻
3. **目标丢失回退**
   - `hasTarget=false` 持续 2s -> 切 `SEARCH`
4. **卡死检测**
   - 20s 无有效状态变化 -> `ERROR` + 重置流程

---

## D. 工程化建议

- 分离进程：`Capture`、`Orchestrator`、`InputDriver`
- IPC 建议：JSONL 文件（初期）-> Named Pipe（后期）
- 每个模块独立日志：`logs/{module}/yyyyMMdd.log`
- Replay：保留低频关键帧 + 决策日志，便于复盘

---

## E. 风险控制

- 仅在指定窗口类名/进程名下执行输入
- 全局紧急停止热键（如 F12）
- 长时间后台运行自动暂停
- 异常高频操作熔断（例如 10 秒内 > N 次重复同动作）

---

## F. 迭代优先级（建议）

P0：ROI + 血量识别 + 基础状态机 + 2~3个动作
P1：OCR + 技能可用识别 + 轮换策略
P2：路径/寻怪 + 死亡恢复 + 更强异常恢复
P3：模型化感知（轻量CV模型）与策略学习
