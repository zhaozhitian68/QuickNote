# QuickNote — 桌面快捷工具箱

## 项目概述

Windows 桌面应用，基于 C# WinForms（.NET Framework），单 exe 运行，无需安装。
集成便签、知识库、端口查询、Claude Code 配置管理、AI 对话五大功能模块。
所有数据统一存储在 exe 同目录下的 `data.json` 文件中，首次运行时自动创建。

## 技术栈

- 语言: C#
- 框架: .NET Framework, Windows Forms
- 数据存储: 本地 JSON 文件（手写 JSON 解析，无第三方依赖）
- 网络: HttpWebRequest（SSE 流式请求）
- 编译产物: `QuickNote.exe`

## 文件结构与类说明

```
QuickNote.cs        主窗口 + 截图 + 热键设置
AppData.cs          统一数据层（单例，读写 data.json）
KnowledgeDB.cs      知识库数据操作（CRUD + 搜索 + 标签）
KnowledgePanel.cs   知识库界面（搜索、标签筛选、分页、卡片列表）
KnowledgeDialogs.cs 知识库对话框（新增/编辑 + 便签转存）
CCProfileStore.cs   CC 配置数据操作 + 写入 ~/.claude/settings.json
CCConfigPanel.cs    CC 配置管理界面（多配置切换、额度查询、模型列表）
ChatPanel.cs        AI 对话界面（Claude API 流式调用）
PortPanel.cs        端口查询界面（netstat + 进程终止）
data.json           用户数据文件（便签、知识库、CC配置、热键设置）
```

## 核心类详解

### AppData（AppData.cs）
- 单例模式，全局唯一数据入口
- 管理四类数据：Notes（便签列表）、KnowledgeItems（知识条目）、CCProfiles（CC配置）、Settings（热键）
- `Load()` / `Save()` 手写 JSON 序列化/反序列化
- `Migrate()` 兼容旧版数据文件迁移（notes.txt、knowledge.json、cc_profiles.json、settings.txt）
- data.json 不存在时静默跳过，程序以默认值启动

### MainForm（QuickNote.cs）
- 程序入口，包含 `Main()` 方法
- TabControl 承载 5 个 Tab 页：快捷便签、知识库、端口查询、CC配置、AI对话
- 全局热键注册（Win32 API RegisterHotKey）：唤起窗口 + 截图
- 系统托盘图标，关闭窗口时隐藏到托盘
- 便签功能：多行文本块，支持复制、删除、转存到知识库
- 内嵌 ScreenshotOverlay 类：全屏截图选区，保存到桌面 + 复制到剪贴板
- 内嵌 HotkeySettingsDialog 类：热键设置对话框

### KnowledgeDB（KnowledgeDB.cs）
- 知识条目的 CRUD 操作，操作 AppData.Instance 中的 KnowledgeItems 列表
- `Search(keyword)` 模糊搜索内容和标签
- `GetAllTags()` 提取所有去重标签（支持中英文逗号、分号、空格分隔）
- KnowledgeItem 数据模型：Id、Content、Tags、CreatedAt、UpdatedAt

### KnowledgePanel（KnowledgePanel.cs）
- 知识库主界面：搜索框（300ms 防抖）+ 标签筛选栏 + 知识卡片列表 + 分页（每页10条）
- 双击卡片编辑，支持复制和删除
- `AddKnowledgeFromNote()` 供便签转存调用

### KnowledgeDialogs（KnowledgeDialogs.cs）
- TransferDialog：便签转存对话框，选择标签后转存
- KnowledgeEditDialog：新增/编辑知识条目对话框
- 两个对话框都有标签下拉选择面板（CheckBox 多选）

### CCProfileStore（CCProfileStore.cs）
- CC 配置的 CRUD 操作
- CCProfile 数据模型：Name、ApiKey、AuthToken、BaseUrl、Model、IncludeCoAuthoredBy、SkipDangerousMode、EffortLevel、QuotaQueryUrl
- `ApplyProfile()` 核心方法：将配置写入 `~/.claude/settings.json`，设置环境变量（ANTHROPIC_API_KEY、ANTHROPIC_AUTH_TOKEN、ANTHROPIC_BASE_URL）和顶层配置项

### CCConfigPanel（CCConfigPanel.cs）
- CC 配置管理界面：配置列表（RadioButton 选择）+ 详情表单 + 切换/保存/删除
- 后台自动获取模型列表（GET /v1/models）
- 额度查询：每分钟轮询 QuotaQueryUrl，解析 token_info 中的 remain_quota_display / today_added_quota 计算百分比

### ChatPanel（ChatPanel.cs）
- AI 对话界面，读取当前激活的 CC 配置发起请求
- 调用 Claude Messages API（/v1/messages），SSE 流式输出
- 支持 Bearer Token 和 x-api-key 两种认证方式
- 上下文管理：最多保留 20 条消息，支持清空上下文
- Enter 发送，Alt+Enter 换行

### PortPanel（PortPanel.cs）
- 输入端口号，调用 `netstat -ano` 查询占用进程
- 显示 PID、进程名、连接详情
- 支持强制终止进程（Process.Kill）

## 数据文件 data.json 结构

```json
{
  "notes": ["便签内容1", "便签内容2"],
  "knowledge": [
    {"id": 1, "content": "内容", "tags": "标签", "createdAt": "...", "updatedAt": "..."}
  ],
  "ccProfiles": {
    "active": "当前激活配置名",
    "profiles": [
      {"name": "...", "apiKey": "...", "authToken": "...", "baseUrl": "...", "model": "...", ...}
    ]
  },
  "settings": {
    "hotkey": "修饰键|虚拟键码",
    "screenshot": "修饰键|虚拟键码"
  }
}
```

注意：data.json 包含 API Key 等敏感信息，已加入 .gitignore，不要提交到仓库。
该文件不存在时程序正常启动（空白状态），用户数据会在使用过程中自动生成。

## 编码约定

- 无第三方 NuGet 依赖，所有 JSON 解析手写实现
- UI 控件全部代码创建（无 Designer 文件）
- 便签文本中的换行符存储为 `{{NEWLINE}}`
- 标签分隔符兼容：中英文逗号、分号、空格
- 热键格式：`修饰键|虚拟键码`（如 `2|123` 表示 Ctrl+F12）
