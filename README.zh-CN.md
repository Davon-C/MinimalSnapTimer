# MinimalSnapTimer

简体中文 | [English](README.md)

MinimalSnapTimer 是一个面向 Windows 的极简坐站提醒 / 倒计时小工具，支持纯时间模式、点击穿透、托盘恢复、多显示器、主题切换和基础多语言。

## 项目简介

MinimalSnapTimer 是一个 Windows 桌面小工具，主要用于：

- 坐着工作 / 站立活动提醒
- 普通倒计时与短时专注提醒
- 纯时间窗口展示

它的目标是简单、轻量、可贴边、可托盘运行，而不是复杂的任务管理或重型番茄钟系统。

## 功能特点

- 坐 / 站快捷计时
- 普通倒计时与秒表模式
- 纯时间模式
- 点击穿透
- 托盘运行与托盘恢复入口
- 多显示器支持
- 浅色 / 深色主题
- 简体中文 / English 基础支持
- 系统通知 / 托盘提醒
- 启动日志与安全回退

## 下载与运行

推荐从 GitHub Releases 下载：

- [Releases 页面](https://github.com/Davon-C/MinimalSnapTimer/releases)

运行方式：

1. 下载 `MinimalSnapTimer_v0.1.0-beta_win-x64.zip`
2. 解压压缩包
3. 双击 `MinimalSnapTimer_v0.1.0-beta_win-x64.exe`

当前发布的是 Windows x64 完整版，采用 self-contained single-file 方式打包。  
因此 exe 体积相对较大，这是 `.NET 8 + WPF + self-contained + single-file` 的正常结果。  
对应的好处是普通用户通常不需要额外安装 .NET Runtime。

## 使用说明

- 点击“坐着工作”可以快速开始坐姿计时
- 点击“站立活动”可以快速开始站立计时
- 可从主窗口切换到纯时间模式
- 开启点击穿透后，窗口本身不会响应鼠标，这是当前设计行为
- 点击穿透开启后，可通过托盘菜单关闭并恢复控制
- 可在设置中切换主题和语言
- 可在设置中打开日志目录、配置目录，或重置窗口位置

## 截图

Screenshots coming soon.

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
- 不提供 lite / framework-dependent 包
- 推荐下载 Release zip 使用

可参考：

- [发布说明](docs/发布说明_v0.1.0-beta.md)
- [变更记录](CHANGELOG.md)

## 已知限制

- 跟随系统主题目前不是完整的实时监听
- English 下少量边角文本可能仍未完全覆盖
- 系统通知是基础实现，不是复杂交互式通知
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

详细说明见：

- [故障排查](docs/故障排查.md)

## 许可证

本项目使用 MIT License，详见 [LICENSE](LICENSE)。
