namespace Diplom
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPackets = new System.Windows.Forms.TabPage();
            this.listViewUdpListeners = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabNetworkActivity = new System.Windows.Forms.TabPage();
            this.chkShowListeningPorts = new System.Windows.Forms.CheckBox();
            this.listViewNetworkActivity = new System.Windows.Forms.ListView();
            this.colInterface = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSent = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colReceived = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTotal = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colProcessNetwork = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colLoad = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabNetworkTraffic = new System.Windows.Forms.TabPage();
            this.listViewNetworkTraffic = new System.Windows.Forms.ListView();
            this.colRemoteIp = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colIncomingTraffic = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colOutgoingTraffic = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTotalTraffic = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colProcessTraffic = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabTcpConnections = new System.Windows.Forms.TabPage();
            this.listViewTcpConnections = new System.Windows.Forms.ListView();
            this.colLocalAddress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colRemoteAddress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colProcess = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabTrafficGraph = new System.Windows.Forms.TabPage();
            this.formsPlotTraffic = new ScottPlot.WinForms.FormsPlot();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnSaveToFile = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabPackets.SuspendLayout();
            this.tabNetworkActivity.SuspendLayout();
            this.tabNetworkTraffic.SuspendLayout();
            this.tabTcpConnections.SuspendLayout();
            this.tabTrafficGraph.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPackets);
            this.tabControl.Controls.Add(this.tabNetworkActivity);
            this.tabControl.Controls.Add(this.tabNetworkTraffic);
            this.tabControl.Controls.Add(this.tabTcpConnections);
            this.tabControl.Controls.Add(this.tabTrafficGraph);
            this.tabControl.Location = new System.Drawing.Point(20, 60);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(740, 500);
            this.tabControl.TabIndex = 4;
            // 
            // tabPackets
            // 
            this.tabPackets.Controls.Add(this.listViewUdpListeners);
            this.tabPackets.Location = new System.Drawing.Point(4, 22);
            this.tabPackets.Name = "tabPackets";
            this.tabPackets.Padding = new System.Windows.Forms.Padding(3);
            this.tabPackets.Size = new System.Drawing.Size(732, 474);
            this.tabPackets.TabIndex = 0;
            this.tabPackets.Text = "Packets";
            this.tabPackets.UseVisualStyleBackColor = true;
            // 
            // listViewUdpListeners
            // 
            this.listViewUdpListeners.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7});
            this.listViewUdpListeners.FullRowSelect = true;
            this.listViewUdpListeners.GridLines = true;
            this.listViewUdpListeners.HideSelection = false;
            this.listViewUdpListeners.Location = new System.Drawing.Point(10, 10);
            this.listViewUdpListeners.Name = "listViewUdpListeners";
            this.listViewUdpListeners.Size = new System.Drawing.Size(700, 430);
            this.listViewUdpListeners.TabIndex = 0;
            this.listViewUdpListeners.UseCompatibleStateImageBehavior = false;
            this.listViewUdpListeners.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Source";
            this.columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Destination";
            this.columnHeader2.Width = 120;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Protocol";
            this.columnHeader3.Width = 80;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Length";
            this.columnHeader4.Width = 70;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Timestamp";
            this.columnHeader5.Width = 100;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Src Port";
            this.columnHeader6.Width = 70;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Dst Port";
            this.columnHeader7.Width = 70;
            // 
            // tabNetworkActivity
            // 
            this.tabNetworkActivity.Controls.Add(this.chkShowListeningPorts);
            this.tabNetworkActivity.Controls.Add(this.listViewNetworkActivity);
            this.tabNetworkActivity.Location = new System.Drawing.Point(4, 22);
            this.tabNetworkActivity.Name = "tabNetworkActivity";
            this.tabNetworkActivity.Size = new System.Drawing.Size(732, 474);
            this.tabNetworkActivity.TabIndex = 2;
            this.tabNetworkActivity.Text = "Network Activity";
            this.tabNetworkActivity.UseVisualStyleBackColor = true;
            // 
            // chkShowListeningPorts
            // 
            this.chkShowListeningPorts.Location = new System.Drawing.Point(10, 10);
            this.chkShowListeningPorts.Name = "chkShowListeningPorts";
            this.chkShowListeningPorts.Size = new System.Drawing.Size(200, 24);
            this.chkShowListeningPorts.TabIndex = 1;
            this.chkShowListeningPorts.Text = "Show Listening Ports Only";
            this.chkShowListeningPorts.UseVisualStyleBackColor = true;
            this.chkShowListeningPorts.CheckedChanged += new System.EventHandler(this.ChkShowListeningPorts_CheckedChanged);
            // 
            // listViewNetworkActivity
            // 
            this.listViewNetworkActivity.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colInterface,
            this.colSent,
            this.colReceived,
            this.colTotal,
            this.colProcessNetwork,
            this.colLoad});
            this.listViewNetworkActivity.FullRowSelect = true;
            this.listViewNetworkActivity.GridLines = true;
            this.listViewNetworkActivity.HideSelection = false;
            this.listViewNetworkActivity.Location = new System.Drawing.Point(10, 40);
            this.listViewNetworkActivity.Name = "listViewNetworkActivity";
            this.listViewNetworkActivity.Size = new System.Drawing.Size(700, 400);
            this.listViewNetworkActivity.TabIndex = 0;
            this.listViewNetworkActivity.UseCompatibleStateImageBehavior = false;
            this.listViewNetworkActivity.View = System.Windows.Forms.View.Details;
            this.listViewNetworkActivity.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ListViewNetworkActivity_ColumnClick);
            // 
            // colInterface
            // 
            this.colInterface.Text = "Protocol";
            this.colInterface.Width = 80;
            // 
            // colSent
            // 
            this.colSent.Text = "Local Address";
            this.colSent.Width = 150;
            // 
            // colReceived
            // 
            this.colReceived.Text = "Remote Address";
            this.colReceived.Width = 150;
            // 
            // colTotal
            // 
            this.colTotal.Text = "Type";
            this.colTotal.Width = 80;
            // 
            // colProcessNetwork
            // 
            this.colProcessNetwork.Text = "Process";
            this.colProcessNetwork.Width = 120;
            // 
            // colLoad
            // 
            this.colLoad.Text = "Load (B/s)";
            this.colLoad.Width = 100;
            // 
            // tabNetworkTraffic
            // 
            this.tabNetworkTraffic.Controls.Add(this.listViewNetworkTraffic);
            this.tabNetworkTraffic.Location = new System.Drawing.Point(4, 22);
            this.tabNetworkTraffic.Name = "tabNetworkTraffic";
            this.tabNetworkTraffic.Size = new System.Drawing.Size(732, 474);
            this.tabNetworkTraffic.TabIndex = 3;
            this.tabNetworkTraffic.Text = "Network Traffic";
            this.tabNetworkTraffic.UseVisualStyleBackColor = true;
            // 
            // listViewNetworkTraffic
            // 
            this.listViewNetworkTraffic.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colRemoteIp,
            this.colIncomingTraffic,
            this.colOutgoingTraffic,
            this.colTotalTraffic,
            this.colProcessTraffic});
            this.listViewNetworkTraffic.FullRowSelect = true;
            this.listViewNetworkTraffic.GridLines = true;
            this.listViewNetworkTraffic.HideSelection = false;
            this.listViewNetworkTraffic.Location = new System.Drawing.Point(10, 10);
            this.listViewNetworkTraffic.Name = "listViewNetworkTraffic";
            this.listViewNetworkTraffic.Size = new System.Drawing.Size(700, 430);
            this.listViewNetworkTraffic.TabIndex = 0;
            this.listViewNetworkTraffic.UseCompatibleStateImageBehavior = false;
            this.listViewNetworkTraffic.View = System.Windows.Forms.View.Details;
            this.listViewNetworkTraffic.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ListViewNetworkTraffic_ColumnClick);
            // 
            // colRemoteIp
            // 
            this.colRemoteIp.Text = "Remote IP";
            this.colRemoteIp.Width = 150;
            // 
            // colIncomingTraffic
            // 
            this.colIncomingTraffic.Text = "Avg Incoming Traffic (B/s)";
            this.colIncomingTraffic.Width = 150;
            // 
            // colOutgoingTraffic
            // 
            this.colOutgoingTraffic.Text = "Avg Outgoing Traffic (B/s)";
            this.colOutgoingTraffic.Width = 150;
            // 
            // colTotalTraffic
            // 
            this.colTotalTraffic.Text = "Avg Total Traffic (B/s)";
            this.colTotalTraffic.Width = 150;
            // 
            // colProcessTraffic
            // 
            this.colProcessTraffic.Text = "Process";
            this.colProcessTraffic.Width = 150;
            // 
            // tabTcpConnections
            // 
            this.tabTcpConnections.Controls.Add(this.listViewTcpConnections);
            this.tabTcpConnections.Location = new System.Drawing.Point(4, 22);
            this.tabTcpConnections.Name = "tabTcpConnections";
            this.tabTcpConnections.Size = new System.Drawing.Size(732, 474);
            this.tabTcpConnections.TabIndex = 4;
            this.tabTcpConnections.Text = "TCP Connections";
            this.tabTcpConnections.UseVisualStyleBackColor = true;
            // 
            // listViewTcpConnections
            // 
            this.listViewTcpConnections.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colLocalAddress,
            this.colRemoteAddress,
            this.colState,
            this.colProcess});
            this.listViewTcpConnections.FullRowSelect = true;
            this.listViewTcpConnections.GridLines = true;
            this.listViewTcpConnections.HideSelection = false;
            this.listViewTcpConnections.Location = new System.Drawing.Point(10, 10);
            this.listViewTcpConnections.Name = "listViewTcpConnections";
            this.listViewTcpConnections.Size = new System.Drawing.Size(700, 430);
            this.listViewTcpConnections.TabIndex = 0;
            this.listViewTcpConnections.UseCompatibleStateImageBehavior = false;
            this.listViewTcpConnections.View = System.Windows.Forms.View.Details;
            // 
            // colLocalAddress
            // 
            this.colLocalAddress.Text = "Local Address";
            this.colLocalAddress.Width = 150;
            // 
            // colRemoteAddress
            // 
            this.colRemoteAddress.Text = "Remote Address";
            this.colRemoteAddress.Width = 150;
            // 
            // colState
            // 
            this.colState.Text = "State";
            this.colState.Width = 100;
            // 
            // colProcess
            // 
            this.colProcess.Text = "Process";
            this.colProcess.Width = 150;
            // 
            // tabTrafficGraph
            // 
            this.tabTrafficGraph.Controls.Add(this.formsPlotTraffic);
            this.tabTrafficGraph.Location = new System.Drawing.Point(4, 22);
            this.tabTrafficGraph.Name = "tabTrafficGraph";
            this.tabTrafficGraph.Size = new System.Drawing.Size(732, 474);
            this.tabTrafficGraph.TabIndex = 5;
            this.tabTrafficGraph.Text = "Traffic Graph";
            this.tabTrafficGraph.UseVisualStyleBackColor = true;
            // 
            // formsPlotTraffic
            // 
            this.formsPlotTraffic.Location = new System.Drawing.Point(10, 10);
            this.formsPlotTraffic.Name = "formsPlotTraffic";
            this.formsPlotTraffic.Size = new System.Drawing.Size(700, 430);
            this.formsPlotTraffic.TabIndex = 0;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(20, 20);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 30);
            this.btnStart.TabIndex = 5;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.BtnStart_Click);
            // 
            // btnPause
            // 
            this.btnPause.Location = new System.Drawing.Point(105, 20);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(75, 30);
            this.btnPause.TabIndex = 6;
            this.btnPause.Text = "Pause";
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new System.EventHandler(this.BtnPause_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(190, 20);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 30);
            this.btnStop.TabIndex = 7;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.BtnStop_Click);
            // 
            // btnSaveToFile
            // 
            this.btnSaveToFile.Location = new System.Drawing.Point(275, 20);
            this.btnSaveToFile.Name = "btnSaveToFile";
            this.btnSaveToFile.Size = new System.Drawing.Size(100, 30);
            this.btnSaveToFile.TabIndex = 8;
            this.btnSaveToFile.Text = "Save to File";
            this.btnSaveToFile.UseVisualStyleBackColor = true;
            this.btnSaveToFile.Click += new System.EventHandler(this.BtnSaveToFile_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.btnSaveToFile);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnPause);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Advanced Network Monitor";
            this.tabControl.ResumeLayout(false);
            this.tabPackets.ResumeLayout(false);
            this.tabNetworkActivity.ResumeLayout(false);
            this.tabNetworkTraffic.ResumeLayout(false);
            this.tabTcpConnections.ResumeLayout(false);
            this.tabTrafficGraph.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPackets;
        private System.Windows.Forms.TabPage tabNetworkActivity;
        private System.Windows.Forms.CheckBox chkShowListeningPorts;
        private System.Windows.Forms.TabPage tabNetworkTraffic;
        private System.Windows.Forms.ListView listViewNetworkTraffic;
        private System.Windows.Forms.ColumnHeader colRemoteIp;
        private System.Windows.Forms.ColumnHeader colIncomingTraffic;
        private System.Windows.Forms.ColumnHeader colOutgoingTraffic;
        private System.Windows.Forms.ColumnHeader colTotalTraffic;
        private System.Windows.Forms.ColumnHeader colProcessTraffic;
        private System.Windows.Forms.TabPage tabTcpConnections;
        private System.Windows.Forms.ListView listViewNetworkActivity;
        private System.Windows.Forms.ColumnHeader colInterface;
        private System.Windows.Forms.ColumnHeader colSent;
        private System.Windows.Forms.ColumnHeader colReceived;
        private System.Windows.Forms.ColumnHeader colTotal;
        private System.Windows.Forms.ColumnHeader colProcessNetwork;
        private System.Windows.Forms.ColumnHeader colLoad;
        private System.Windows.Forms.ListView listViewTcpConnections;
        private System.Windows.Forms.ColumnHeader colLocalAddress;
        private System.Windows.Forms.ColumnHeader colRemoteAddress;
        private System.Windows.Forms.ColumnHeader colState;
        private System.Windows.Forms.ColumnHeader colProcess;
        private System.Windows.Forms.ListView listViewUdpListeners;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.TabPage tabTrafficGraph;
        private ScottPlot.WinForms.FormsPlot formsPlotTraffic;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnSaveToFile;
    }
}