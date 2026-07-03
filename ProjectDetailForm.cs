using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZrxDotNetCSProject5.newModels;
namespace ZrxDotNetCSProject5
{
    public partial class ProjectDetailForm : Form // 推荐继承 AntdUI.Window
    {
        private Projectmodel _currentProject;

        internal ProjectDetailForm(Projectmodel project)
        {
            InitializeComponent();
            _currentProject = project;

            // 示例：把项目信息显示到窗体控件上
            // 假设你有 Label 或 Input 控件
            //lblProjectName.Text = project.Name ?? "未知项目";
            //txtBusinessCode.Text = project.BusinessCode ?? "";
            //txtWBS.Text = project.WBS ?? "";
            //txtCreateTime.Text = project.CreateTime ?? "";
            //txtCreator.Text = project.Creator ?? "";
            // ... 其他字段类似

            // 窗体标题已经在调用处设置，这里也可以再确认一次
            // this.Text = $"项目详情配置 - {project.Name}";
        }

        // 后续可以加保存按钮、校验逻辑等
        private void btnSave_Click(object sender, EventArgs e)
        {
            // 示例：保存修改（如果允许编辑）
            //_currentProject.BusinessCode = txtBusinessCode.Text;
            // ... 更新其他字段

            // 关闭窗体
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ProjectDetailForm_Load(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tree1_SelectChanged(object sender, AntdUI.TreeSelectEventArgs e)
        {

        }
    }
}
