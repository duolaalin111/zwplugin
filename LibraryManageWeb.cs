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
        // ����������ԣ��� WebBridge ���Է��� httpClient
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

            // ==================== �ؼ���ע�� C# Bridge ====================
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
                        //browser.ShowDevTools();
                    }));
                }
            };

            browser.LoadError += (s, e) =>
            {
                MessageBox.Show($"����ǰ��ʧ�ܣ�{e.ErrorText}", "����");
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
    // ====================== WebBridge ======================
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
                    MessageBox.Show("��ͼֽ����ʧ��: " + ex.Message);
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
                    return JsonSerializer.Serialize(new { success = false, message = "δ�ҵ�CAD�ĵ�" });

                // 1. ����CAD���������� DWG + PNG �ļ�
                bool isSuccess = await _parent.SendCommandAndWaitAsync(doc, "ZWCAD_��� ", "ZWCAD_���");
                if (!isSuccess)
                    return JsonSerializer.Serialize(new { success = false, message = "�������ʧ��" });

                var (uploadSuccess, uploadMsg) = await _parent.UploadDrawingsToBackend(typeId, descProp);

                return JsonSerializer.Serialize(new
                {
                    success = uploadSuccess,
                    message = uploadSuccess ? "���ɹ�" : uploadMsg
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
                    return JsonSerializer.Serialize(new { success = false, message = "����ͼֽʧ��" });
                }

                var doc = CadApp.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return JsonSerializer.Serialize(new { success = false, message = "δ�ҵ�CAD�ĵ�" });

                doc.SendStringToExecute("ZWCAD_����1 ", true, false, false);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "���������ѷ��ͣ�����CAD��ѡ������"
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
                        message = "����ͼֽʧ��"
                    });
                }

                var doc = CadApp.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return JsonSerializer.Serialize(new { success = false, message = "δ�ҵ�CAD�ĵ�" });

                doc.SendStringToExecute("ZWCAD_����1_Explode ", true, false, false);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "ը�����������ѷ��ͣ�����CAD��ѡ������"
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
                        $"�����ļ�׼��ʧ�ܣ�HTTP {response.StatusCode}\n\n" +
                        $"��Ӧ����: {errorBody?.Substring(0, Math.Min(500, errorBody?.Length ?? 0))}\n\n" +
                        $"����URL: {httpClient.BaseAddress}{apiUrl}",
                        "�����������");
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
                    $"�����ļ�׼��ʧ�� (HTTP����)��\n\n{ex.Message}\n\n" +
                    $"����URL: {httpClient.BaseAddress}{apiUrl}",
                    "�����������");
                return false;
            }
            catch (JsonException ex)
            {
                MessageBox.Show(
                    $"�����ļ�׼��ʧ�� (JSON��������)��\n\n{ex.Message}\n\n" +
                    $"����URL: {httpClient.BaseAddress}{apiUrl}",
                    "�����������");
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
                    return (false, "rukuĿ¼������: " + outputDir);
                }

                var files = new DirectoryInfo(outputDir).GetFiles()
                    .OrderByDescending(f => f.LastWriteTime).ToList();

                var dwgFile = files.FirstOrDefault(f => f.Extension.ToLower() == ".dwg");
                var pngFile = files.FirstOrDefault(f => f.Name.EndsWith(".png") && !f.Name.Contains("����ͼ"));
                var thumbFile = files.FirstOrDefault(f => f.Name.Contains("����ͼ"));

                if (dwgFile == null)
                    return (false, "δ�ҵ�DWG�ļ�");
                if (pngFile == null)
                    return (false, "δ�ҵ�PNGԤ��ͼ ���� CAD����������δ����Ԥ��ͼ");
                if (thumbFile == null)
                    return (false, "δ�ҵ�����ͼ");

                string descProp = !string.IsNullOrEmpty(customDescProp)
                    ? customDescProp
                    : await GetDescPropForNode(typeId);
                if (descProp == null)
                    return (false, "��ȡ���Զ���ʧ�ܣ�typeId=" + typeId);

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
                            return (true, "���ɹ�");
                        var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "δ֪����";
                        return (false, $"API����code={code}: {msg}");
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
                MessageBox.Show("��ȡ���Զ���ʧ�ܣ�\n" + ex.Message);
                return "{}";
            }
        }
        internal async Task<bool> SendCommandAndWaitAsync(Document doc, string executeString, string commandName)
        {
            return await CadHelper.SendCommandAndWaitAsync(doc, executeString, commandName);
        }
    }
    }
