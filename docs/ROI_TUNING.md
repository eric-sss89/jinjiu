# ROI_TUNING（MVP）

先用可视化框把 ROI 调准，再谈识别精度。

## 工具
- `src/Jinjiu.RoiCalibrator`

## 用法
```powershell
cd src/Jinjiu.RoiCalibrator
dotnet run
```

会输出：
- `preview/roi_preview.jpg`

## 调参步骤
1. 打开 `src/Jinjiu.RoiCalibrator/appsettings.json`
2. 调整 `Roi.PlayerHp`、`Roi.TargetHp`（0~1 相对坐标）
3. 再次 `dotnet run`
4. 查看 `preview/roi_preview.jpg` 是否准确覆盖 UI
5. ROI 确认后，同步复制到：
   - `src/Jinjiu.Orchestrator/appsettings.json`

## 坐标说明
- `X,Y`：左上角（相对整屏比例）
- `W,H`：宽高（相对整屏比例）
