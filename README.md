# SerialSync

Visual Studio 风格的 Avalonia 串口调试工具。采用 [Crystal.Avalonia](https://github.com/0use-TE/Crystal.Avalonia) 作为应用宿主，[GOZA.Dock](https://github.com/0use-TE/GOZA.Dock) 提供 IDE 级可停靠布局。

![SerialSync](https://img.shields.io/badge/.NET-10-purple) ![Avalonia](https://img.shields.io/badge/Avalonia-12-blue)

## 界面

- **菜单栏**：系统原生标题栏 + 视图/帮助菜单（非自定义顶栏）
- **教程浮层**：首次启动显示，可通过菜单或状态栏「教程」开关
- **DockRegion 四象限**：面板可拖拽、跨区移动、双击 Tab 全屏
- **属性网格**：左侧串口连接配置（端口 / 波特率 / 校验 / DTR / RTS）
- **终端视图**：接收区等宽字体、文本/HEX 切换
- **输出窗口**：右侧日志；底部发送 / 快捷 / 序列面板各自独立

## 功能

| 区域 | 能力 |
|------|------|
| 串口 | 扫描端口、连接/断开、DTR/RTS |
| 接收 | 文本 / HEX 显示、保存到文件、字节计数、清空 |
| 发送 | 文本（可选 CR/LF/CRLF）、HEX、二进制文件、循环发送、**Ctrl+Enter** 快捷发送 |
| 快捷 | 独立输入区、保存常用指令、一键发送 |
| 序列 | 独立输入区、多步编排、可调步骤间隔、自动运行 |
| 日志 | 实时输出、搜索过滤、打开日志目录 |
| 布局 | 分割条尺寸与主题自动保存 |

## 项目结构

| 项目 | 说明 |
|------|------|
| `SerialSync` | 共享 UI 库（View / ViewModel / Services） |
| `SerialSync.Desktop` | Windows / Linux / macOS 桌面启动项 |
| `SerialSync.Browser` | WebAssembly 预览（GitHub Pages） |

## 运行

```bash
dotnet run --project SerialSync.Desktop
```

Native AOT 发布：

```bash
dotnet publish SerialSync.Desktop/SerialSync.Desktop.csproj -c Release -r win-x64 -p:PublishAot=true
```

## Web 界面预览 (GitHub Pages)

浏览器版仅用于展示 UI 样式，串口为模拟数据，**无法访问真实硬件**。

```bash
dotnet workload install wasm-tools
dotnet publish SerialSync.Browser/SerialSync.Browser.csproj -c Release
dotnet serve -d:SerialSync.Browser/bin/Release/net10.0-browser/publish/wwwroot
```

在线预览：<https://0use-te.github.io/SerialSync/>（推送 `master` 后由 GitHub Actions 自动部署到 `gh-pages` 分支）

> 首次启用：仓库 **Settings → Pages → Build and deployment → Branch** 选择 `gh-pages` / `/ (root)`。

## 技术栈

- .NET 10 · Avalonia 12 · Semi.Avalonia
- Crystal.Avalonia · GOZA.Dock
- CommunityToolkit.Mvvm · Serilog · System.IO.Ports

## 数据目录

```
%LocalAppData%\SerialSync\
├── layout.json           # 布局与主题
├── serial-settings.json  # 串口参数记忆
├── send-presets.json     # 快捷指令与序列
└── Logs\                 # 滚动日志文件
```

## License

MIT
