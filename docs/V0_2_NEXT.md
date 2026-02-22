# v0.2 Next Steps

已完成 v0.2 第一块：InputDriver 真实输入注入骨架（默认关闭）。

## 当前行为
- 默认 `SimulationOnly=true` 或 `RealInputEnabled=false`，不会发真实按键
- 开启真实输入还需满足：
  1. `RealInputEnabled=true`
  2. `driver.enabled` 存在
  3. `driver.unsafe.enabled` 存在
  4. （可选）焦点防护通过

## 已支持动作映射（骨架）
- `cast_skill_1` -> 键 `1`
- `tab_target` -> `Tab`
- `use_potion` -> 键 `5`（示例，可改）
- `unstuck_move` -> 键 `W`

## 下一步
1. 将硬编码按键映射改为配置化
2. 加入按键按下时长/随机抖动参数
3. 加入动作审计日志（输入级）
4. 做真实输入下的安全回归测试清单
