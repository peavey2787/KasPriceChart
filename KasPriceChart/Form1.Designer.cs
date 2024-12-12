namespace KasPriceChart
{
    partial class MainForm
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.txtInterval = new System.Windows.Forms.TextBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.lblInterval = new System.Windows.Forms.Label();
            this.lblDataLoaded = new System.Windows.Forms.Label();
            this.lblCountDown = new System.Windows.Forms.Label();
            this.lblRefreshIn = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblLastTimeStamp = new System.Windows.Forms.Label();
            this.lblCurrentPrice = new System.Windows.Forms.Label();
            this.lblCurrentHashrate = new System.Windows.Forms.Label();
            this.chkCoinCodex = new System.Windows.Forms.CheckBox();
            this.chkUseOnlyUploadedData = new System.Windows.Forms.CheckBox();
            this.chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPagePrice = new System.Windows.Forms.TabPage();
            this.tabPageHashrate = new System.Windows.Forms.TabPage();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.cmbViewTimspan = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPagePrice.SuspendLayout();
            this.tabPageHashrate.SuspendLayout();
            this.SuspendLayout();
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(3, 3);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(861, 318);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "Price";
            // 
            // txtInterval
            // 
            this.txtInterval.Location = new System.Drawing.Point(395, 40);
            this.txtInterval.Name = "txtInterval";
            this.txtInterval.Size = new System.Drawing.Size(33, 20);
            this.txtInterval.TabIndex = 1;
            this.txtInterval.Text = "5";
            this.txtInterval.TextChanged += new System.EventHandler(this.txtInterval_TextChanged);
            // 
            // btnStart
            // 
            this.btnStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStart.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnStart.Location = new System.Drawing.Point(12, 40);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 2;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnImport
            // 
            this.btnImport.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnImport.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnImport.Location = new System.Drawing.Point(672, 37);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(58, 23);
            this.btnImport.TabIndex = 4;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // btnExport
            // 
            this.btnExport.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExport.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnExport.Location = new System.Drawing.Point(813, 37);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(58, 23);
            this.btnExport.TabIndex = 5;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // lblInterval
            // 
            this.lblInterval.AutoSize = true;
            this.lblInterval.Location = new System.Drawing.Point(276, 43);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(113, 13);
            this.lblInterval.TabIndex = 6;
            this.lblInterval.Text = "Update Interval (mins):";
            // 
            // lblDataLoaded
            // 
            this.lblDataLoaded.AutoSize = true;
            this.lblDataLoaded.Location = new System.Drawing.Point(124, 78);
            this.lblDataLoaded.Name = "lblDataLoaded";
            this.lblDataLoaded.Size = new System.Drawing.Size(250, 13);
            this.lblDataLoaded.TabIndex = 7;
            this.lblDataLoaded.Text = "No data loaded, starting new dataset from right now";
            // 
            // lblCountDown
            // 
            this.lblCountDown.AutoSize = true;
            this.lblCountDown.Location = new System.Drawing.Point(205, 43);
            this.lblCountDown.Name = "lblCountDown";
            this.lblCountDown.Size = new System.Drawing.Size(28, 13);
            this.lblCountDown.TabIndex = 8;
            this.lblCountDown.Text = "5:00";
            // 
            // lblRefreshIn
            // 
            this.lblRefreshIn.AutoSize = true;
            this.lblRefreshIn.Location = new System.Drawing.Point(124, 43);
            this.lblRefreshIn.Name = "lblRefreshIn";
            this.lblRefreshIn.Size = new System.Drawing.Size(59, 13);
            this.lblRefreshIn.TabIndex = 9;
            this.lblRefreshIn.Text = "Refresh In:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cmbViewTimspan);
            this.groupBox1.Controls.Add(this.chkAutoStart);
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Controls.Add(this.chkCoinCodex);
            this.groupBox1.Controls.Add(this.chkUseOnlyUploadedData);
            this.groupBox1.Controls.Add(this.btnExport);
            this.groupBox1.Controls.Add(this.lblRefreshIn);
            this.groupBox1.Controls.Add(this.btnImport);
            this.groupBox1.Controls.Add(this.lblCountDown);
            this.groupBox1.Controls.Add(this.lblInterval);
            this.groupBox1.Controls.Add(this.lblDataLoaded);
            this.groupBox1.Controls.Add(this.txtInterval);
            this.groupBox1.Controls.Add(this.btnStart);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBox1.Location = new System.Drawing.Point(0, 350);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(875, 100);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.AutoSize = true;
            this.chkAutoStart.Location = new System.Drawing.Point(12, 77);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(79, 17);
            this.chkAutoStart.TabIndex = 13;
            this.chkAutoStart.Text = "Auto Start?";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            this.chkAutoStart.CheckedChanged += new System.EventHandler(this.chkAutoStart_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lblLastTimeStamp);
            this.groupBox2.Controls.Add(this.lblCurrentPrice);
            this.groupBox2.Controls.Add(this.lblCurrentHashrate);
            this.groupBox2.Location = new System.Drawing.Point(5, -7);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(865, 44);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            // 
            // lblLastTimeStamp
            // 
            this.lblLastTimeStamp.AutoSize = true;
            this.lblLastTimeStamp.Font = new System.Drawing.Font("Ink Free", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLastTimeStamp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(199)))), ((int)(((byte)(186)))));
            this.lblLastTimeStamp.Location = new System.Drawing.Point(507, 12);
            this.lblLastTimeStamp.Name = "lblLastTimeStamp";
            this.lblLastTimeStamp.Size = new System.Drawing.Size(119, 20);
            this.lblLastTimeStamp.TabIndex = 15;
            this.lblLastTimeStamp.Text = "Last Updated:";
            // 
            // lblCurrentPrice
            // 
            this.lblCurrentPrice.AutoSize = true;
            this.lblCurrentPrice.Font = new System.Drawing.Font("Ink Free", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentPrice.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(199)))), ((int)(((byte)(186)))));
            this.lblCurrentPrice.Location = new System.Drawing.Point(18, 12);
            this.lblCurrentPrice.Name = "lblCurrentPrice";
            this.lblCurrentPrice.Size = new System.Drawing.Size(116, 20);
            this.lblCurrentPrice.TabIndex = 13;
            this.lblCurrentPrice.Text = "Current Price:";
            // 
            // lblCurrentHashrate
            // 
            this.lblCurrentHashrate.AutoSize = true;
            this.lblCurrentHashrate.Font = new System.Drawing.Font("Ink Free", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentHashrate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(199)))), ((int)(((byte)(186)))));
            this.lblCurrentHashrate.Location = new System.Drawing.Point(258, 12);
            this.lblCurrentHashrate.Name = "lblCurrentHashrate";
            this.lblCurrentHashrate.Size = new System.Drawing.Size(152, 20);
            this.lblCurrentHashrate.TabIndex = 14;
            this.lblCurrentHashrate.Text = "Current Hashrate:";
            // 
            // chkCoinCodex
            // 
            this.chkCoinCodex.AutoSize = true;
            this.chkCoinCodex.Location = new System.Drawing.Point(672, 62);
            this.chkCoinCodex.Name = "chkCoinCodex";
            this.chkCoinCodex.Size = new System.Drawing.Size(83, 17);
            this.chkCoinCodex.TabIndex = 11;
            this.chkCoinCodex.Text = "CoinCodex?";
            this.chkCoinCodex.UseVisualStyleBackColor = true;
            this.chkCoinCodex.CheckedChanged += new System.EventHandler(this.chkCoinCodex_CheckedChanged);
            // 
            // chkUseOnlyUploadedData
            // 
            this.chkUseOnlyUploadedData.AutoSize = true;
            this.chkUseOnlyUploadedData.Location = new System.Drawing.Point(672, 80);
            this.chkUseOnlyUploadedData.Name = "chkUseOnlyUploadedData";
            this.chkUseOnlyUploadedData.Size = new System.Drawing.Size(144, 17);
            this.chkUseOnlyUploadedData.TabIndex = 10;
            this.chkUseOnlyUploadedData.Text = "Use only uploaded data?";
            this.chkUseOnlyUploadedData.UseVisualStyleBackColor = true;
            this.chkUseOnlyUploadedData.CheckedChanged += new System.EventHandler(this.chkUseOnlyUploadedData_CheckedChanged);
            // 
            // chart2
            // 
            chartArea2.Name = "ChartArea1";
            this.chart2.ChartAreas.Add(chartArea2);
            this.chart2.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.Name = "Legend1";
            this.chart2.Legends.Add(legend2);
            this.chart2.Location = new System.Drawing.Point(3, 3);
            this.chart2.Name = "chart2";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.chart2.Series.Add(series2);
            this.chart2.Size = new System.Drawing.Size(861, 318);
            this.chart2.TabIndex = 11;
            this.chart2.Text = "Hashrate";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPagePrice);
            this.tabControl1.Controls.Add(this.tabPageHashrate);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(875, 350);
            this.tabControl1.TabIndex = 12;
            // 
            // tabPagePrice
            // 
            this.tabPagePrice.Controls.Add(this.chart1);
            this.tabPagePrice.Location = new System.Drawing.Point(4, 22);
            this.tabPagePrice.Name = "tabPagePrice";
            this.tabPagePrice.Padding = new System.Windows.Forms.Padding(3);
            this.tabPagePrice.Size = new System.Drawing.Size(867, 324);
            this.tabPagePrice.TabIndex = 0;
            this.tabPagePrice.Text = "Price";
            this.tabPagePrice.UseVisualStyleBackColor = true;
            // 
            // tabPageHashrate
            // 
            this.tabPageHashrate.Controls.Add(this.chart2);
            this.tabPageHashrate.Location = new System.Drawing.Point(4, 22);
            this.tabPageHashrate.Name = "tabPageHashrate";
            this.tabPageHashrate.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHashrate.Size = new System.Drawing.Size(867, 324);
            this.tabPageHashrate.TabIndex = 1;
            this.tabPageHashrate.Text = "Hashrate";
            this.tabPageHashrate.UseVisualStyleBackColor = true;
            // 
            // cmbViewTimspan
            // 
            this.cmbViewTimspan.FormattingEnabled = true;
            this.cmbViewTimspan.Items.AddRange(new object[] {
            "All Data",
            "5 Minutes",
            "15 Minutes",
            "30 Minutes",
            "1 Hour",
            "4 Hour",
            "12 Hour",
            "1 Day",
            "1 Week",
            "2 Weeks",
            "1 Month",
            "1 Year"});
            this.cmbViewTimspan.Location = new System.Drawing.Point(489, 43);
            this.cmbViewTimspan.Name = "cmbViewTimspan";
            this.cmbViewTimspan.Size = new System.Drawing.Size(121, 21);
            this.cmbViewTimspan.TabIndex = 14;
            this.cmbViewTimspan.Text = "All Data";
            this.cmbViewTimspan.SelectedIndexChanged += new System.EventHandler(this.cmbViewTimspan_SelectedIndexChanged);
            this.cmbViewTimspan.TextUpdate += new System.EventHandler(this.cmbViewTimspan_TextUpdate);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(31)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(875, 450);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.groupBox1);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(199)))), ((int)(((byte)(186)))));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Kaspa Chart";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPagePrice.ResumeLayout(false);
            this.tabPageHashrate.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.TextBox txtInterval;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Label lblInterval;
        private System.Windows.Forms.Label lblDataLoaded;
        private System.Windows.Forms.Label lblCountDown;
        private System.Windows.Forms.Label lblRefreshIn;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkUseOnlyUploadedData;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPagePrice;
        private System.Windows.Forms.TabPage tabPageHashrate;
        private System.Windows.Forms.Label lblCurrentPrice;
        private System.Windows.Forms.Label lblCurrentHashrate;
        private System.Windows.Forms.Label lblLastTimeStamp;
        private System.Windows.Forms.CheckBox chkCoinCodex;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.ComboBox cmbViewTimspan;
    }
}

