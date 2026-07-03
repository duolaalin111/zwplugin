using AntdUI;
using ZrxDotNetCSProject5.newModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;

namespace ZrxDotNetCSProject5
{
    public partial class Form1try : Form
    {
        private AntdUI.Window window;
        AntList<Projectmodel> antList;
        Projectmodel promodel;
        public Form1try()
        {
            InitializeComponent();
            InitTableColumns();          // 初始化列配置
            BindEventHandler();          // 绑定事件（如按钮点击）
            this.Load += Form1try_Load;  // 窗体加载时获取数据
        }

        private async void Form1try_Load(object sender, EventArgs e)
        {
            await LoadProjectsAsync();
        }

        private async Task LoadProjectsAsync()
        {
            AntdUI.Message.info(this, "正在加载");
            try
            {
                var projects = await ApiHelper.GetProjectsAsync();

                if (projects != null)
                {
                    foreach (var pro in projects)
                    {
                        pro.CellLinks = new CellLink[]
                        {
                    new CellButton(Guid.NewGuid().ToString(), "项目详情配置", TTypeMini.Primary),
                    new CellButton(Guid.NewGuid().ToString(), "删除", TTypeMini.Error)
                        };
                    }
                    antList = new AntList<Projectmodel>(projects);
                    table1.Binding(antList);
                }
                else
                {
                    AntdUI.Message.warn(this, "加载失败，无法获取项目列表，请检查网络或后端服务。");
                }
            }
            catch (HttpRequestException ex)
            {
                AntdUI.Message.error(this, $"请求失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"发生错误：{ex.Message}");
            }
        }
        private void InitTableColumns()
        {
            var columns = new AntdUI.ColumnCollection();

            table1.Columns = new ColumnCollection()
            {
                new ColumnCheck("Selected"){Fixed = true},
                new Column("","序号"){
                    Width = "50",
                    Render = (value,record,rowindex)=>{return (rowindex+1); },
                    Fixed = true,//冻结列
                },
                new Column("Name", "项目名称", ColumnAlign.Center)
                {
                    Width="120",
                },
                new Column("BusinessCode", "商机编号",ColumnAlign.Center),
                new Column("WBS", "WBS号",ColumnAlign.Center),
                new Column("CreateTime", "创建时间",ColumnAlign.Center),
                new Column("Creator", "创建人员",ColumnAlign.Center),
                new Column("Structure", "结构/二次人员",ColumnAlign.Center),
                new Column("Assigned", "被转派人员",ColumnAlign.Center),
                new Column("CreateType", "创建方式",ColumnAlign.Center),
                new Column("Status", "项目状态",ColumnAlign.Center),
                //new Column("CellLinks", "操作栏", ColumnAlign.Center),
                new Column("CellLinks","操作栏",ColumnAlign.Center){
                    Width = "150",
                    Fixed = true,//冻结列
                },
            };

        }
        private void BindEventHandler()
        {
            //buttonADD.Click += ButtonADD_Click;
            //buttonDEL.Click += ButtonDEL_Click;
            table1.CellButtonClick += Table1_CellButtonClick;
        }
        private async void Table1_CellButtonClick(object sender, TableButtonEventArgs e)
        {
            var buttontext = e.Btn.Text;

            if (e.Record is Projectmodel pro)
            {
                promodel = pro;
                switch (buttontext)
                {
                    //暂不支持进入整行编辑，只支持指定单元格编辑，推荐使用弹窗或抽屉编辑整行数据
                    case "项目详情配置":
                        // 创建详情窗体，并把当前行数据传进去
                        var detailForm = new ProjectDetailForm(pro);

                        // 动态设置窗体标题：包含项目名称
                        detailForm.Text = $"项目详情配置 - {pro.Name}";

                        // 以模态方式打开（阻塞式，用户必须关闭详情窗体才能继续操作主窗体）
                        detailForm.ShowDialog();

                        // 可选：如果详情窗体有“保存”操作并修改了数据，这里可以刷新表格
                        // table1.Binding(antList);  // 或 table1.Refresh();
                        break;
                    case "删除":
                        var confirm = AntdUI.Modal.open(this, "删除警告！", "确认要删除选择的数据吗？", TType.Warn);
                        if (confirm == DialogResult.OK)
                        {
                            AntdUI.Message.info(this, "删除中...");
                            try
                            {
                                bool success = await ApiHelper.DeleteProjectAsync(pro.Id);
                                if (success)
                                {
                                    antList.Remove(pro);
                                    AntdUI.Message.success(this, "删除成功，项目已删除。");
                                }
                                else
                                {
                                    AntdUI.Message.error(this, "删除失败，后端删除失败，请稍后重试。");
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                AntdUI.Message.error(this, $"请求失败：{ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                AntdUI.Message.error(this, $"发生错误：{ex.Message}");
                            }
                        }
                        break;

                }
            }
        }
        //搜索
        private async void button1_Click(object sender, EventArgs e)
        {
            // 从 UI 控件获取筛选条件
            var criteria = new ProjectSearchCriteria
            {
                projectName = input1.Text,          // 项目名称输入框
                businessCode = input2.Text,         // 商机编号
                wbs = input3.Text,                  // WBS 编号
                createType = select1.SelectedValue?.ToString(), // 创建方式下拉框值
                createBy = select2.SelectedValue?.ToString()    // 创建人员下拉框值
            };

            // 如果下拉框的选项值为“全部”或空，则不传递该字段（设为 null）
            if (string.IsNullOrEmpty(criteria.createType)) criteria.createType = null;
            if (string.IsNullOrEmpty(criteria.createBy)) criteria.createBy = null;

            AntdUI.Message.info(this, "搜索中...");
            try
            {
                var result = await ApiHelper.SearchProjectsAsync(criteria);

                if (result != null)
                {
                    // 重新为每个项目添加操作栏
                    foreach (var pro in result)
                    {
                        pro.CellLinks = new CellLink[]
                        {
                    new CellButton(Guid.NewGuid().ToString(), "项目详情配置", TTypeMini.Primary),
                    new CellButton(Guid.NewGuid().ToString(), "删除", TTypeMini.Error)
                        };
                    }

                    antList = new AntList<Projectmodel>(result);
                    table1.Binding(antList);
                    AntdUI.Message.success(this, "搜索完成");
                }
                else
                {
                    AntdUI.Message.warn(this, "查询失败，搜索请求失败，请稍后重试。");
                }
            }
            catch (HttpRequestException ex)
            {
                AntdUI.Message.error(this, $"请求失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"发生错误：{ex.Message}");
            }
        }
        //项目名称
        private void input1_TextChanged(object sender, EventArgs e)
        {

        }
        //创建人员
        private void select2_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
        {

        }

        private void 项目管理表_CellClick(object sender, AntdUI.TableClickEventArgs e)
        {

        }
        
        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            
        }
        //商机需求
        private void input2_TextChanged(object sender, EventArgs e)
        {

        }
        //WBS编号
        private void input3_TextChanged(object sender, EventArgs e)
        {

        }
        //创建方式
        private void select1_SelectedIndexChanged(object sender, IntEventArgs e)
        {

        }
        //重置
        private async void button2_Click(object sender, EventArgs e)
        {
            // 清空输入框
            input1.Text = string.Empty;
            input2.Text = string.Empty;
            input3.Text = string.Empty;

            // 重置下拉框的选择（假设第一个选项是“全部”，设置索引为0；如果无“全部”则设为-1）
            if (select1.Items.Count > 0) select1.SelectedIndex = -1;
            if (select2.Items.Count > 0) select2.SelectedIndex = -1;

            // 重新加载全部数据
            await LoadProjectsAsync();
        }
    }
}
