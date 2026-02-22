# AI_DECISION_INTERFACE_V1

目标：让“识别”和“决策”解耦。Orchestrator 负责感知与状态，DecisionAgent 负责动作决策。

## 模式
- `Decision.Mode = rule`：使用内置规则决策
- `Decision.Mode = agent`：优先读取 `agent_action.json`（超时自动回退 rule）

## 输入（给 Agent）
- 文件：`outbox/game_state.json`
- 关键字段：`seq/mode/hpPct/targetHpPct/frameDiff/stalledSeconds`

## 输出（Agent 给 Orchestrator）
- 最新动作文件：`outbox/agent_action.json`
- 决策历史文件：`outbox/agent_decision_history.jsonl`
- 字段：
```json
{
  "time": "2026-...",
  "source": "decision-agent-v1",
  "stateSeq": 120,
  "action": "cast_skill_1",
  "reason": "agent_mode_combat"
}
```

## 回退策略
- 若 `agent_action.json` 不存在或过期（`AgentActionMaxAgeSec`），自动回退 rule，保证不中断。
