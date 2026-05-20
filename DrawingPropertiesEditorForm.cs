using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace ZrxDotNetCSProject5
{
    public partial class DrawingPropertiesEditorForm : Form
    {
        private DrawingDetail originalDetail;
        public DrawingDetail UpdatedDrawingDetail { get; private set; }

        private TextBox txtCode, txtName;
        private Dictionary<string, TextBox> propTextBoxes = new Dictionary<string, TextBox>();
        private TableLayoutPanel tableLayout;

        private Button btnSave, btnCancel;

        public DrawingPropertiesEditorForm(DrawingDetail detail)
        {
            originalDetail = detail;
            UpdatedDrawingDetail = CopyDetail(detail);
            InitializeDynamicForm();
            LoadData();
        }

        private void InitializeDynamicForm()
        {
            this.Text = "编辑图纸属性";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(5)
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(tableLayout, "图纸编号:", out txtCode);
            AddRow(tableLayout, "图纸名称:", out txtName);

            Label lblSeparator = new Label
            {
                Text = "扩展属性",
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3)
            };
            tableLayout.Controls.Add(lblSeparator, 0, tableLayout.RowCount);
            tableLayout.Controls.Add(new Label(), 1, tableLayout.RowCount);
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayout.RowCount++;

            if (!string.IsNullOrEmpty(originalDetail.DescProp))
            {
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(originalDetail.DescProp))
                    {
                        var root = doc.RootElement;
                        foreach (var property in root.EnumerateObject())
                        {
                            string key = property.Name;
                            Label lbl = new Label
                            {
                                Text = key + ":",
                                TextAlign = ContentAlignment.MiddleLeft,
                                AutoSize = true,
                                Margin = new Padding(3)
                            };
                            TextBox txt = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3) };
                            propTextBoxes[key] = txt;

                            tableLayout.Controls.Add(lbl, 0, tableLayout.RowCount);
                            tableLayout.Controls.Add(txt, 1, tableLayout.RowCount);
                            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                            tableLayout.RowCount++;
                        }
                    }
                }
                catch { }
            }

            if (propTextBoxes.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "（无扩展属性）",
                    ForeColor = Color.Gray,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3)
                };
                tableLayout.Controls.Add(lblEmpty, 0, tableLayout.RowCount);
                tableLayout.SetColumnSpan(lblEmpty, 2);
                tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tableLayout.RowCount++;
            }

            mainPanel.Controls.Add(tableLayout);

            FlowLayoutPanel btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.RightToLeft
            };
            btnSave = new Button { Text = "确认", DialogResult = DialogResult.OK, Width = 80 };
            btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 80 };
            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);

            this.Controls.Add(mainPanel);
            this.Controls.Add(btnPanel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void AddRow(TableLayoutPanel table, string labelText, out TextBox textBox)
        {
            Label lbl = new Label
            {
                Text = labelText,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(3)
            };
            textBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3) };
            table.Controls.Add(lbl, 0, table.RowCount);
            table.Controls.Add(textBox, 1, table.RowCount);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowCount++;
        }

        private void LoadData()
        {
            txtCode.Text = UpdatedDrawingDetail.Code;
            txtName.Text = UpdatedDrawingDetail.Name;

            if (!string.IsNullOrEmpty(UpdatedDrawingDetail.DescProp))
            {
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(UpdatedDrawingDetail.DescProp))
                    {
                        var root = doc.RootElement;
                        foreach (var property in root.EnumerateObject())
                        {
                            string key = property.Name;
                            string value = property.Value.GetString();
                            if (propTextBoxes.TryGetValue(key, out TextBox txt))
                            {
                                txt.Text = value;
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void SaveData()
        {
            UpdatedDrawingDetail.Code = txtCode.Text;
            UpdatedDrawingDetail.Name = txtName.Text;

            var propDict = new Dictionary<string, string>();
            foreach (var kv in propTextBoxes)
            {
                propDict[kv.Key] = kv.Value.Text;
            }
            UpdatedDrawingDetail.DescProp = propDict.Count > 0 ? JsonSerializer.Serialize(propDict) : "{}";
        }

        private DrawingDetail CopyDetail(DrawingDetail src)
        {
            return new DrawingDetail
            {
                Id = src.Id,           // 关键修复：复制 Id
                TypeId = src.TypeId,
                Code = src.Code,
                Name = src.Name,
                FilePath = src.FilePath,
                PreviewPath = src.PreviewPath,
                DescProp = src.DescProp,
                CreateTime = src.CreateTime,
                UpdateTime = src.UpdateTime
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
                SaveData();
            base.OnFormClosing(e);
        }
    }
}