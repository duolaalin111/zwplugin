using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using System.Threading.Tasks;

namespace ZrxDotNetCSProject5
{
    public partial class loginAntdUI : Form
    {
        private AntdUI.Input txtUsername;
        private AntdUI.Input txtPassword;
        private AntdUI.Button btnLogin;
        private AntdUI.Checkbox chkRemember;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private AntdUI.Panel cardPanel;

        public loginAntdUI()
        {
            InitializeComponent();
            this.Load += LoginAntdUI_Load;
        }

        private void LoginAntdUI_Load(object sender, EventArgs e)
        {
            SetupLoginUI();
        }

        private void SetupLoginUI()
        {
            // ==================== 窗体基础设置 ====================
            this.Text = "用户登录 - 图库管理系统";

            // ✅ 修复 1：禁用自动缩放，避免 DPI 问题
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.None;

            // ✅ 修复 2：设置固定初始大小
            this.Size = new Size(450, 620);
            this.ClientSize = new Size(450, 620);

            // ✅ 修复 3：设置最大尺寸，防止拉得太大
            this.MaximumSize = new Size(600, 800);
            this.MinimumSize = new Size(400, 550);

            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);

            // ✅ 修复 4：允许最大化/最小化
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // ✅ 修复 5：允许拖动边框调整大小
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // 顶部装饰条
            var topBar = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = 6,
                BackColor = Color.FromArgb(255, 153, 0)
            };
            this.Controls.Add(topBar);

            // ==================== 登录卡片容器 ====================
            cardPanel = new AntdUI.Panel
            {
                Size = new Size(350, 460),
                Back = Color.White,
                Radius = 12,
                Shadow = 15,
                ShadowColor = Color.FromArgb(0, 0, 0),
                ShadowOpacity = 0.1F,
                Padding = new Padding(40, 50, 40, 40),
                Dock = DockStyle.None
            };

            // ✅ 修复 6：使用 ClientSize 计算居中位置
            this.Resize += (s, e) => {
                if (cardPanel != null && this.ClientSize.Width > 0)
                {
                    cardPanel.Location = new Point(
                        (this.ClientSize.Width - cardPanel.Width) / 2,
                        (this.ClientSize.Height - cardPanel.Height) / 2 - 20  // 稍微偏上一点
                    );
                }
            };

            // 初始化位置 (首次加载时居中)
            cardPanel.Location = new Point(
                (this.ClientSize.Width - cardPanel.Width) / 2,
                (this.ClientSize.Height - cardPanel.Height) / 2 - 20
            );

            this.Controls.Add(cardPanel);

            // ==================== 标题区域 ====================
            lblTitle = new System.Windows.Forms.Label
            {
                Text = "欢迎登录",
                Font = new Font("Microsoft YaHei", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblTitle.Location = new Point((cardPanel.Width - lblTitle.Width) / 2, 60);
            cardPanel.Controls.Add(lblTitle);

            lblSubtitle = new System.Windows.Forms.Label
            {
                Text = "图库管理系统 v2.0",
                Font = new Font("Microsoft YaHei", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblSubtitle.Location = new Point(
                (cardPanel.Width - lblSubtitle.Width) / 2,
                lblTitle.Bottom + 5
            );
            cardPanel.Controls.Add(lblSubtitle);

            // ==================== 用户名输入框 ====================
            var lblUser = new System.Windows.Forms.Label
            {
                Text = "用户名",
                Font = new Font("Microsoft YaHei", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Location = new Point(40, 160)
            };
            cardPanel.Controls.Add(lblUser);

            txtUsername = new AntdUI.Input
            {
                Location = new Point(40, 180),
                Size = new Size(270, 42),
                PlaceholderText = "请输入用户名/工号",
                AllowClear = true,
                BorderWidth = 1F,
                BorderColor = Color.FromArgb(217, 217, 217),
                BorderActive = Color.FromArgb(255, 153, 0),
                Radius = 6,
                PrefixSvg = "<svg viewBox=\"0 0 1024 1024\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M512 64C264.6 64 64 264.6 64 512s200.6 448 448 448 448-200.6 448-448S759.4 64 512 64z m0 820c-205.4 0-372-166.6-372-372s166.6-372 372-372 372 166.6 372 372-166.6 372-372 372z\"/><path d=\"M512 256c-106 0-192 86-192 192s86 192 192 192 192-86 192-192-86-192-192-192z m0 320c-70.7 0-128-57.3-128-128s57.3-128 128-128 128 57.3 128 128-57.3 128-128 128z\"/><path d=\"M512 704c-141.4 0-264.6 79.4-329.4 196.6 97.6 73.6 214.2 117.4 329.4 117.4s231.8-43.8 329.4-117.4C776.6 783.4 653.4 704 512 704z\"/></svg>",
                PrefixFore = Color.FromArgb(150, 150, 150)
            };
            cardPanel.Controls.Add(txtUsername);

            // ==================== 密码输入框 ====================
            var lblPass = new System.Windows.Forms.Label
            {
                Text = "密码",
                Font = new Font("Microsoft YaHei", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Location = new Point(40, 245)
            };
            cardPanel.Controls.Add(lblPass);

            txtPassword = new AntdUI.Input
            {
                Location = new Point(40, 270),
                Size = new Size(270, 42),
                PlaceholderText = "请输入密码",
                PasswordChar = '●',
                AllowClear = true,
                BorderWidth = 1F,
                BorderColor = Color.FromArgb(217, 217, 217),
                BorderActive = Color.FromArgb(255, 153, 0),
                Radius = 6,
                PrefixSvg = "<svg viewBox=\"0 0 1024 1024\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M512 64C264.6 64 64 264.6 64 512s200.6 448 448 448 448-200.6 448-448S759.4 64 512 64z m0 820c-205.4 0-372-166.6-372-372s166.6-372 372-372 372 166.6 372 372-166.6 372-372 372z\"/><path d=\"M512 256c-106 0-192 86-192 192s86 192 192 192 192-86 192-192-86-192-192-192z m0 320c-70.7 0-128-57.3-128-128s57.3-128 128-128 128 57.3 128 128-57.3 128-128 128z\"/><path d=\"M512 704c-141.4 0-264.6 79.4-329.4 196.6 97.6 73.6 214.2 117.4 329.4 117.4s231.8-43.8 329.4-117.4C776.6 783.4 653.4 704 512 704z\"/></svg>",
                PrefixFore = Color.FromArgb(150, 150, 150)
            };
            cardPanel.Controls.Add(txtPassword);

            // ==================== 记住我 & 忘记密码 ====================
            chkRemember = new AntdUI.Checkbox
            {
                Text = "记住我",
                Location = new Point(40, 325),
                Font = new Font("Microsoft YaHei", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                Checked = false
            };
            cardPanel.Controls.Add(chkRemember);

            var lblForgot = new System.Windows.Forms.Label
            {
                Text = "忘记密码？",
                Font = new Font("Microsoft YaHei", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(255, 153, 0),
                AutoSize = true,
                Location = new Point(240, 327),
                Cursor = Cursors.Hand
            };
            lblForgot.Click += (s, e) => AntdUI.Message.info(this, "请联系管理员重置密码");
            lblForgot.MouseEnter += (s, e) => lblForgot.Font = new Font("Microsoft YaHei", 9, FontStyle.Underline);
            lblForgot.MouseLeave += (s, e) => lblForgot.Font = new Font("Microsoft YaHei", 9, FontStyle.Regular);
            cardPanel.Controls.Add(lblForgot);

            // ==================== 登录按钮 ====================
            btnLogin = new AntdUI.Button
            {
                Text = "登 录",
                Type = TTypeMini.Primary,
                Size = new Size(270, 45),
                Location = new Point(40, 375),
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                Radius = 8,
                BackColor = Color.FromArgb(255, 153, 0),
                ForeColor = Color.White
            };
            btnLogin.Click += BtnLogin_Click;
            cardPanel.Controls.Add(btnLogin);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(user))
            {
                AntdUI.Message.warn(this, "请输入用户名");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(pass))
            {
                AntdUI.Message.warn(this, "请输入密码");
                txtPassword.Focus();
                return;
            }

            btnLogin.Loading = true;
            btnLogin.Enabled = false;

            await Task.Delay(1500);

            if (user == "admin" && pass == "123456")
            {
                LoginStateManager.Login(user);

                AntdUI.Message.success(this, $"登录成功！欢迎 {user}");

                await Task.Delay(800);

                var mainForm = new LibraryManageAntdUI();
                mainForm.Show();
                this.Hide();
            }
            else
            {
                AntdUI.Message.error(this, "用户名或密码错误 (试一下 admin / 123456)");
                btnLogin.Loading = false;
                btnLogin.Enabled = true;
                txtPassword.Text = "";
                txtPassword.Focus();
            }
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            if (keyData == Keys.Enter)
            {
                if (!btnLogin.Loading)
                {
                    BtnLogin_Click(null, EventArgs.Empty);
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}