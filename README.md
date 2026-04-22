# ZenTimeBox

![ZenTimeBox screenshot](./image.png)

ZenTimeBox is a lightweight Windows tray timer prototype focused on low-friction timeboxing.

It keeps the timer in the notification area instead of opening a large window, so you can track time with a quick glance while staying in flow.

## Current Prototype

- Windows tray timer built with `.NET 8` and `WinForms`
- Preset sessions: `15 / 25 / 45 / 60` minutes
- Large tray digits for minutes and seconds
- 1px border progress around the icon during the minute phase
- Theme-aware rendering for dark and light taskbars

## Run

```powershell
dotnet run --project .\src\ZenTimeBox.Demo\ZenTimeBox.Demo.csproj
```

## Notes

- This repository currently contains the prototype implementation.
- The focus is on tray readability, timing feedback, and minimal interaction.

---

# ZenTimeBox

![ZenTimeBox 截图](./image.png)

ZenTimeBox 是一个轻量的 Windows 托盘计时器原型，目标是做一个低打扰、低摩擦的时间盒工具。

它把计时信息放在系统托盘里，而不是打开一个大窗口，让你在保持专注的同时，靠余光快速确认时间状态。

## 当前原型能力

- 基于 `.NET 8` 和 `WinForms` 的 Windows 托盘计时器
- 预设时长：`15 / 25 / 45 / 60` 分钟
- 托盘中显示较大的分钟/秒钟数字
- 在分钟阶段使用 1px 外边框表示秒级进度
- 根据深色/浅色任务栏自动适配显示效果

## 运行方式

```powershell
dotnet run --project .\src\ZenTimeBox.Demo\ZenTimeBox.Demo.csproj
```

## 说明

- 当前仓库内容以原型验证为主。
- 重点在于托盘可读性、时间流逝反馈和极简交互。
