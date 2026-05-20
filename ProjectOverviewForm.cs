using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AntdUI; // 关键命名空间

namespace ZrxDotNetCSProject5
{
    public partial class ProjectOverviewForm : Form
    {
        // 声明控件
        private AntdUI.Table table;
        private AntdUI.Pagination pagination;
        private AntdUI.Input txt项目名称, txt商机编号, txtWBS号;
        private AntdUI.Select sel创建方式, sel创建人员;
        private AntdUI.Button btn查询, btn重置;

        // 数据源
        private List<ProjectInfoUI> allData = new List<ProjectInfoUI>();
        private int pageSize = 10;
        private int currentPage = 1;

        public ProjectOverviewForm()
        {
            InitializeComponent();
            // 1. 第一步：在构造函数里画好界面
            InitUI();
        }

        private void ProjectOverviewForm_Load(object sender, EventArgs e)
        {
            // 2. 第二步：窗体加载时，生成假数据并展示
            LoadMockData();
            BindTableData();
        }

        // ==================== 核心方法区 ====================

        private void InitUI()
        {
            this.Text = "项目管理总览";
            this.Size = new Size(1300, 800);
            this.BackColor = Color.White;

            // --- 1. 顶部查询区 (必须先添加) ---
            var panelTop = new AntdUI.Panel
            {
                Dock = DockStyle.Top,
                Height = 80, // 稍微加高一点，给搜索按钮留空间
                Padding = new Padding(10, 15, 10, 5),
                BackColor = Color.FromArgb(248, 249, 250) // 浅灰色背景，区分查询区
            };

            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            // 辅助方法：快速创建带标签的输入框
            txt项目名称 = new AntdUI.Input { PlaceholderText = "请输入", Width = 140 };
            txt商机编号 = new AntdUI.Input { PlaceholderText = "请输入", Width = 140 };
            txtWBS号 = new AntdUI.Input { PlaceholderText = "请输入", Width = 140 };
            sel创建方式 = new AntdUI.Select { PlaceholderText = "请选择", Width = 120 };
            sel创建方式.Items.AddRange(new object[] { "手动", "自动" });
            sel创建人员 = new AntdUI.Select { PlaceholderText = "请选择", Width = 120 };

            btn查询 = new AntdUI.Button
            {
                Text = "搜索",
                Type = TTypeMini.Primary,
                IconSvg = "SearchOutlined", // 如果你有图标库
                BackColor = Color.FromArgb(67, 154, 134), // 还原截图中的青绿色
                Width = 80,
                Margin = new Padding(20, 0, 5, 0)
            };

            btn重置 = new AntdUI.Button
            {
                Text = "重置",
                Type = TTypeMini.Default,
                BackColor = Color.FromArgb(255, 102, 51), // 还原截图中的橙色
                ForeColor = Color.White,
                Width = 80
            };

            // 依次添加：标签 + 控件
            AddQueryItem(flowLayout, "项目名称", txt项目名称);
            AddQueryItem(flowLayout, "商机编号", txt商机编号);
            AddQueryItem(flowLayout, "WBS号", txtWBS号);
            AddQueryItem(flowLayout, "创建方式", sel创建方式);
            AddQueryItem(flowLayout, "创建人员", sel创建人员);
            flowLayout.Controls.Add(btn查询);
            flowLayout.Controls.Add(btn重置);

            panelTop.Controls.Add(flowLayout);

            // --- 2. 底部分页栏 (必须在 Fill 之前添加) ---
            var panelBottom = new AntdUI.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10)
            };
            pagination = new AntdUI.Pagination
            {
                Dock = DockStyle.Right, // 原型图中通常居右或居中
                Total = 100,
                PageSize = this.pageSize,
                ShowSizeChanger = true
            };
            panelBottom.Controls.Add(pagination);

            // --- 3. 中间表格 (最后添加，设置 Dock.Fill) ---
            table = new AntdUI.Table
            {
                Dock = DockStyle.Fill,
                // ... 列定义保持你之前的代码即可 ...
            };

            // 核心：添加顺序决定遮挡关系
            this.Controls.Add(table);        // 3. 填充层
            this.Controls.Add(panelTop);     // 1. 顶部层
            this.Controls.Add(panelBottom);  // 2. 底部层

            // 事件绑定...
            btn重置.Click += (s, e) => {
                txt项目名称.Text = txt商机编号.Text = txtWBS号.Text = "";
                sel创建方式.SelectedIndex = sel创建人员.SelectedIndex = -1;
            };
        }

        // 辅助方法：美化布局
        private void AddQueryItem(FlowLayoutPanel panel, string labelText, Control control)
        {
            var lbl = new AntdUI.Label
            {
                Text = labelText,
                AutoSize = true,
                Margin = new Padding(10, 7, 5, 0)
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(control);
        }

        private void LoadMockData()
        {
            // 生成 25 条测试数据
            for (int i = 1; i <= 25; i++)
            {
                allData.Add(new ProjectInfoUI
                {
                    ID = i,
                    序号 = i,
                    项目名称 = i == 1 ? "QQQ" : "测试项目" + i,
                    商机编号 = i == 1 ? "111" : "测试" + i,
                    WBS号 = "WBS-" + i.ToString("D3"),
                    创建时间 = DateTime.Now.AddDays(-i),
                    创建人员 = "zhangjian",
                    结构二次人员 = "",
                    被转派人员 = "",
                    创建方式 = "手动",
                    项目状态 = "进行中",
                    操作 = "删除"
                });
            }
        }

        private void BindTableData()
        {
            // 本地内存分页逻辑
            pagination.Total = allData.Count;
            var pageData = allData.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
            table.DataSource = pageData;
        }
    }

    // ==================== 数据模型区 ====================
    // 为了不污染你的原始类，建一个继承类专门给表格用
    public class ProjectInfoUI : ProjectInfo
    {
        public bool Checked { get; set; }
        public int 序号 { get; set; }
        public string 操作 { get; set; }
    }
}