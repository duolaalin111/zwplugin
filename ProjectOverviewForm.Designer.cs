namespace ZrxDotNetCSProject5
{
    partial class ProjectOverviewForm
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
            this.battery1 = new AntdUI.Battery();
            this.SuspendLayout();
            // 
            // battery1
            // 
            this.battery1.Location = new System.Drawing.Point(244, 107);
            this.battery1.Name = "battery1";
            this.battery1.Size = new System.Drawing.Size(75, 23);
            this.battery1.TabIndex = 0;
            this.battery1.Text = "battery1";
            // 
            // ProjectOverviewForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.battery1);
            this.Name = "ProjectOverviewForm";
            this.Load += new System.EventHandler(this.ProjectOverviewForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Battery battery1;
    }
}