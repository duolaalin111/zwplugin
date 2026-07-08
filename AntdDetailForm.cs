using System;
using System.Data;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;

namespace ZrxDotNetCSProject5
{
    public partial class AntdDetailForm : Form
    {
        private long _drawingId;
        private HttpClient _http;
        private AntdUI.Table _propsTable;
        private System.Data.DataTable _dataTable;
        private System.Windows.Forms.Button _btnExport, _btnEdit, _btnDelete;
        private string _code = "", _name = "";
        private string _descProp = "{}", _filePath = "";
        private string _token;

        public AntdDetailForm(long drawingId, HttpClient http, string token = "")
        {
            _drawingId = drawingId;
            _http = http;
            _token = token;
            InitUI();
            this.Load += async (s, e) => await LoadDetail();
        }

        private void InitUI()
        {
            this.Text = "图纸详情";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(420, 480);
            this.MinimumSize = new Size(350, 300);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(245, 247, 250);

            var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 1 };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

            _dataTable = new System.Data.DataTable();
            _dataTable.Columns.Add("属性", typeof(string));
            _dataTable.Columns.Add("值", typeof(string));
            _dataTable.Rows.Add("加载中...", "");

            _propsTable = new AntdUI.Table
            {
                Dock = DockStyle.Fill, DataSource = _dataTable,
                Font = new Font("微软雅黑", 9), BackColor = Color.White
            };
            if (_propsTable.Columns.Count >= 2)
            { _propsTable.Columns[0].Width = "90"; _propsTable.Columns[1].Width = "270"; }

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0), BackColor = Color.FromArgb(250, 250, 250)
            };

            _btnExport = new System.Windows.Forms.Button { Text = "📤 出库", Margin = new Padding(4), Size = new Size(100, 36), Font = new Font("微软雅黑", 9), BackColor = Color.FromArgb(64, 158, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnEdit = new System.Windows.Forms.Button { Text = "✏️ 编辑", Margin = new Padding(4), Size = new Size(100, 36), Font = new Font("微软雅黑", 9) };
            _btnDelete = new System.Windows.Forms.Button { Text = "🗑️ 删除", Margin = new Padding(4), Size = new Size(100, 36), Font = new Font("微软雅黑", 9), BackColor = Color.FromArgb(245, 108, 108), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnExport.FlatAppearance.BorderSize = 0;
            _btnEdit.FlatAppearance.BorderSize = 0;
            _btnDelete.FlatAppearance.BorderSize = 0;

            _btnExport.Click += (s, e) => DoExport();
            _btnEdit.Click += (s, e) => DoEdit();
            _btnDelete.Click += (s, e) => DoDelete();

            btnPanel.Controls.Add(_btnExport);
            btnPanel.Controls.Add(_btnEdit);
            btnPanel.Controls.Add(_btnDelete);

            mainPanel.Controls.Add(_propsTable, 0, 0);
            mainPanel.Controls.Add(btnPanel, 0, 1);
            this.Controls.Add(mainPanel);
        }

        private async Task LoadDetail()
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"api/drawings/detail?id={_drawingId}");
                if (!string.IsNullOrEmpty(_token))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                var resp = await _http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetInt32() != 200) { MessageBox.Show("加载详情失败"); return; }
                    var data = root.GetProperty("data");
                    _code = data.GetProperty("code").GetString();
                    _name = data.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
                    _descProp = data.TryGetProperty("descProp", out var dp) ? dp.GetString() ?? "{}" : "{}";
                    _filePath = data.TryGetProperty("filePath", out var fp) ? fp.GetString() ?? "" : "";

                    _dataTable.Rows.Clear();
                    _dataTable.Rows.Add("图纸编号", _code);
                    _dataTable.Rows.Add("图纸名称", _name);
                    _dataTable.Rows.Add("创建时间", data.TryGetProperty("createTime", out var ct) ? ct.GetString() ?? "-" : "-");
                    _dataTable.Rows.Add("修改时间", data.TryGetProperty("updateTime", out var ut) ? ut.GetString() ?? "-" : "-");

                    if (!string.IsNullOrEmpty(_descProp) && _descProp != "{}")
                    {
                        using (JsonDocument propsDoc = JsonDocument.Parse(_descProp))
                        {
                            foreach (var kv in propsDoc.RootElement.EnumerateObject())
                                _dataTable.Rows.Add(kv.Name, kv.Value.ToString());
                        }
                    }
                    _propsTable.Refresh();
                }
            }
            catch (Exception ex) { _dataTable.Rows.Clear(); _dataTable.Rows.Add("错误", ex.Message); }
        }

        private async void DoExport()
        {
            var dlg = MessageBox.Show("「是」= 普通出库\n「否」= 炸开出库", "出库选项", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (dlg == DialogResult.Cancel) return;
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"api/drawings/detail?id={_drawingId}");
                if (!string.IsNullOrEmpty(_token))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                var resp = await _http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();
                string filePath;
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    filePath = doc.RootElement.GetProperty("data").GetProperty("filePath").GetString();
                }

                string projectDir = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                string chukuDir = System.IO.Path.Combine(projectDir, "chuku");
                if (!System.IO.Directory.Exists(chukuDir))
                    System.IO.Directory.CreateDirectory(chukuDir);

                string localPath = System.IO.Path.Combine(chukuDir, _code + ".dwg");
                string fullUrl = System.Net.WebUtility.HtmlDecode(filePath);

                var dlReq = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                var dlResp = await _http.SendAsync(dlReq);
                dlResp.EnsureSuccessStatusCode();
                using (var fs = new System.IO.FileStream(localPath, System.IO.FileMode.Create))
                    await dlResp.Content.CopyToAsync(fs);

                var cadDoc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (cadDoc != null)
                {
                    string cmd = dlg == DialogResult.Yes ? "ZWCAD_出库1 " : "ZWCAD_出库1_Explode ";
                    cadDoc.SendStringToExecute(cmd, true, false, false);
                    MessageBox.Show("出库命令已发送！", "提示");
                }
            }
            catch (Exception ex) { MessageBox.Show("出库失败：" + ex.Message); }
        }

        private async void DoDelete()
        {
            MessageBox.Show("点击了删除按钮，ID=" + _drawingId, "调试");
            if (_drawingId <= 0) return;
            if (MessageBox.Show($"确定删除「{_code}」？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Delete, $"api/drawings?id={_drawingId}");
                if (!string.IsNullOrEmpty(_token))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                var resp = await _http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                MessageBox.Show("删除成功！", "提示");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("删除失败：" + ex.Message); }
        }

        private async void DoEdit()
        {
            try
            {
                // 解析现有属性，构建输入表单
                var props = new System.Collections.Generic.Dictionary<string, string>();
                if (!string.IsNullOrEmpty(_descProp) && _descProp != "{}")
                {
                    using (JsonDocument d = JsonDocument.Parse(_descProp))
                        foreach (var kv in d.RootElement.EnumerateObject())
                            props[kv.Name] = kv.Value.ToString();
                }
                if (props.Count == 0)
                {
                    MessageBox.Show("该图纸没有可编辑的属性");
                    return;
                }

                // 动态创建编辑弹窗
                var form = new System.Windows.Forms.Form { Text = "编辑属性 - " + _code, Size = new Size(400, 150 + props.Count * 35), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false };
                var mainLayout = new System.Windows.Forms.TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 1 };
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

                var inputsPanel = new System.Windows.Forms.FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true };
                var inputs = new System.Collections.Generic.Dictionary<string, System.Windows.Forms.TextBox>();
                foreach (var kv in props)
                {
                    var row = new System.Windows.Forms.Panel { Size = new Size(360, 28) };
                    var lbl = new System.Windows.Forms.Label { Text = kv.Key, Width = 100, TextAlign = ContentAlignment.MiddleRight };
                    var txt = new System.Windows.Forms.TextBox { Left = 110, Width = 220, Text = kv.Value };
                    row.Controls.Add(lbl);
                    row.Controls.Add(txt);
                    row.Height = 30;
                    inputsPanel.Controls.Add(row);
                    inputs[kv.Key] = txt;
                }
                var btnSave = new System.Windows.Forms.Button { Text = "保存", Size = new Size(80, 28), DialogResult = DialogResult.OK };
                var btnCancel = new System.Windows.Forms.Button { Text = "取消", Size = new Size(80, 28), DialogResult = DialogResult.Cancel };
                var btnRow = new System.Windows.Forms.FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 5, 0, 0) };
                btnRow.Controls.Add(btnSave);
                btnRow.Controls.Add(btnCancel);

                mainLayout.Controls.Add(inputsPanel, 0, 0);
                mainLayout.Controls.Add(btnRow, 0, 1);
                form.Controls.Add(mainLayout);
                form.AcceptButton = btnSave;
                form.CancelButton = btnCancel;

                if (form.ShowDialog() != DialogResult.OK) return;

                // 收集用户输入
                var newProps = new System.Collections.Generic.Dictionary<string, object>();
                foreach (var kv in inputs)
                    newProps[kv.Key] = kv.Value.Text;

                // 发送 API
                var body = new { id = _drawingId, properties = (object)newProps };
                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var req = new HttpRequestMessage(HttpMethod.Post, "api/drawings/edit") { Content = content };
                if (!string.IsNullOrEmpty(_token))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                var resp = await _http.SendAsync(req);
                var respBody = await resp.Content.ReadAsStringAsync();
                if (resp.IsSuccessStatusCode)
                {
                    _descProp = JsonSerializer.Serialize(newProps);
                    // 刷新属性表格
                    _dataTable.Rows.Clear();
                    await LoadDetail();
                    MessageBox.Show("属性已保存！", "提示");
                }
                else
                    MessageBox.Show("编辑失败：" + respBody, "错误");
            }
            catch (Exception ex) { MessageBox.Show("编辑异常：" + ex.Message); }
        }
    }
}
