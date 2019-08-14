namespace weCare
{
    partial class frmConsole
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmConsole));
            this.gcTask = new DevExpress.XtraGrid.GridControl();
            this.gvTask = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gridColumn1 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn2 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn3 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.defaultLookAndFeel = new DevExpress.LookAndFeel.DefaultLookAndFeel(this.components);
            this.btnUpload = new DevExpress.XtraEditors.SimpleButton();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.progressBarControl = new DevExpress.XtraEditors.ProgressBarControl();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.txtCard = new DevExpress.XtraEditors.TextEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.upDate = new DevExpress.XtraEditors.DateEdit();
            ((System.ComponentModel.ISupportInitialize)(this.gcTask)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvTask)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtCard.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDate.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // gcTask
            // 
            this.gcTask.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcTask.Location = new System.Drawing.Point(0, 36);
            this.gcTask.MainView = this.gvTask;
            this.gcTask.Name = "gcTask";
            this.gcTask.Size = new System.Drawing.Size(499, 639);
            this.gcTask.TabIndex = 3;
            this.gcTask.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvTask});
            // 
            // gvTask
            // 
            this.gvTask.Appearance.HeaderPanel.Font = new System.Drawing.Font("宋体", 9F);
            this.gvTask.Appearance.HeaderPanel.Options.UseFont = true;
            this.gvTask.Appearance.HeaderPanel.Options.UseTextOptions = true;
            this.gvTask.Appearance.Row.Font = new System.Drawing.Font("宋体", 9F);
            this.gvTask.Appearance.Row.Options.UseFont = true;
            this.gvTask.Appearance.ViewCaption.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold);
            this.gvTask.Appearance.ViewCaption.ForeColor = System.Drawing.Color.MediumBlue;
            this.gvTask.Appearance.ViewCaption.Options.UseFont = true;
            this.gvTask.Appearance.ViewCaption.Options.UseForeColor = true;
            this.gvTask.Appearance.ViewCaption.Options.UseTextOptions = true;
            this.gvTask.Appearance.ViewCaption.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gvTask.Appearance.ViewCaption.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.gvTask.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.gridColumn1,
            this.gridColumn2,
            this.gridColumn3});
            this.gvTask.GridControl = this.gcTask;
            this.gvTask.Name = "gvTask";
            this.gvTask.OptionsView.ColumnAutoWidth = false;
            this.gvTask.OptionsView.ShowDetailButtons = false;
            this.gvTask.OptionsView.ShowGroupExpandCollapseButtons = false;
            this.gvTask.OptionsView.ShowGroupPanel = false;
            this.gvTask.OptionsView.ShowViewCaption = true;
            this.gvTask.ViewCaption = "上传时间 2016-08-08 14:25";
            this.gvTask.ViewCaptionHeight = 20;
            // 
            // gridColumn1
            // 
            this.gridColumn1.AppearanceCell.Options.UseTextOptions = true;
            this.gridColumn1.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridColumn1.AppearanceCell.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.gridColumn1.AppearanceHeader.Options.UseTextOptions = true;
            this.gridColumn1.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridColumn1.AppearanceHeader.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.gridColumn1.Caption = "序号";
            this.gridColumn1.FieldName = "sortNo";
            this.gridColumn1.Name = "gridColumn1";
            this.gridColumn1.OptionsColumn.AllowEdit = false;
            this.gridColumn1.OptionsColumn.AllowFocus = false;
            this.gridColumn1.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.gridColumn1.OptionsFilter.AllowAutoFilter = false;
            this.gridColumn1.OptionsFilter.AllowFilter = false;
            this.gridColumn1.Visible = true;
            this.gridColumn1.VisibleIndex = 0;
            this.gridColumn1.Width = 47;
            // 
            // gridColumn2
            // 
            this.gridColumn2.AppearanceCell.Options.UseTextOptions = true;
            this.gridColumn2.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridColumn2.AppearanceCell.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.gridColumn2.AppearanceHeader.Options.UseTextOptions = true;
            this.gridColumn2.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridColumn2.AppearanceHeader.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.gridColumn2.Caption = "执行时间";
            this.gridColumn2.FieldName = "execTime";
            this.gridColumn2.Name = "gridColumn2";
            this.gridColumn2.OptionsColumn.AllowEdit = false;
            this.gridColumn2.OptionsColumn.AllowFocus = false;
            this.gridColumn2.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.gridColumn2.OptionsFilter.AllowAutoFilter = false;
            this.gridColumn2.OptionsFilter.AllowFilter = false;
            this.gridColumn2.Visible = true;
            this.gridColumn2.VisibleIndex = 1;
            this.gridColumn2.Width = 166;
            // 
            // gridColumn3
            // 
            this.gridColumn3.Caption = "    描述";
            this.gridColumn3.FieldName = "execDesc";
            this.gridColumn3.Name = "gridColumn3";
            this.gridColumn3.OptionsColumn.AllowEdit = false;
            this.gridColumn3.OptionsColumn.AllowFocus = false;
            this.gridColumn3.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.gridColumn3.OptionsFilter.AllowAutoFilter = false;
            this.gridColumn3.OptionsFilter.AllowFilter = false;
            this.gridColumn3.Visible = true;
            this.gridColumn3.VisibleIndex = 2;
            this.gridColumn3.Width = 335;
            // 
            // notifyIcon
            // 
            this.notifyIcon.BalloonTipText = "孕产妇分娩信息上传控制台";
            this.notifyIcon.BalloonTipTitle = "孕产妇分娩信息上传控制台";
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "孕产妇分娩信息上传控制台";
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseDoubleClick);
            // 
            // defaultLookAndFeel
            // 
            this.defaultLookAndFeel.LookAndFeel.SkinName = "Office 2010 Blue";
            // 
            // btnUpload
            // 
            this.btnUpload.Appearance.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnUpload.Appearance.ForeColor = System.Drawing.Color.Crimson;
            this.btnUpload.Appearance.Options.UseFont = true;
            this.btnUpload.Appearance.Options.UseForeColor = true;
            this.btnUpload.Image = ((System.Drawing.Image)(resources.GetObject("btnUpload.Image")));
            this.btnUpload.Location = new System.Drawing.Point(355, 2);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(129, 31);
            this.btnUpload.TabIndex = 4;
            this.btnUpload.Text = "立即上传 (&U)";
            this.btnUpload.Click += new System.EventHandler(this.btnDownload_Click);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // progressBarControl
            // 
            this.progressBarControl.Location = new System.Drawing.Point(4, 220);
            this.progressBarControl.Name = "progressBarControl";
            this.progressBarControl.Properties.LookAndFeel.SkinName = "Visual Studio 2013 Blue";
            this.progressBarControl.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.progressBarControl.Size = new System.Drawing.Size(412, 44);
            this.progressBarControl.TabIndex = 5;
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.txtCard);
            this.panelControl1.Controls.Add(this.labelControl2);
            this.panelControl1.Controls.Add(this.labelControl1);
            this.panelControl1.Controls.Add(this.btnUpload);
            this.panelControl1.Controls.Add(this.upDate);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(499, 36);
            this.panelControl1.TabIndex = 6;
            // 
            // txtCard
            // 
            this.txtCard.Location = new System.Drawing.Point(234, 5);
            this.txtCard.Name = "txtCard";
            this.txtCard.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCard.Properties.Appearance.Options.UseFont = true;
            this.txtCard.Size = new System.Drawing.Size(115, 24);
            this.txtCard.TabIndex = 6;
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelControl2.Location = new System.Drawing.Point(183, 12);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(45, 14);
            this.labelControl2.TabIndex = 5;
            this.labelControl2.Text = "卡号：";
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelControl1.Location = new System.Drawing.Point(4, 11);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(45, 14);
            this.labelControl1.TabIndex = 1;
            this.labelControl1.Text = "日期：";
            // 
            // upDate
            // 
            this.upDate.EditValue = null;
            this.upDate.Location = new System.Drawing.Point(49, 6);
            this.upDate.Name = "upDate";
            this.upDate.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.upDate.Properties.Appearance.Options.UseFont = true;
            this.upDate.Properties.AppearanceDropDown.Font = new System.Drawing.Font("Tahoma", 10.5F);
            this.upDate.Properties.AppearanceDropDown.Options.UseFont = true;
            this.upDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.upDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.upDate.Properties.DisplayFormat.FormatString = "yyyy-MM-dd";
            this.upDate.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.upDate.Properties.EditFormat.FormatString = "yyyy-MM-dd";
            this.upDate.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.upDate.Properties.Mask.EditMask = "yyyy-MM-dd";
            this.upDate.Size = new System.Drawing.Size(119, 24);
            this.upDate.TabIndex = 0;
            // 
            // frmConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(499, 675);
            this.Controls.Add(this.gcTask);
            this.Controls.Add(this.panelControl1);
            this.Controls.Add(this.progressBarControl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmConsole";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "孕产妇分娩信息上传控制台";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmConsole_FormClosing);
            this.Load += new System.EventHandler(this.frmConsole_Load);
            this.SizeChanged += new System.EventHandler(this.frmConsole_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.gcTask)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvTask)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.panelControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtCard.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDate.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gcTask;
        private DevExpress.XtraGrid.Views.Grid.GridView gvTask;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn1;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn2;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn3;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private DevExpress.LookAndFeel.DefaultLookAndFeel defaultLookAndFeel;
        private DevExpress.XtraEditors.SimpleButton btnUpload;
        internal System.Windows.Forms.Timer timer;
        private DevExpress.XtraEditors.ProgressBarControl progressBarControl;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.DateEdit upDate;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.TextEdit txtCard;
        private DevExpress.XtraEditors.LabelControl labelControl2;
    }
}