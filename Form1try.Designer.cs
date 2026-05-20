using AntdUI;
using System.Drawing;
using System.Windows.Forms;
namespace ZrxDotNetCSProject5
{
    partial class Form1try
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.select2 = new AntdUI.Select();
            this.button2 = new AntdUI.Button();
            this.input1 = new AntdUI.Input();
            this.input2 = new AntdUI.Input();
            this.select1 = new AntdUI.Select();
            this.table1 = new AntdUI.Table();
            this.pagination1 = new AntdUI.Pagination();
            this.input3 = new AntdUI.Input();
            this.button1 = new AntdUI.Button();
            this.button3 = new AntdUI.Button();
            this.button4 = new AntdUI.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 7;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 53.21429F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46.78571F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 183F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 214F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 123F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 141F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 118F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 106F));
            this.tableLayoutPanel1.Controls.Add(this.select2, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.button2, 11, 0);
            this.tableLayoutPanel1.Controls.Add(this.input1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.input2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.select1, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.table1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.pagination1, 4, 2);
            this.tableLayoutPanel1.Controls.Add(this.input3, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.button1, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.button3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.button4, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.58559F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 89.41441F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1216, 712);
            this.tableLayoutPanel1.TabIndex = 0;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // select2
            // 
            this.select2.Location = new System.Drawing.Point(754, 4);
            this.select2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.select2.Name = "select2";
            this.select2.PrefixText = "创建人员：";
            this.select2.Size = new System.Drawing.Size(206, 60);
            this.select2.TabIndex = 6;
            this.select2.SelectedIndexChanged += new AntdUI.IntEventHandler(this.select2_SelectedIndexChanged);
            // 
            // button2
            // 
            this.button2.BackActive = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(217)))), ((int)(((byte)(224)))));
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(161)))), ((int)(((byte)(255)))));
            this.button2.Font = new System.Drawing.Font("宋体", 10F);
            this.button2.Location = new System.Drawing.Point(1096, 4);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button2.Name = "button2";
            this.button2.OriginalBackColor = System.Drawing.Color.WhiteSmoke;
            this.button2.Size = new System.Drawing.Size(116, 60);
            this.button2.TabIndex = 1;
            this.button2.Text = "重置";
            this.button2.Type = AntdUI.TTypeMini.Primary;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // input1
            // 
            this.input1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.input1.Font = new System.Drawing.Font("宋体", 10F);
            this.input1.Location = new System.Drawing.Point(2, 2);
            this.input1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.input1.Name = "input1";
            this.input1.PrefixText = "项目名称：";
            this.input1.Size = new System.Drawing.Size(175, 65);
            this.input1.TabIndex = 2;
            this.input1.TextChanged += new System.EventHandler(this.input1_TextChanged);
            // 
            // input2
            // 
            this.input2.Location = new System.Drawing.Point(181, 2);
            this.input2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.input2.Name = "input2";
            this.input2.PrefixText = "商机需求：";
            this.input2.Size = new System.Drawing.Size(154, 65);
            this.input2.TabIndex = 3;
            this.input2.TextChanged += new System.EventHandler(this.input2_TextChanged);
            // 
            // select1
            // 
            this.select1.Location = new System.Drawing.Point(569, 2);
            this.select1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.select1.Name = "select1";
            this.select1.PrefixText = "创建方式：";
            this.select1.Size = new System.Drawing.Size(179, 65);
            this.select1.TabIndex = 5;
            this.select1.SelectedIndexChanged += new AntdUI.IntEventHandler(this.select1_SelectedIndexChanged);
            // 
            // table1
            // 
            this.table1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.tableLayoutPanel1.SetColumnSpan(this.table1, 7);
            this.table1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.table1.Gap = 12;
            this.table1.Location = new System.Drawing.Point(4, 73);
            this.table1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.table1.Name = "table1";
            this.table1.Size = new System.Drawing.Size(1208, 576);
            this.table1.TabIndex = 7;
            this.table1.Text = "table1";
            this.table1.CellClick += new AntdUI.Table.ClickEventHandler(this.项目管理表_CellClick);
            // 
            // pagination1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.pagination1, 3);
            this.pagination1.Location = new System.Drawing.Point(754, 657);
            this.pagination1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pagination1.Name = "pagination1";
            this.pagination1.Size = new System.Drawing.Size(458, 51);
            this.pagination1.TabIndex = 8;
            this.pagination1.Text = "pagination1";
            this.pagination1.Total = 50;
            // 
            // input3
            // 
            this.input3.Location = new System.Drawing.Point(339, 2);
            this.input3.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.input3.Name = "input3";
            this.input3.PrefixText = "WBS编号：";
            this.input3.Size = new System.Drawing.Size(226, 65);
            this.input3.TabIndex = 4;
            this.input3.TextChanged += new System.EventHandler(this.input3_TextChanged);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(113)))), ((int)(((byte)(186)))), ((int)(((byte)(93)))));
            this.button1.Font = new System.Drawing.Font("宋体", 10F);
            this.button1.Location = new System.Drawing.Point(968, 4);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(118, 60);
            this.button1.TabIndex = 0;
            this.button1.Text = "搜索";
            this.button1.Type = AntdUI.TTypeMini.Primary;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(113)))), ((int)(((byte)(186)))), ((int)(((byte)(93)))));
            this.button3.Location = new System.Drawing.Point(4, 657);
            this.button3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(171, 51);
            this.button3.TabIndex = 9;
            this.button3.Text = "项目详情配置";
            this.button3.Type = AntdUI.TTypeMini.Primary;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(73)))), ((int)(((byte)(73)))));
            this.button4.Location = new System.Drawing.Point(183, 657);
            this.button4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(148, 51);
            this.button4.TabIndex = 10;
            this.button4.Text = "删除";
            this.button4.Type = AntdUI.TTypeMini.Primary;
            // 
            // Form1try
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1216, 712);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Form1try";
            this.Text = "Form1";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private AntdUI.Button button1;
        private AntdUI.Button button2;
        private AntdUI.Input input1;
        private AntdUI.Input input2;
        private AntdUI.Input input3;
        private AntdUI.Select select2;
        private AntdUI.Select select1;
        private AntdUI.Table table1;
        private AntdUI.Pagination pagination1;
        private AntdUI.Button button3;
        private AntdUI.Button button4;
    }
}