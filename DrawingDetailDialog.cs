using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZrxDotNetCSProject5
{
    public partial class DrawingDetailDialog : Form
    {
        private long _drawingId;
        private HttpClient _httpClient;
        private TableLayoutPanel _mainPanel;
        private DataGridView _propsGrid;
        private Button _btnExport, _btnEdit, _btnDelete;
        private Label _lblTitle;
        private string _drawingCode = "";
        private string _drawingName = "";
        private string _descProp = "{}";
        private readonly Func<long, Task<bool>> _exportAction;
        private readonly Func<long, Task<bool>> _exportExplodeAction;

        public DrawingDetailDialog(long drawingId, HttpClient httpClient,
            Func<long, Task<bool>> exportAction = null,
            Func<long, Task<bool>> exportExplodeAction = null)
        {
            _drawingId = drawingId;
            _httpClient = httpClient;
            _exportAction = exportAction;
            _exportExplodeAction = exportExplodeAction;
            InitializeUI();
            this.Load += async (s, e) => await LoadDetail();
        }

        private void InitializeUI()
        {
            this.Text = "图纸详情";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(450, 500);
            this.MinimumSize = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.Sizable;

            _mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 3,
            };
            _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            _mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            _lblTitle = new Label
            {
                Text = "加载中...",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _propsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 30,
            };
            _propsGrid.Columns.Add("Key", "属性名");
            _propsGrid.Columns.Add("Value", "属性值");
            _propsGrid.Columns["Key"].Width = 130;
            _propsGrid.Columns["Key"].DefaultCellStyle.Font = new Font("微软雅黑", 9, FontStyle.Bold);
            _propsGrid.Columns["Value"].DefaultCellStyle.Font = new Font("微软雅黑", 9);

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 8, 0, 0),
            };

            _btnExport = new Button { Text = "📤 出库", Size = new Size(90, 35), Font = new Font("微软雅黑", 9) };
            _btnEdit = new Button { Text = "✏️ 编辑", Size = new Size(90, 35), Font = new Font("微软雅黑", 9) };
            _btnDelete = new Button { Text = "🗑️ 删除", Size = new Size(90, 35), Font = new Font("微软雅黑", 9), ForeColor = Color.Red };

            _btnExport.Click += async (s, e) => await BtnExport_Click();
            _btnEdit.Click += (s, e) => BtnEdit_Click();
            _btnDelete.Click += async (s, e) => await BtnDelete_Click();

            btnPanel.Controls.Add(_btnExport);
            btnPanel.Controls.Add(_btnEdit);
            btnPanel.Controls.Add(_btnDelete);

            _mainPanel.Controls.Add(_lblTitle, 0, 0);
            _mainPanel.Controls.Add(_propsGrid, 0, 1);
            _mainPanel.Controls.Add(btnPanel, 0, 2);
            this.Controls.Add(_mainPanel);
        }

        private async Task LoadDetail()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/drawings/detail?id={_drawingId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetInt32() != 200) return;
                    var data = root.GetProperty("data");

                    _drawingCode = data.GetProperty("code").GetString();
                    _drawingName = data.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
                    _descProp = data.TryGetProperty("descProp", out var dp) ? dp.GetString() ?? "{}" : "{}";

                    _lblTitle.Text = $"图纸详情 - {_drawingCode}";

                    _propsGrid.Rows.Add("图纸编号", _drawingCode);
                    _propsGrid.Rows.Add("图纸名称", _drawingName);
                    _propsGrid.Rows.Add("创建时间", data.TryGetProperty("createTime", out var ct) ? ct.GetString() ?? "-" : "-");
                    _propsGrid.Rows.Add("修改时间", data.TryGetProperty("updateTime", out var ut) ? ut.GetString() ?? "-" : "-");

                    if (!string.IsNullOrEmpty(_descProp) && _descProp != "{}")
                    {
                        try
                        {
                            using (JsonDocument propDoc = JsonDocument.Parse(_descProp))
                            {
                                foreach (var kv in propDoc.RootElement.EnumerateObject())
                                    _propsGrid.Rows.Add(kv.Name, kv.Value.ToString());
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _lblTitle.Text = "加载失败";
                _propsGrid.Rows.Add("错误", ex.Message);
            }
        }

        private async Task BtnExport_Click()
        {
            if (_exportAction == null && _exportExplodeAction == null)
            {
                MessageBox.Show("出库功能不可用");
                return;
            }
            var result = MessageBox.Show(
                "出库方式：\n「是」= 普通出库\n「否」= 炸开出库",
                "出库选项",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel) return;
            try
            {
                bool ok;
                if (result == DialogResult.Yes)
                    ok = _exportAction != null && await _exportAction(_drawingId);
                else
                    ok = _exportExplodeAction != null && await _exportExplodeAction(_drawingId);

                if (ok)
                    MessageBox.Show("出库命令已发送，请在CAD中选择插入点", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show("出库失败：" + ex.Message);
            }
        }

        private async Task BtnDelete_Click()
        {
            if (_drawingId <= 0) return;
            var confirm = MessageBox.Show(
                $"确定要删除图纸「{_drawingName}」吗？此操作不可恢复。",
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
            try
            {
                var response = await _httpClient.DeleteAsync($"api/drawings?id={_drawingId}");
                response.EnsureSuccessStatusCode();
                MessageBox.Show("删除成功！", "提示");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("删除失败：" + ex.Message); }
        }

        private void BtnEdit_Click()
        {
            try
            {
                var detail = new DrawingDetail { Id = (int)_drawingId, Code = _drawingCode, DescProp = _descProp };
                var editForm = new DrawingPropertiesEditorForm(detail);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    var updated = editForm.UpdatedDrawingDetail;
                    SaveDrawingDetail(updated).ContinueWith(t =>
                    {
                        if (t.Result)
                            this.Invoke(new Action(() => MessageBox.Show("属性已保存！", "提示")));
                    });
                }
            }
            catch { }
        }

        private async Task<bool> SaveDrawingDetail(DrawingDetail detail)
        {
            try
            {
                var body = new { id = detail.Id, properties = detail.DescProp };
                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/drawings/edit", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
