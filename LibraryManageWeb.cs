using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using AntdUI;
using System.Linq; // 必须包含这一行
using ZwSoft.ZwCAD.ApplicationServices;
using System.Threading;
// 关键：使用别名解决 Application 冲突
using CadApp = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace ZrxDotNetCSProject5
{
    public partial class LibraryManageWeb : Form
    {
        private ChromiumWebBrowser browser;
        private HttpClient httpClient;
        private string apiBaseUrl = "http://192.168.1.110:8080/";
        private string frontendUrl = "http://localhost:5173/";
        // 添加这个属性，让 WebBridge 可以访问 httpClient
        public HttpClient HttpClient => httpClient;

        public LibraryManageWeb()
        {
            InitializeComponent();
            InitHttpClient();
            InitBrowser();
        }

        private void InitHttpClient()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(apiBaseUrl);
        }

        private void InitBrowser()
        {
            CefSharpSettings.LegacyJavascriptBindingEnabled = true;

            if (!Cef.IsInitialized)
            {
                var settings = new CefSettings();

                settings.CachePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "CefCache"
                );

                settings.CefCommandLineArgs.Add("disable-web-security", "1");
                settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");

                CefSharpSettings.ConcurrentTaskExecution = true;

                Cef.Initialize(settings);
            }

            browser = new ChromiumWebBrowser(frontendUrl)
            {
                Dock = DockStyle.Fill
            };

            // ==================== 关键：注册 C# Bridge ====================
            browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;

            browser.JavascriptObjectRepository.Register(
                "bound",
                new WebBridge(this),
                isAsync: true,
                options: BindingOptions.DefaultBinder
            );

            // ============================================================

            this.Controls.Add(browser);

            browser.IsBrowserInitializedChanged += (s, e) =>
            {
                if (browser.IsBrowserInitialized)
                {
                    this.Invoke(new Action(() =>
                    {
                        browser.ShowDevTools();

                        //MessageBox.Show("CEF Bridge 注册成功！");
                    }));
                }
            };

            browser.LoadError += (s, e) =>
            {
                MessageBox.Show($"加载前端失败：{e.ErrorText}", "错误");
            };
        }


        public async void SaveDrawingDetail(DrawingDetail detail)
        {
            try
            {
                var requestBody = new { id = detail.Id, properties = detail.DescProp };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/drawings/edit", content);
                if (response.IsSuccessStatusCode)
                {
                    browser.ExecuteScriptAsync($"if(window.vm) vm.loadDetail({detail.Id})");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
    // ====================== WebBridge ======================
    public class WebBridge
    {
        private readonly LibraryManageWeb _parent;
        public WebBridge(LibraryManageWeb parent) { _parent = parent; }
        private readonly HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://192.168.1.110:8080/")
        };
        // ... 你小伙伴原来的所有方法保持不变 ...
        public string GetProductGroups()
        { /* 原代码 */
            try { var response = _parent.HttpClient.GetAsync("api/model-groups").Result; return response.Content.ReadAsStringAsync().Result; }
            catch (Exception ex) { return $"{{\"code\":500, \"message\":\"{ex.Message}\"}}"; }
        }

        public string GetTreeData(int productId)
        { /* 原代码 */
            try { var response = _parent.HttpClient.GetAsync($"api/product-schemes?productId={productId}").Result; return response.Content.ReadAsStringAsync().Result; }
            catch (Exception ex) { return $"{{\"code\":500, \"message\":\"{ex.Message}\"}}"; }
        }

        public string GetDrawings(long cabinetId)
        { /* 原代码 */
            try { var response = _parent.HttpClient.GetAsync($"api/drawings/simple?cabinetId={cabinetId}").Result; return response.Content.ReadAsStringAsync().Result; }
            catch (Exception ex) { return $"{{\"code\":500, \"message\":\"{ex.Message}\"}}"; }
        }

        public string FilterDrawings(long cabinetId, string valScope)
        { /* 原代码 */
            try
            {
                var requestBody = new { cabinetId, valScope };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = _parent.HttpClient.PostAsync("api/drawings/filter", content).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex) { return $"{{\"code\":500, \"message\":\"{ex.Message}\"}}"; }
        }

        public string GetDrawingDetail(long drawingId)
        { /* 原代码 */
            try { var response = _parent.HttpClient.GetAsync($"api/drawings/detail?id={drawingId}").Result; return response.Content.ReadAsStringAsync().Result; }
            catch (Exception ex) { return $"{{\"code\":500, \"message\":\"{ex.Message}\"}}"; }
        }

        public void EditDrawing(string detailJson)
        { /* 原代码 */
            _parent.Invoke(new Action(() => {
                var detail = JsonSerializer.Deserialize<DrawingDetail>(detailJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var editForm = new DrawingPropertiesEditorForm(detail);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    _parent.SaveDrawingDetail(editForm.UpdatedDrawingDetail);
                }
            }));
        }

        public string DeleteDrawing(long id)
        { /* 原代码 */
            try { var response = _parent.HttpClient.DeleteAsync($"api/drawings?id={id}").Result; return response.Content.ReadAsStringAsync().Result; }
            catch (Exception ex) { return $"{{\"code\":500, \"message\":\"{ex.Message}\"}}"; }
        }
        // ====================== 新增出入库方法 ======================

        // ==================== 新增：入库和出库方法 ====================
        /*public string StartImport(long typeId)
        {
            try
            {
                var doc = CadApp.DocumentManager.MdiActiveDocument;

                if (doc == null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "未找到CAD文档"
                    });
                }

                // 测试命令
                doc.SendStringToExecute("ZWCAD_入库 ", true, false, false);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"入库命令发送成功，typeId={typeId}"
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = ex.ToString()
                });
            }
        }
        */
        public async Task<string> StartImport(long typeId)
        {
            try
            {
                var doc = CadApp.DocumentManager.MdiActiveDocument;

                if (doc == null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "未找到CAD文档"
                    });
                }

                // 等待CAD命令执行完成
                bool isSuccess = await SendCommandAndWaitAsync(
                    doc,
                    "ZWCAD_入库 ",
                    "ZWCAD_入库"
                );

                if (!isSuccess)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "入库命令失败"
                    });
                }

                // 上传
                bool uploadResult =
                    await UploadDrawingsToBackend(typeId);

                return JsonSerializer.Serialize(new
                {
                    success = uploadResult,
                    message = uploadResult
                        ? "上传成功"
                        : "上传失败"
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = ex.ToString()
                });
            }
        }
        public async Task<string> StartExport(long drawingId)
        {
            try
            {
                // ==================== 下载DWG ====================

                bool downloadSuccess =
                    await PrepareExportFile(drawingId);

                if (!downloadSuccess)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "下载图纸失败"
                    });
                }

                var doc = CadApp.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return JsonSerializer.Serialize(new { success = false, message = "未找到CAD文档" });

                doc.SendStringToExecute("ZWCAD_出库1 ", true, false, false);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "出库命令已发送，请在CAD中选择插入点"
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { success = false, message = ex.Message });
            }
        }

        public async Task<string> StartExportExplode(long drawingId)
        {
            try
            {
                // ==================== 下载DWG ====================

                bool downloadSuccess =
                    await PrepareExportFile(drawingId);

                if (!downloadSuccess)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "下载图纸失败"
                    });
                }

                var doc = CadApp.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return JsonSerializer.Serialize(new { success = false, message = "未找到CAD文档" });

                doc.SendStringToExecute("ZWCAD_出库1_Explode ", true, false, false);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "炸开出库命令已发送，请在CAD中选择插入点"
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { success = false, message = ex.Message });
            }
        }
        private async Task DownloadFileAsync(
            string fileUrl,
            string localPath
)
        {
            using (var response = await httpClient.GetAsync(fileUrl))
            {
                response.EnsureSuccessStatusCode();

                using (var fs = new FileStream(
                    localPath,
                    FileMode.Create,
                    FileAccess.Write
                ))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
        }
        private async Task<bool> PrepareExportFile(long drawingId)
        {
            try
            {
                // ==================== 获取图纸详情 ====================

                string apiUrl =
                    $"api/drawings/detail?id={drawingId}";

                var response =
                    await httpClient.GetAsync(apiUrl);

                response.EnsureSuccessStatusCode();

                string json =
                    await response.Content.ReadAsStringAsync();

                // ==================== 解析 JSON ====================

                using (JsonDocument doc =
                       JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    // 判断业务状态
                    if (root.GetProperty("code").GetInt32() != 200)
                    {
                        return false;
                    }

                    // data
                    var data = root.GetProperty("data");

                    // 图纸编号
                    string code =
                        data.GetProperty("code").GetString();

                    // DWG路径
                    string filePath =
                        data.GetProperty("filePath").GetString();

                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        return false;
                    }

                    // ==================== chuku目录 ====================

                    string projectDir =
                        Path.GetDirectoryName(
                            System.Reflection.Assembly
                                .GetExecutingAssembly()
                                .Location
                        );

                    string chukuDir =
                        Path.Combine(projectDir, "chuku");

                    // 创建目录
                    if (!Directory.Exists(chukuDir))
                    {
                        Directory.CreateDirectory(chukuDir);
                    }

                    // 清理旧DWG
                    foreach (var file in Directory.GetFiles(
                                 chukuDir,
                                 "*.dwg"))
                    {
                        File.Delete(file);
                    }

                    // ==================== 本地保存路径 ====================

                    string localSavePath =
                        Path.Combine(
                            chukuDir,
                            code + ".dwg"
                        );

                    // ==================== 下载地址 ====================

                    string fullUrl =
                        System.Net.WebUtility.HtmlDecode(filePath);

                    // ==================== 下载文件 ====================

                    await DownloadFileAsync(
                        fullUrl,
                        localSavePath
                    );

                    // ==================== 检查文件 ====================

                    return File.Exists(localSavePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "出库文件准备失败：\n\n" +
                    ex.Message
                );

                return false;
            }
        }
        private async Task<bool> UploadDrawingsToBackend(long typeId)
        {
            try
            {
                string projectDir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location
                );

                string outputDir = Path.Combine(projectDir, "ruku");

                if (!Directory.Exists(outputDir))
                    return false;

                // 获取最新文件
                var files = new DirectoryInfo(outputDir)
                    .GetFiles()
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                var dwgFile = files.FirstOrDefault(
                    f => f.Extension.ToLower() == ".dwg"
                );

                var pngFile = files.FirstOrDefault(
                    f => f.Name.EndsWith(".png") &&
                    !f.Name.Contains("缩略图")
                );

                var thumbFile = files.FirstOrDefault(
                    f => f.Name.Contains("缩略图")
                );

                if (dwgFile == null || pngFile == null || thumbFile == null)
                {
                    MessageBox.Show("未找到生成的图纸文件");
                    return false;
                }

                // 获取属性
                string descProp = await GetDescPropForNode(typeId);

                if (descProp == null)
                    return false;

                using (var content = new MultipartFormDataContent())
                {
                    // typeId
                    content.Add(
                        new StringContent(typeId.ToString()),
                        "typeId"
                    );

                    // dwg
                    var dwgContent = new StreamContent(
                        dwgFile.OpenRead()
                    );

                    content.Add(
                        dwgContent,
                        "file",
                        dwgFile.Name
                    );

                    // preview
                    var previewContent = new StreamContent(
                        pngFile.OpenRead()
                    );

                    content.Add(
                        previewContent,
                        "previewFile",
                        pngFile.Name
                    );

                    // thumb
                    var thumbContent = new StreamContent(
                        thumbFile.OpenRead()
                    );

                    content.Add(
                        thumbContent,
                        "thumbFile",
                        thumbFile.Name
                    );

                    // 属性
                    content.Add(
                        new StringContent(descProp, Encoding.UTF8),
                        "descProp"
                    );

                    // 上传
                    var response = await httpClient.PostAsync(
                        "api/drawings/create",
                        content
                    );

                    var resultJson =
                        await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc =
                           JsonDocument.Parse(resultJson))
                    {
                        var root = doc.RootElement;

                        return root.GetProperty("code")
                                   .GetInt32() == 200;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

                return false;
            }
        }
        private async Task<string> GetDescPropForNode(long cabinetId)
        {
            try
            {
                var response = await httpClient.GetAsync(
                    $"api/drawings/simple?cabinetId={cabinetId}"
                );

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    // 接口失败
                    if (root.GetProperty("code").GetInt32() != 200)
                    {
                        return "{}";
                    }

                    // 获取 data
                    if (root.TryGetProperty("data", out var dataArray)
                        && dataArray.GetArrayLength() > 0)
                    {
                        var firstItem = dataArray[0];

                        // 获取 attrNames
                        if (firstItem.TryGetProperty("attrNames", out var attrNamesElement)
                            && attrNamesElement.ValueKind == JsonValueKind.Array)
                        {
                            var attrNames = attrNamesElement
                                .EnumerateArray()
                                .Select(x => x.GetString())
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList();

                            // 没有属性
                            if (attrNames.Count == 0)
                            {
                                return "{}";
                            }

                            // 生成空属性JSON
                            var properties = new Dictionary<string, string>();

                            foreach (var name in attrNames)
                            {
                                properties[name] = "";
                            }

                            return JsonSerializer.Serialize(properties);
                        }
                    }

                    // 默认空属性
                    return "{}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "获取属性定义失败：\n" + ex.Message
                );

                return "{}";
            }
        }
        private async Task<bool> SendCommandAndWaitAsync(Document doc, string executeString, string commandName)
        {
            var tcs = new TaskCompletionSource<bool>();

            // 【修改点】：正确的委托类型是 CommandEventHandler
            CommandEventHandler endedHandler = null;
            CommandEventHandler cancelledHandler = null;
            CommandEventHandler failedHandler = null;

            // 清理事件绑定的局部方法（防止内存泄漏）
            void CleanupEvents()
            {
                doc.CommandEnded -= endedHandler;
                doc.CommandCancelled -= cancelledHandler;
                doc.CommandFailed -= failedHandler;
            }

            // 1. 监听命令正常结束
            endedHandler = (s, e) =>
            {
                if (e.GlobalCommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    CleanupEvents();
                    tcs.TrySetResult(true);
                }
            };

            // 2. 监听命令被用户取消 (按了 ESC)
            cancelledHandler = (s, e) =>
            {
                if (e.GlobalCommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    CleanupEvents();
                    tcs.TrySetResult(false);
                }
            };

            // 3. 监听命令执行失败
            failedHandler = (s, e) =>
            {
                if (e.GlobalCommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    CleanupEvents();
                    tcs.TrySetResult(false);
                }
            };

            // 绑定监听器
            doc.CommandEnded += endedHandler;
            doc.CommandCancelled += cancelledHandler;
            doc.CommandFailed += failedHandler;

            // 发送执行命令
            doc.SendStringToExecute(executeString, true, false, false);

            // 设置一个兜底的超时机制（比如 60 秒），防止死等
            var timeoutTask = Task.Delay(60000);
            var finishedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (finishedTask == timeoutTask)
            {
                CleanupEvents();
                throw new Exception("等待 CAD 命令执行超时 (60秒)。");
            }

            return await tcs.Task;
        }
    }
}