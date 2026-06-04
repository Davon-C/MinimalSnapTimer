# MinimalSnapTimer

简体中文 | [English](README.md)

MinimalSnapTimer 是一个面向 Windows 的轻量桌面倒计时器，支持坐/站提醒、纯时间模式、托盘运行、多显示器、主题切换和基础中英文本地化。

## 项目简介

这个项目的目标不是做一个复杂的任务管理器，而是做一个：

- 启动快
- 离线可用
- 无广告
- 可直接分发
- 适合桌面常驻的小型计时工具

当前版本基于 `.NET 8 + WPF`，主要面向 Windows 10 / 11。

## 功能特点

- 倒计时、暂停、继续、停止、重置
- 坐着工作 / 站立活动工作流
- 纯时间模式
- 点击穿透
- 托盘运行与托盘恢复入口
- 多显示器窗口保存与恢复
- 浅色 / 深色主题
- 简体中文 / English 基础切换
- 提醒弹窗、系统通知、托盘提醒
- 启动日志与异常保护

## 下载与运行

推荐从 GitHub Releases 下载：

- [Releases 页面](https://github.com/Davon-C/MinimalSnapTimer/releases)

运行方式：

1. 下载 `MinimalSnapTimer_v0.1.0-beta_win-x64.zip`
2. 解压压缩包
3. 双击 `MinimalSnapTimer_v0.1.0-beta_win-x64.exe`

当前只提供完整版单文件 exe，采用：

- `.NET 8`
- `WPF`
- `win-x64`
- `self-contained`
- `single-file`

因此 exe 体积相对较大，这是正常现象。对应好处是普通用户通常不需要额外安装 `.NET Desktop Runtime`。

## 使用说明

- 点击“坐着工作”可快速开始坐姿计时
- 点击“站立活动”可快速开始站立计时
- 可从主窗口切换到纯时间模式
- 开启点击穿透后，窗口本身不再响应鼠标，需要通过托盘菜单恢复
- 可在设置中切换主题和语言
- 可在设置中打开日志目录、配置目录，或重置窗口位置

## 内存与常驻资源说明

近期做过一轮发布版资源审计，结论是：

- 常见空闲场景下，`Private Memory` 大多稳定在约 `57–58 MB`
- 启动后 `Working Set` 可能暂时更高，但空闲后会回落
- 打开设置窗口会短时抬高内存，关闭后会部分回落，这更像 WPF 缓存与对象保留，不是明显泄漏
- 对于 `WPF + self-contained + 托盘 + 主题 + 多语言 + 通知` 这一类桌面小工具，这个量级是合理的

当前没有证据表明需要为这一级别内存占用从 0 重写项目。

## 构建项目

开发环境：

- Windows 10 / 11
- .NET 8 SDK
- WPF

常用命令：

```powershell
dotnet restore
dotnet build .\MinimalSnapTimer.sln -c Debug
dotnet test .\MinimalSnapTimer.sln -c Debug
```

发布命令：

```powershell
dotnet publish .\src\MinimalSnapTimer\MinimalSnapTimer.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false /p:PublishReadyToRun=false
```

## 发布说明

- 当前版本：`v0.1.0-beta`
- 当前只提供完整版 exe
- 不再提供 lite / framework-dependent 包
- 推荐直接下载 Release zip 使用

更多说明见：

- [发布说明](docs/发布说明_v0.1.0-beta.md)
- [变更记录](CHANGELOG.md)

## 已知限制

- 跟随系统主题目前不是完整的实时监听
- English 下少量边角文本可能仍未完全覆盖
- 系统通知是轻量实现，不是复杂交互式通知
- 当前没有安装器
- 当前没有自动更新
- 点击穿透开启后，窗口本身不会响应鼠标，需要通过托盘恢复

## 故障排查

常见问题：

- 双击无反应：查看 `startup.log`
- 点击穿透后点不到窗口：从托盘菜单关闭点击穿透
- 窗口不见了：从托盘显示主窗口，或重置窗口位置
- 通知不弹：检查应用内通知开关和系统通知设置
- 下载后被系统拦截：当前是未签名 beta 桌面工具，请自行判断是否信任

详见：

- [故障排查](docs/故障排查.md)

## 许可证

本项目使用 [MIT License](LICENSE)。
