# SerialSync

Visual Studio 风格的 Avalonia 串口调试工具。采用 [Crystal.Avalonia](https://github.com/0use-TE/Crystal.Avalonia) 作为应用宿主，[GOZA.Dock](https://github.com/0use-TE/GOZA.Dock) 提供 IDE 级可停靠布局。

![SerialSync](https://img.shields.io/badge/.NET-10-purple) ![Avalonia](https://img.shields.io/badge/Avalonia-12-blue)

## 界面

- **菜单栏**：系统原生标题栏 + 视图/帮助菜单（非自定义顶栏）
- **教程浮层**：首次启动显示，可通过菜单或状态栏「教程」开关
- **DockRegion 四象限**：面板可拖拽、跨区移动、双击 Tab 全屏
- **属性网格**：左侧串口连接配置（端口 / 波特率 / 校验 / DTR / RTS）
- **终端视图**：接收区等宽字体、文本/HEX 切换
- **输出窗口**：右侧日志、底部发送历史 + 编辑器

## 功能

| 区域 | 能力 |
|------|------|
| 串口 | 扫描端口、连接/断开、DTR/RTS |
| 接收 | 文本 / HEX 显示、字节计数、清空 |
| 发送 | 文本（可选 CR/LF/CRLF）、HEX、二进制文件、**Ctrl+Enter** 快捷发送 |
| 日志 | 实时输出、搜索过滤、打开日志目录 |
| 布局 | 分割条尺寸与主题自动保存 |

## 运行

```bash
dotnet run --project SerialSync
```

## 技术栈

- .NET 10 · Avalonia 12 · Semi.Avalonia
- Crystal.Avalonia · GOZA.Dock
- CommunityToolkit.Mvvm · Serilog · System.IO.Ports

## 数据目录

```
%LocalAppData%\SerialSync\
├── layout.json      # 布局与主题
└── Logs\            # 滚动日志文件
```

## License

MIT
