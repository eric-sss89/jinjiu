# Phase 2 - 基于屏幕信息生成指令（最小闭环）

## 目标
在不引入重模型的前提下，先完成：
- 读取采集到的屏幕帧
- 计算画面统计信息（亮度、帧差、静止时长）
- 根据规则生成“发给助手”的结构化指令
- 写入 `outbox/commands_to_assistant.jsonl`

## 输入
- `captures/*.jpg`（Phase 1 产物）

## 输出
- JSONL，每行一条指令：
```json
{
  "time": "2026-02-22T18:50:00+08:00",
  "target": "assistant",
  "type": "scene_changed",
  "priority": "normal",
  "message": "检测到明显场景切换，请关注当前界面变化",
  "evidence": { "diffRatio": 0.42 }
}
```

## 规则（当前）
1. `scene_changed`：帧差比例 > 阈值
2. `screen_too_dark`：平均亮度低于阈值
3. `screen_stalled`：画面长期变化很小（疑似卡住）

## 后续升级
- OCR 文本提取（优先接入 PaddleOCR/Tesseract）
- 关键区域识别（小地图/状态栏/弹窗）
- 指令去重、节流与合并
