using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZrxDotNetCSProject5
{
    // 创建一个简单的输入窗体
    public partial class InputNameForm : Form
    {
        public string InputName { get; private set; }

        public InputNameForm()
        {
            InitializeComponent();

            this.Text = "请输入图纸名称";
            this.Size = new Size(300, 150);

            // 创建控件
            Label lbl = new Label
            {
                Text = "请输入名称：",
                Location = new Point(10, 20),
                Size = new Size(280, 20)
            };

            TextBox txtName = new TextBox
            {
                Location = new Point(10, 50),
                Size = new Size(260, 20)
            };

            Button btnOk = new Button
            {
                Text = "确认",
                Location = new Point(200, 80),
                Size = new Size(75, 30)
            };
            btnOk.Click += (sender, e) =>
            {
                InputName = txtName.Text.Trim();
                if (!string.IsNullOrEmpty(InputName))
                {
                    this.DialogResult = DialogResult.OK; // 确认后关闭
                }
                else
                {
                    MessageBox.Show("名称不能为空！");
                }
            };

            this.Controls.Add(lbl);
            this.Controls.Add(txtName);
            this.Controls.Add(btnOk);
        }

        private void InputNameForm_Load(object sender, EventArgs e)
        {

        }
    }
}
