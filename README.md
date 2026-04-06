# QuickNote — Windows 桌面快捷工具箱

一个轻量级 Windows 桌面工具，集便签、知识库、端口查询、Claude Code 配置管理、AI 对话于一体。

单文件 exe，无需安装，开箱即用。

<!-- 建议在此处放一张截图或 GIF 动图 -->
<!-- ![screenshot](screenshots/demo.gif) -->

## 功能特性

- **快捷便签** — 全局热键唤起，随时记录灵感，支持多行文本
- **知识库** — 标签分类 + 模糊搜索 + 分页浏览，便签一键转存
- **端口查询** — 输入端口号即可查看占用进程，支持一键终止
- **Claude Code 配置管理** — 多套 API 配置一键切换，自动写入 `~/.claude/settings.json`，支持额度查询
- **AI 对话** — 内置 Claude 对话窗口，SSE 流式输出，支持上下文管理
- **截图工具** — 全局热键触发，框选区域截图，自动保存到桌面并复制到剪贴板
- **系统托盘** — 关闭窗口自动最小化到托盘，不占用任务栏

## 快速开始

### 直接使用

1. 前往 [Releases](../../releases) 页面下载最新的 `QuickNote.exe`
2. 双击运行，无需安装

### 从源码编译

```bash
git clone https://github.com/zhaozhitian68/QuickNote.git
cd QuickNote
csc /target:winexe /out:QuickNote.exe /win32icon:app.ico *.cs /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll
```

> 需要 .NET Framework 4.0+ 环境（Windows 7 及以上系统自带）

## 默认快捷键

| 功能 | 快捷键 | 说明 |
|------|--------|------|
| 唤起/隐藏窗口 | `Ctrl + F12` | 可在右键菜单中自定义 |
| 截图 | `Ctrl + F11` | 可在右键菜单中自定义 |

## 数据存储

所有数据保存在 exe 同目录下的 `data.json` 文件中：
- 首次运行自动创建
- 删除该文件即可恢复初始状态
- 该文件包含你的 API Key 等配置，请勿分享给他人

## 项目结构

```
QuickNote.cs          主窗口、截图、热键设置
AppData.cs            统一数据层（单例，读写 data.json）
KnowledgeDB.cs        知识库数据操作
KnowledgePanel.cs     知识库界面
KnowledgeDialogs.cs   知识库对话框（新增/编辑/转存）
CCProfileStore.cs     CC 配置数据操作
CCConfigPanel.cs      CC 配置管理界面
ChatPanel.cs          AI 对话界面
PortPanel.cs          端口查询界面
```

## 技术栈

- C# / .NET Framework / Windows Forms
- 零第三方依赖，所有 JSON 解析手写实现
- UI 全代码构建，无 Designer 文件

## License

[MIT](LICENSE)
