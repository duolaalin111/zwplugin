using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI; // 引入 AntdUI 命名空间

namespace ZrxDotNetCSProject5
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            SetupAntdComponents();
        }

        private void SetupAntdComponents()
        {
            this.Controls.Clear();
            this.Text = "AntdUI 组件演示";
            this.Size = new Size(1000, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;

            // 1. 按钮组 (使用 TTypeMini)
            var btnPrimary = new AntdUI.Button
            {
                Text = "Primary",
                Type = TTypeMini.Primary,
                Shape = TShape.Default,
                Location = new Point(20, 20),
                Size = new Size(100, 35)
            };
            this.Controls.Add(btnPrimary);

            var btnSuccess = new AntdUI.Button
            {
                Text = "Success",
                Type = TTypeMini.Success,
                Shape = TShape.Default,
                Location = new Point(130, 20),
                Size = new Size(100, 35)
            };
            this.Controls.Add(btnSuccess);

            // 2. 输入框 (Input) - ✅ 使用 PlaceholderText (文档指定)
            var inputText = new AntdUI.Input
            {
                PlaceholderText = "输入文本",
                Location = new Point(20, 70),
                Size = new Size(200, 35)
            };
            this.Controls.Add(inputText);

            var inputPassword = new AntdUI.Input
            {
                PlaceholderText = "输入密码",
                UseSystemPasswordChar = true,
                Location = new Point(20, 120),
                Size = new Size(200, 35)
            };
            this.Controls.Add(inputPassword);

            // 3. 日期选择器 (DatePicker)
            var datePicker = new AntdUI.DatePicker
            {
                Location = new Point(230, 70),
                Size = new Size(200, 35),
                Format = "yyyy-MM-dd"
            };
            this.Controls.Add(datePicker);

            // 4. 下拉选择框 (Select) - ✅ 使用 Placeholder (文档指定)
            var select = new AntdUI.Select
            {
                PlaceholderText = "选择选项",
                Location = new Point(230, 120),
                Size = new Size(200, 35)
            };
            select.Items.Add(new SelectItem("选项1", "1"));
            select.Items.Add(new SelectItem("选项2", "2"));
            select.Items.Add(new SelectItem("选项3", "3"));
            this.Controls.Add(select);

           

            // 6. 消息提示 (Message) - ✅ 完全匹配文档规范 (直接调用 Message.Info())
            var btnMessage = new AntdUI.Button
            {
                Text = "显示消息",
                Type = TTypeMini.Primary,
                Location = new Point(20, 350),
                Size = new Size(100, 35)
            };
            btnMessage.Click += (s, e) =>
            {
                AntdUI.Message.info(this, "这是一个消息提示！");
            };
            this.Controls.Add(btnMessage);


            // 7. 标签页 (Tabs) - ✅ 修正为使用 Pages 属性 (文档指定)
            var tabs = new AntdUI.Tabs
            {
                Location = new Point(20, 400),
                Size = new Size(300, 200)
            };

            // ✅ 修正1: 使用 Text 属性设置标题（文档指定）
            // ✅ 修正2: 通过 Controls.Add 添加内容控件（文档未指定 Content 属性）
            var tabPage1 = new AntdUI.TabPage { Text = "标签1" };
            var label1 = new AntdUI.Label { Text = "标签1内容", AutoSize = true, Location = new Point(10, 10) };
            tabPage1.Controls.Add(label1); // 添加内容

            var tabPage2 = new AntdUI.TabPage { Text = "标签2" };
            var label2 = new AntdUI.Label { Text = "标签2内容", AutoSize = true, Location = new Point(10, 10) };
            tabPage2.Controls.Add(label2); // 添加内容

            tabs.Pages.Add(tabPage1);
            tabs.Pages.Add(tabPage2);
            this.Controls.Add(tabs);
        }
    }
}