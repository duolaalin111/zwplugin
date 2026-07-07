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
using System.Linq;
using AntdUI;
using ZwSoft.ZwCAD.ApplicationServices;
using CadApp = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace ZrxDotNetCSProject5
{
    public partial class LibraryManageWeb : Form
    {
        private ChromiumWebBrowser browser;
        private HttpClient httpClient;
        private string apiBaseUrl = AppConfig.ApiBaseUrl;
        private string frontendUrl = AppConfig.FrontendUrl;
        public HttpClient HttpClient => httpClient;

        public void RefreshBrowser()
        {
            if (browser != null && browser.IsBrowserInitialized)
                browser.ExecuteScriptAsync("location.reload()");
        }

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
                settings.CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CefCache");
                settings.CefCommandLineArgs.Add("disable-web-security", "1");
                settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");
                CefSharpSettings.ConcurrentTaskExecution = true;
                Cef.Initialize(settings);
            }

            browser = new ChromiumWebBrowser(frontendUrl)
            {
                Dock = DockStyle.Fill
            };

            browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            browser.JavascriptObjectRepository.Register(
                "bound",
                new WebBridge(this),
                isAsync: true,
                options: BindingOptions.DefaultBinder
            );

            this.Controls.Add(browser);

            browser.IsBrowserInitializedChanged += (s, e) =>
            {
                if (browser.IsBrowserInitialized)
                {
                    this.Invoke(new Action(() =>
                    {
                        //browser.ShowDevTools();
                    }));
                }
            };

            browser.LoadError += (s, e) =>
            {
                MessageBox.Show($"加载前端失败：{e.ErrorText}", "错误");
            };
        }

        public async Task SaveDrawingDetail(DrawingDetail detail)
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

    public class WebBridge
    {
        private readonly LibraryManageWeb _parent;
        public WebBridge(LibraryManageWeb parent) { _parent = parent; }

        private static string ErrorJson(string message) =>
            JsonSerializer.Serialize(new { code = 500, message });

        public async Task<string> GetProductGroups()
        {
            try
            {
                var response = await _parent.HttpClient.GetAsync("api/model-groups");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) { return ErrorJson(ex.Message); }
        }

        public async Task<string> GetTreeData(int productId)
        {
            try
            {
                var response = await _parent.HttpClient.GetAsync($"api/product-schemes?productId={productId}");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) { return ErrorJson(ex.Message); }
        }

        public async Task<string> GetDrawings(long cabinetId)
        {
            try
            {
                var response = await _parent.HttpClient.GetAsync($"api/drawings/simple?cabinetId={cabinetId}");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) { return ErrorJson(ex.Message); }
        }

        public async Task<string> FilterDrawings(long cabinetId, string valScope)
        {
            try
            {
                var requestBody = new { cabinetId, valScope };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _parent.HttpClient.PostAsync("api/drawings/filter", content);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) { return ErrorJson(ex.Message); }
        }

        public async Task<string> GetDrawingDetail(long drawingId)
        {
            try
            {
                var response = await _parent.HttpClient.GetAsync($"api/drawings/detail?id={drawingId}");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) { return ErrorJson(ex.Message); }
        }

        public void EditDrawing(string detailJson)
        {
            _parent.Invoke(new Action(() => {
                var detail = JsonSerializer.Deserialize<DrawingDetail>(detailJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var editForm = new DrawingPropertiesEditorForm(detail);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    _parent.SaveDrawingDetail(editForm.UpdatedDrawingDetail);
                }
            }));
        }

        public void OpenDrawingDetail(long drawingId)
        {
            _parent.BeginInvoke(new Action(() => {
                try
                {
                    var dlg = new DrawingDetailDialog(drawingId, _parent.HttpClient,
                        exportAction: async (id) => {
                            var result = await StartExport(id);
                            JsonDocument data = JsonDocument.Parse(result);
                            bool ok = data.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
                            data.Dispose();
                            return ok;
                        },
                        exportExplodeAction: async (id) => {
                            var result = await StartExportExplode(id);
                            JsonDocument data = JsonDocument.Parse(result);
                            bool ok = data.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
                            data.Dispose();
                            return ok;
                        });
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.ShowDialog(_parent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("打开图纸详情失败: " + ex.Message);
                }
            }));
        }

        public async Task<string> DeleteDrawing(long id)
        {
            try
            {
                var response = await _parent.HttpClient.DeleteAsync($"api/drawings?id={id}");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) { return ErrorJson(ex.Message); }
        }

        public async Task<string> StartImport(long typeId, string token = "", string descProp = "")
        {
            try
            {
                var doc = CadApp.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return JsonSerializer.Serialize(new { success = false, message = "未找到CAD文档" });

                bool isSuccess = await _parent.SendCommandAndWaitAsync(doc, "ZWCAD_入库 ", "ZWCAD_入库");
                if (!isSuccess)
                    return JsonSerializer.Serialize(new { success = false, message = "入库命令失败" });

                var (uploadSuccess, uploadMsg) = await _parent.UploadDrawingsToBackend(typeId, descProp);

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
                bool downloadSuccess = await _parent.PrepareExportFile(drawingId);

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
                bool downloadSuccess = await _parent.PrepareExportFile(drawingId);

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
    }

    public partial class LibraryManageWeb
    {
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

        internal async Task<(bool success, string message)> UploadDrawingsToBackend(long typeId, string customDescProp = "")
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
                    return (false, "未找到PNG预览图");
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

        internal async Task<bool> SendCommandAndWaitAsync(Document doc, string executeString, string commandName)
        {
            return await CadHelper.SendCommandAndWaitAsync(doc, executeString, commandName);
        }
    }
}
