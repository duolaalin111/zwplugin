# ZWCAD 图库管理插件

中望CAD (ZWCAD) .NET 插件，提供图库管理功能，支持通过嵌入式浏览器 (CefSharp) 访问 Web 前端，实现图纸的入库、出库、编辑、删除等操作。

## 项目结构

```
ZrxDotNetCSProject5/
├── AppConfig.cs          # API 地址集中配置
├── ApiHelper.cs          # 项目管理 HTTP API 封装
├── CadHelper.cs          # ZWCAD 命令执行、图纸属性获取等共享工具
├── Commands.cs           # ZWCAD 命令定义（入口点）
├── PlugInApplication.cs  # ZWCAD 插件初始化，注册菜单栏
├── LoginStateManager.cs  # 全局登录状态（已废弃，登录移至 Web 端）
│
├── LibraryManageWeb.cs   # 图库管理主窗体（CefSharp 嵌入式浏览器）
├── LibraryManageWeb.Designer.cs
├── WebBridge (内嵌)      # C# ↔ JS 桥接，供前端调用
│
├── LibraryManageAntdUI.cs  # 图库管理（原生 AntdUI 版本，已废弃）
├── AntdDetailForm.cs       # AntdUI 图纸详情弹窗
├── DrawingDetailDialog.cs  # 图纸详情对话框（CefSharp 版本）
├── DrawingPropertiesEditorForm.cs  # 扩展属性编辑器
├── InputNameForm.cs        # 入库命名对话框
│
├── Form1try.cs             # 项目管理表单
├── ProjectDetailForm.cs    # 项目详情窗体
├── ProjectOverviewForm.cs  # 项目管理总览
├── ProjectManagement.cs    # 项目管理（旧版）
├── ProjectInfo.cs          # 项目信息数据模型
├── newmodels/
│   └── Projectmodel.cs     # 项目数据模型
│
├── loginAntdUI.cs          # 登录界面（已废弃，登录移至 Web 端）
│
├── Form1.cs                # AntdUI 组件演示 / 非标图纸标准化
├── GenerateLoopTags.cs     # 生成回路标签
├── Settings.cs             # 设置界面
│
└── AppConfig.cs            # 集中配置 API / 前端地址
```

## 功能特性

- **图库管理网页版**：通过 CefSharp 内嵌 Chromium 浏览器，加载 Vue/React 前端页面
- **入库**：在 CAD 中选择对象 → 导出 DWG + PNG → 上传到后端 API
- **出库**：从后端下载 DWG → 在 CAD 中选择插入点 → 插入图纸
- **炸开出库**：下载 DWG 后以炸开方式插入（直接插入实体）
- **图纸详情**：查看 / 编辑图纸扩展属性（JSON 格式）
- **图纸删除**：从后端删除图纸记录

## 技术栈

- .NET Framework 4.7
- WinForms
- [CefSharp](https://github.com/cefsharp/CefSharp) 90.6.70（嵌入式浏览器）
- [AntdUI](https://github.com/AntdUI/AntdUI) 2.3.2（原生 UI 控件）
- ZWCAD 2026 .NET API (`ZwManaged.dll`, `ZwDatabaseMgd.dll`)

## 配置

所有 API 地址集中在 `AppConfig.cs`：

```csharp
public const string ApiBaseUrl = "http://192.168.1.102:8080/";   // 后端 API
public const string FrontendUrl = "http://localhost:5173/";      // 前端开发服务器
```

## 部署

1. Visual Studio 打开 `ZrxDotNetCSProject5.sln`，选择 `x64 | Release` 编译
2. 将 `bin\x64\Release\` 目录下的所有文件复制到目标电脑
3. 目标电脑打开 ZWCAD，输入 `NETLOAD` 命令选择 `ZrxDotNetCSProject5.dll`
4. 点击菜单栏 **图库管理 → 图库管理** 进入

## ZWCAD 命令一览

| 命令 | 功能 |
|------|------|
| `LIBWEB` | 打开图库管理网页版 |
| `LOGIN` | 登录（已废弃） |
| `ZWCAD_入库` | CAD 入库命令（框选 → 导出文件） |
| `ZWCAD_出库1` | CAD 出库命令（选择插入点 → 插入块） |
| `ZWCAD_出库1_Explode` | CAD 炸开出库（插入后自动炸开） |
