# Phase 1 - 屏幕采集模块设计

## 技术选型
- 语言：C#
- 运行时：.NET 8（Windows）
- 采集方式：`System.Drawing` + `Graphics.CopyFromScreen`

## 设计说明
- 使用定时循环按固定间隔截取主屏
- 每一帧保存为 JPEG
- 文件命名：`frame_yyyyMMdd_HHmmss_fff.jpg`
- 输出 JSON 行日志，便于后续管道读取

## 配置项
- `IntervalMs`：采集间隔（默认 1000ms）
- `OutputDir`：输出目录（默认 `captures`）
- `JpegQuality`：JPEG 质量（默认 80）
- `MaxFrames`：最大采集帧数（默认 0，表示无限）

## 后续扩展预留
- 多屏采集
- ROI 区域采样
- 帧差检测（减少冗余）
- 直接将帧推送到分析模块（内存队列）
