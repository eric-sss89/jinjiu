# Jinjiu 指令协议（发给助手）

## 文件
- 输出文件：`src/Jinjiu.Orchestrator/outbox/commands_to_assistant.jsonl`

## 字段
- `time`: ISO 时间
- `target`: 固定 `assistant`
- `type`: 指令类型（scene_changed / screen_too_dark / screen_stalled）
- `priority`: low / normal / high
- `message`: 给助手的人类可读消息
- `evidence`: 证据字段（阈值、亮度、帧差、持续时长等）

## 示例
```json
{
  "time": "2026-02-22T18:52:18.190+08:00",
  "target": "assistant",
  "type": "screen_stalled",
  "priority": "high",
  "message": "画面超过20秒几乎无变化，可能卡住，请检查。",
  "evidence": {
    "stalledSeconds": 21,
    "diffRatio": 0.003
  }
}
```
