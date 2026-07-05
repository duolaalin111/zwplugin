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
        private string apiBaseUrl = AppConfig.ApiBaseUrl;
        private string frontendUrl = AppConfig.FrontendUrl;
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
        public async Task<string> StartImport(long typeId, string token = "", string descProp = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(token))
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var doc = CadApp.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return JsonSerializer.Serialize(new { success = false, message = "未找到CAD文档" });

                // 1. 运行CAD入库命令，生成 DWG + PNG 文件
                bool isSuccess = await SendCommandAndWaitAsync(doc, "ZWCAD_入库 ", "ZWCAD_入库");
                if (!isSuccess)
                    return JsonSerializer.Serialize(new { success = false, message = "入库命令失败" });

                // 2. 上传生成的文件到后端API
                var (uploadSuccess, uploadMsg) = await UploadDrawingsToBackend(typeId, descProp);

                return JsonSerializer.Serialize(new
                {
                    success = uploadSuccess,
                    message = uploadSuccess ? "入库成功" : uploadMsg
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { success = false, message = ex.Message });
            }
        }
        public async Task<string> StartExport(long drawingId, string token = "")
        {
            try
            {
                httpClient.DefaultRequestHeaders.Authorization = null;
                if (!string.IsNullOrEmpty(token))
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                bool downloadSuccess = await PrepareExportFile(drawingId);

                if (!downloadSuccess)
                {
                    return JsonSerializer.Serialize(new { success = false, message = "下载图纸失败" });
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

        public async Task<string> StartExportExplode(long drawingId, string token = "")
        {
            try
            {
                httpClient.DefaultRequestHeaders.Authorization = null;
                if (!string.IsNullOrEmpty(token))
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                bool downloadSuccess = await PrepareExportFile(drawingId);

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
        private async Task DownloadFileAsync(string fileUrl, string localPath)
        {
            await CadHelper.DownloadFileAsync(httpClient, fileUrl, localPath);
        }
        public async Task<bool> PrepareExportFile(long drawingId)
        {
            string apiUrl = $"api/drawings/detail?id={drawingId}";
            try
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(
                        $"出库文件准备失败：HTTP {response.StatusCode}\n\n" +
                        $"响应内容: {errorBody?.Substring(0, Math.Min(500, errorBody?.Length ?? 0))}\n\n" +
                        $"请求URL: {httpClient.BaseAddress}{apiUrl}",
                        "出库错误详情");
                    return false;
                }

                string json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.GetProperty("code").GetInt32() != 200)
                        return false;

                    var data = root.GetProperty("data");
                    string code = data.GetProperty("code").GetString();
                    string filePath = data.GetProperty("filePath").GetString();

                    if (string.IsNullOrWhiteSpace(filePath))
                        return false;

                    string projectDir = Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location);

                    string chukuDir = Path.Combine(projectDir, "chuku");
                    if (!Directory.Exists(chukuDir))
                        Directory.CreateDirectory(chukuDir);

                    foreach (var file in Directory.GetFiles(chukuDir, "*.dwg"))
                        File.Delete(file);

                    string localSavePath = Path.Combine(chukuDir, code + ".dwg");
                    string fullUrl = System.Net.WebUtility.HtmlDecode(filePath);

                    await DownloadFileAsync(fullUrl, localSavePath);

                    return File.Exists(localSavePath);
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    $"出库文件准备失败 (HTTP错误)：\n\n{ex.Message}\n\n" +
                    $"请求URL: {httpClient.BaseAddress}{apiUrl}",
                    "出库错误详情");
                return false;
            }
            catch (JsonException ex)
            {
                MessageBox.Show(
                    $"出库文件准备失败 (JSON解析错误)：\n\n{ex.Message}\n\n" +
                    $"请求URL: {httpClient.BaseAddress}{apiUrl}",
                    "出库错误详情");
                return false;
            }
        }
        private async Task<(bool success, string message)> UploadDrawingsToBackend(long typeId, string customDescProp = "")
        {
            try
            {
                string projectDir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                string outputDir = Path.Combine(projectDir, "ruku");

                if (!Directory.Exists(outputDir))
                {
                    return (false, "ruku目录不存在: " + outputDir);
                }

                var files = new DirectoryInfo(outputDir).GetFiles()
                    .OrderByDescending(f => f.LastWriteTime).ToList();

                var dwgFile = files.FirstOrDefault(f => f.Extension.ToLower() == ".dwg");
                var pngFile = files.FirstOrDefault(f => f.Name.EndsWith(".png") && !f.Name.Contains("缩略图"));
                var thumbFile = files.FirstOrDefault(f => f.Name.Contains("缩略图"));

                if (dwgFile == null)
                    return (false, "未找到DWG文件");
                if (pngFile == null)
                    return (false, "未找到PNG预览图 —— CAD入库命令可能未生成预览图");
                if (thumbFile == null)
                    return (false, "未找到缩略图");

                string descProp = !string.IsNullOrEmpty(customDescProp)
                    ? customDescProp
                    : await GetDescPropForNode(typeId);
                if (descProp == null)
                    return (false, "获取属性定义失败，typeId=" + typeId);

                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(typeId.ToString()), "typeId");
                    content.Add(new StreamContent(dwgFile.OpenRead()), "file", dwgFile.Name);
                    content.Add(new StreamContent(pngFile.OpenRead()), "previewFile", pngFile.Name);
                    content.Add(new StreamContent(thumbFile.OpenRead()), "thumbFile", thumbFile.Name);
                    content.Add(new StringContent(descProp, Encoding.UTF8), "descProp");

                    var response = await httpClient.PostAsync("api/drawings/create", content);
                    var resultJson = await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(resultJson))
                    {
                        var root = doc.RootElement;
                        var code = root.GetProperty("code").GetInt32();
                        if (code == 200)
                            return (true, "入库成功");
                        var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "未知错误";
                        return (false, $"API返回code={code}: {msg}");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        private async Task<string> GetDescPropForNode(long cabinetId)
        {
            try
            {
                return await CadHelper.GetDescPropForNode(httpClient, cabinetId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取属性定义失败：\n" + ex.Message);
                return "{}";
            }
        }
        private async Task<bool> SendCommandAndWaitAsync(Document doc, string executeString, string commandName)
        {
            return await CadHelper.SendCommandAndWaitAsync(doc, executeString, commandName);
        }
    }
    }
