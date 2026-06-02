# README 使用说明

论坛发布包建议附带本文件。

## 这是什么
MinimalSnapTimer 是一个轻量、离线、无广告的 Windows 倒计时工具，适合：

- 坐 40 / 站 15 提醒
- 普通倒计时
- 纯时间浮窗显示

## 怎么运行
双击 `MinimalSnapTimer_v0.1.0-beta_win-x64.exe` 即可。

## 主要功能
- 坐 / 站工作流
- 普通倒计时和秒表
- 纯时间模式
- 点击穿透
- 托盘菜单
- 多显示器恢复
- 浅色 / 深色主题
- 简体中文 / English

## 常见问题
### exe 为什么这么大
因为当前发布的是 `.NET 8 + WPF + win-x64 + self-contained + single-file` 完整版，运行时和 WPF 依赖都打进了同一个 exe。

### 点击穿透后点不到窗口怎么办
通过托盘菜单关闭点击穿透。

### 窗口找不到了怎么办
打开设置，点击“重置窗口位置”。

### 日志在哪
`%LOCALAPPDATA%\MinimalSnapTimer\logs`
