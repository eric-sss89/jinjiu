# SAFETY_GUARDS（MVP）

为避免误操作，InputDriver 增加了三层防护：

## 1) 启用开关
- `driver.enabled` 文件存在才执行动作
- 删除该文件即可暂停执行

## 2) 紧急停止
- 检测到 `driver.stop` 文件后立即阻断动作
- 用于临时紧急刹车

## 3) 动作白名单
- 仅允许 `AllowedActions` 列表内动作
- 未登记动作一律跳过

## 4) 焦点防护
- `FocusGuardEnabled=true` 时启用焦点校验
- 默认 `UseSystemForegroundTitle=true`：通过 Windows API 获取真实前台窗口标题
- 仅当标题包含 `AllowedWindowKeywords` 执行动作
- 兼容回退：`UseSystemForegroundTitle=false` 时读取 `focus_window.txt`
