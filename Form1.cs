using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Collections;
using System.Threading.Tasks;
using ScottPlot;
using System.Data.SQLite;

namespace Diplom
{
    public partial class Form1 : Form
    {
        private readonly System.Windows.Forms.Timer updateTimer;
        private bool showListeningPortsOnly = false;
        private (long BytesSent, long BytesReceived) previousTotalTraffic = (0, 0);
        private Dictionary<string, (string Protocol, string LocalAddress, string RemoteAddress, string Type, int Pid, string ProcessName, string State, double LoadBs)> networkActivityCache = new Dictionary<string, (string, string, string, string, int, string, string, double)>();
        private Dictionary<int, string> processNameCache = new Dictionary<int, string>();
        private SortOrder networkActivitySortOrder = SortOrder.None;
        private int networkActivitySortColumn = -1;
        private SortOrder networkTrafficSortOrder = SortOrder.None;
        private int networkTrafficSortColumn = -1;
        private ICaptureDevice captureDevice;
        private bool isCapturing = false;
        private readonly Queue<ListViewItem> packetQueue = new Queue<ListViewItem>();
        private readonly System.Windows.Forms.Timer packetUpdateTimer;
        private readonly Dictionary<int, Queue<(long SentBytes, long ReceivedBytes, DateTime Timestamp)>> processTrafficHistory = new Dictionary<int, Queue<(long, long, DateTime)>>();
        private const int MinuteInSeconds = 60;
        private readonly System.Windows.Forms.Timer chartUpdateTimer;
        private readonly List<double> sentTrafficValues = new List<double>();
        private readonly List<double> receivedTrafficValues = new List<double>();
        private readonly List<DateTime> timeStamps = new List<DateTime>();
        private const int ChartMaxPoints = 60;
        private const double TrafficThresholdMBps = 1.0 * 1024 * 1024; // Порог 1 МБ/с
        private const int PacketLengthThreshold = 1500; // Порог длины пакета

        private SQLiteConnection dbConnection;

        public Form1()
        {
            InitializeComponent();
            ApplyVisualStyles();
            listViewNetworkActivity.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(listViewNetworkActivity, true);
            listViewNetworkTraffic.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(listViewNetworkTraffic, true);
            listViewTcpConnections.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(listViewTcpConnections, true);
            listViewUdpListeners.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(listViewUdpListeners, true);
            listViewSuspicious.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(listViewSuspicious, true);

            updateTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            updateTimer.Tick += async (s, args) => await UpdateNetworkDataAsync();
            packetUpdateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            packetUpdateTimer.Tick += PacketUpdateTimer_Tick;
            chartUpdateTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            chartUpdateTimer.Tick += ChartUpdateTimer_Tick;
            InitializePacketCapture();
            InitializeChart();
            InitializeDatabase();

            listViewNetworkActivity.Columns[5].Text = "Load (B/s)";
            listViewNetworkTraffic.Columns[1].Text = "Avg Incoming Traffic (B/s)";
            listViewNetworkTraffic.Columns[2].Text = "Avg Outgoing Traffic (B/s)";
            listViewNetworkTraffic.Columns[3].Text = "Avg Total Traffic (B/s)";
        }

        private void InitializeDatabase()
        {
            try
            {
                dbConnection = new SQLiteConnection("Data Source=network_monitor.db;Version=3;");
                dbConnection.Open();

                string createSuspiciousPacketsTable = @"
                    CREATE TABLE IF NOT EXISTS SuspiciousPackets (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Source TEXT NOT NULL,
                        Destination TEXT NOT NULL,
                        Protocol TEXT NOT NULL,
                        Length INTEGER NOT NULL,
                        Timestamp TEXT NOT NULL,
                        SourcePort TEXT,
                        DestPort TEXT,
                        Reason TEXT NOT NULL
                    )";
                string createSuspiciousProcessesTable = @"
                    CREATE TABLE IF NOT EXISTS SuspiciousProcesses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Pid INTEGER NOT NULL,
                        ProcessName TEXT NOT NULL,
                        AvgTrafficBs REAL NOT NULL,
                        Timestamp TEXT NOT NULL,
                        Reason TEXT NOT NULL
                    )";

                using (var cmd = new SQLiteCommand(createSuspiciousPacketsTable, dbConnection))
                { cmd.ExecuteNonQuery(); }
                using (var cmd = new SQLiteCommand(createSuspiciousProcessesTable, dbConnection))
                { cmd.ExecuteNonQuery(); }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyVisualStyles()
        {
            BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            ForeColor = System.Drawing.Color.White;
            tabControl.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            tabControl.ForeColor = System.Drawing.Color.White;

            tabControl.SelectedTab.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            foreach (TabPage tab in tabControl.TabPages)
            {
                tab.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            }

            listViewUdpListeners.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            listViewUdpListeners.ForeColor = System.Drawing.Color.White;
            listViewNetworkActivity.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            listViewNetworkActivity.ForeColor = System.Drawing.Color.White;
            listViewNetworkTraffic.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            listViewNetworkTraffic.ForeColor = System.Drawing.Color.White;
            listViewTcpConnections.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            listViewTcpConnections.ForeColor = System.Drawing.Color.White;
            listViewSuspicious.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            listViewSuspicious.ForeColor = System.Drawing.Color.White;

            listViewUdpListeners.DrawItem += (s, e) =>
            {
                e.DrawDefault = true;
                if ((e.ItemIndex % 2) == 0)
                    e.Item.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
                else
                    e.Item.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            };
            listViewNetworkActivity.DrawItem += (s, e) =>
            {
                e.DrawDefault = true;
                if ((e.ItemIndex % 2) == 0)
                    e.Item.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
                else
                    e.Item.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            };
            listViewNetworkTraffic.DrawItem += (s, e) =>
            {
                e.DrawDefault = true;
                if ((e.ItemIndex % 2) == 0)
                    e.Item.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
                else
                    e.Item.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            };
            listViewTcpConnections.DrawItem += (s, e) =>
            {
                e.DrawDefault = true;
                if ((e.ItemIndex % 2) == 0)
                    e.Item.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
                else
                    e.Item.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            };
            listViewSuspicious.DrawItem += (s, e) =>
            {
                e.DrawDefault = true;
                if ((e.ItemIndex % 2) == 0)
                    e.Item.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
                else
                    e.Item.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            };

            btnStart.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnStart.ForeColor = System.Drawing.Color.White;
            btnPause.BackColor = System.Drawing.Color.FromArgb(255, 165, 0);
            btnPause.ForeColor = System.Drawing.Color.White;
            btnStop.BackColor = System.Drawing.Color.FromArgb(220, 53, 69);
            btnStop.ForeColor = System.Drawing.Color.White;
            btnSaveToFile.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            btnSaveToFile.ForeColor = System.Drawing.Color.White;
            btnRefreshSuspicious.BackColor = System.Drawing.Color.FromArgb(0, 150, 136);
            btnRefreshSuspicious.ForeColor = System.Drawing.Color.White;
            btnClearDatabase.BackColor = System.Drawing.Color.FromArgb(153, 50, 204);
            btnClearDatabase.ForeColor = System.Drawing.Color.White;

            btnStart.MouseEnter += (s, e) => btnStart.BackColor = System.Drawing.Color.FromArgb(0, 100, 180);
            btnStart.MouseLeave += (s, e) => btnStart.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnPause.MouseEnter += (s, e) => btnPause.BackColor = System.Drawing.Color.FromArgb(200, 130, 0);
            btnPause.MouseLeave += (s, e) => btnPause.BackColor = System.Drawing.Color.FromArgb(255, 165, 0);
            btnStop.MouseEnter += (s, e) => btnStop.BackColor = System.Drawing.Color.FromArgb(180, 40, 50);
            btnStop.MouseLeave += (s, e) => btnStop.BackColor = System.Drawing.Color.FromArgb(220, 53, 69);
            btnSaveToFile.MouseEnter += (s, e) => btnSaveToFile.BackColor = System.Drawing.Color.FromArgb(30, 140, 60);
            btnSaveToFile.MouseLeave += (s, e) => btnSaveToFile.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            btnRefreshSuspicious.MouseEnter += (s, e) => btnRefreshSuspicious.BackColor = System.Drawing.Color.FromArgb(0, 120, 110);
            btnRefreshSuspicious.MouseLeave += (s, e) => btnRefreshSuspicious.BackColor = System.Drawing.Color.FromArgb(0, 150, 136);
            btnClearDatabase.MouseEnter += (s, e) => btnClearDatabase.BackColor = System.Drawing.Color.FromArgb(123, 40, 164);
            btnClearDatabase.MouseLeave += (s, e) => btnClearDatabase.BackColor = System.Drawing.Color.FromArgb(153, 50, 204);

            Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            chkShowListeningPorts.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnStart.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnPause.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnStop.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnSaveToFile.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnRefreshSuspicious.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnClearDatabase.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        }

        private void InitializeChart()
        {
            formsPlotTraffic.Plot.Title("Network Traffic");
            formsPlotTraffic.Plot.XLabel("Time");
            formsPlotTraffic.Plot.YLabel("Traffic (B/s)");
            formsPlotTraffic.Plot.Legend.IsVisible = true;
            formsPlotTraffic.Plot.Legend.Location = Alignment.UpperRight;
            formsPlotTraffic.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
            formsPlotTraffic.Refresh();
        }

        private void ChartUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateTrafficGraph();
        }

        private void UpdateTrafficGraph()
        {
            try
            {
                double totalSentBs = 0, totalReceivedBs = 0;
                lock (processTrafficHistory)
                {
                    foreach (var pid in processTrafficHistory.Keys.ToList())
                    {
                        var queue = processTrafficHistory[pid];
                        while (queue.Count > 0 && (DateTime.Now - queue.Peek().Timestamp).TotalSeconds > MinuteInSeconds)
                        {
                            queue.Dequeue();
                        }

                        if (queue.Count > 0)
                        {
                            long totalSent = queue.Sum(x => x.SentBytes);
                            long totalReceived = queue.Sum(x => x.ReceivedBytes);
                            double totalTime = (DateTime.Now - queue.Min(x => x.Timestamp)).TotalSeconds;
                            totalSentBs += totalTime > 0 ? totalSent / totalTime : 0;
                            totalReceivedBs += totalTime > 0 ? totalReceived / totalTime : 0;
                        }
                    }
                }

                timeStamps.Add(DateTime.Now);
                sentTrafficValues.Add(totalSentBs);
                receivedTrafficValues.Add(totalReceivedBs);

                while (timeStamps.Count > ChartMaxPoints)
                {
                    timeStamps.RemoveAt(0);
                    sentTrafficValues.RemoveAt(0);
                    receivedTrafficValues.RemoveAt(0);
                }

                formsPlotTraffic.Plot.Clear();
                var timeValues = timeStamps.Select(t => t.ToOADate()).ToArray();
                if (timeValues.Length == 0 || sentTrafficValues.Count == 0 || receivedTrafficValues.Count == 0)
                {
                    formsPlotTraffic.Refresh();
                    return;
                }

                var sentScatter = formsPlotTraffic.Plot.Add.Scatter(timeValues, sentTrafficValues.ToArray());
                sentScatter.Color = ScottPlot.Colors.Blue;
                sentScatter.Label = "Sent Traffic";
                var receivedScatter = formsPlotTraffic.Plot.Add.Scatter(timeValues, receivedTrafficValues.ToArray());
                receivedScatter.Color = ScottPlot.Colors.Red;
                receivedScatter.Label = "Received Traffic";

                double maxTraffic = Math.Max(sentTrafficValues.DefaultIfEmpty(0).Max(), receivedTrafficValues.DefaultIfEmpty(0).Max());
                formsPlotTraffic.Plot.Axes.SetLimitsY(0, maxTraffic > 0 ? maxTraffic * 1.1 : 1);
                if (timeValues.Length > 1)
                {
                    formsPlotTraffic.Plot.Axes.SetLimitsX(timeValues.First(), timeValues.Last());
                }

                formsPlotTraffic.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating traffic graph: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializePacketCapture()
        {
            try
            {
                var devices = LibPcapLiveDeviceList.Instance;
                if (devices.Count == 0)
                {
                    MessageBox.Show("No network devices found. Please ensure Npcap or WinPcap is installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                captureDevice = devices.FirstOrDefault(d => ((LibPcapLiveDevice)d).Addresses.Any(a => a.Addr?.ipAddress != null));
                if (captureDevice == null)
                {
                    MessageBox.Show("No suitable network device found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                captureDevice.OnPacketArrival += new PacketArrivalEventHandler(CaptureDevice_OnPacketArrivalAsync);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing packet capture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CaptureDevice_OnPacketArrivalAsync(object sender, PacketCapture e)
        {
            if (!isCapturing) return;

            var rawPacket = e.GetPacket();
            if (rawPacket == null) return;

            Task.Run(() =>
            {
                try
                {
                    var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

                    string source = "Unknown";
                    string destination = "Unknown";
                    string protocol = "Unknown";
                    string sourcePort = "N/A";
                    string destPort = "N/A";
                    int length = rawPacket.Data.Length;
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

                    var ipPacket = packet.Extract<IPPacket>();
                    if (ipPacket != null)
                    {
                        source = ipPacket.SourceAddress.ToString();
                        destination = ipPacket.DestinationAddress.ToString();
                        protocol = ipPacket.Protocol.ToString().ToUpper();

                        var tcpPacket = packet.Extract<TcpPacket>();
                        if (tcpPacket != null)
                        {
                            protocol = "TCP";
                            sourcePort = tcpPacket.SourcePort.ToString();
                            destPort = tcpPacket.DestinationPort.ToString();
                            if (tcpPacket.SourcePort == 80 || tcpPacket.DestinationPort == 80)
                            {
                                protocol = "HTTP";
                            }

                            var connections = TcpTableHelper.GetTcpConnections();
                            var matchingConn = connections.FirstOrDefault(c =>
                                (c.LocalAddress == source && c.LocalPort == tcpPacket.SourcePort) ||
                                (c.RemoteAddress == destination && c.RemotePort == tcpPacket.DestinationPort));

                            if (matchingConn.Pid != 0)
                            {
                                int pid = matchingConn.Pid;
                                lock (processTrafficHistory)
                                {
                                    if (!processTrafficHistory.ContainsKey(pid))
                                        processTrafficHistory[pid] = new Queue<(long, long, DateTime)>();

                                    var queue = processTrafficHistory[pid];
                                    if (queue.Count == 0 || queue.Last().Timestamp < DateTime.Now.AddSeconds(-5))
                                    {
                                        queue.Enqueue((0, 0, DateTime.Now));
                                    }

                                    var (sent, received, _) = queue.Last();
                                    if (source == matchingConn.LocalAddress)
                                        sent += length;
                                    else
                                        received += length;
                                    queue.Dequeue();
                                    queue.Enqueue((sent, received, DateTime.Now));
                                }
                            }
                        }
                        else if (ipPacket.Protocol == PacketDotNet.ProtocolType.Udp)
                        {
                            protocol = "UDP";
                            var udpPacket = packet.Extract<UdpPacket>();
                            if (udpPacket != null)
                            {
                                sourcePort = udpPacket.SourcePort.ToString();
                                destPort = udpPacket.DestinationPort.ToString();
                            }
                        }
                        else if (ipPacket.Protocol == PacketDotNet.ProtocolType.Icmp)
                        {
                            protocol = "ICMP";
                        }
                    }

                    string reason = "";
                    bool isSuspicious = false;
                    if (length > PacketLengthThreshold)
                    {
                        reason = $"Length exceeds {PacketLengthThreshold} bytes";
                        isSuspicious = true;
                    }
                    else if (protocol == "Unknown")
                    {
                        reason = "Unknown protocol";
                        isSuspicious = true;
                    }

                    if (isSuspicious)
                    {
                        SaveSuspiciousPacket(source, destination, protocol, length, timestamp, sourcePort, destPort, reason);
                    }

                    bool isNormalPacket = length <= 1500 && (protocol == "TCP" || protocol == "UDP" || protocol == "HTTP" || protocol == "ICMP");
                    System.Drawing.Color itemColor = isNormalPacket ? System.Drawing.Color.Green : System.Drawing.Color.Red;

                    lock (packetQueue)
                    {
                        packetQueue.Enqueue(new ListViewItem(new[] {
                            source,
                            destination,
                            protocol,
                            length.ToString(),
                            timestamp,
                            sourcePort,
                            destPort
                        })
                        { ForeColor = itemColor });
                        if (packetQueue.Count > 1000)
                        {
                            packetQueue.Dequeue();
                        }
                    }
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"Error processing packet: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        private void SaveSuspiciousPacket(string source, string destination, string protocol, int length, string timestamp, string sourcePort, string destPort, string reason)
        {
            try
            {
                using (var cmd = new SQLiteCommand("INSERT INTO SuspiciousPackets (Source, Destination, Protocol, Length, Timestamp, SourcePort, DestPort, Reason) VALUES (@source, @dest, @protocol, @length, @timestamp, @srcPort, @dstPort, @reason)", dbConnection))
                {
                    cmd.Parameters.AddWithValue("@source", source);
                    cmd.Parameters.AddWithValue("@dest", destination);
                    cmd.Parameters.AddWithValue("@protocol", protocol);
                    cmd.Parameters.AddWithValue("@length", length);
                    cmd.Parameters.AddWithValue("@timestamp", timestamp);
                    cmd.Parameters.AddWithValue("@srcPort", sourcePort);
                    cmd.Parameters.AddWithValue("@dstPort", destPort);
                    cmd.Parameters.AddWithValue("@reason", reason);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveSuspiciousPacket] Error: {ex.Message}");
            }
        }

        private void PacketUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (listViewUdpListeners.InvokeRequired)
            {
                listViewUdpListeners.Invoke(new MethodInvoker(() => PacketUpdateTimer_Tick(sender, e)));
                return;
            }

            lock (packetQueue)
            {
                while (packetQueue.Count > 0)
                {
                    listViewUdpListeners.Items.Add(packetQueue.Dequeue());
                }

                while (listViewUdpListeners.Items.Count > 1000)
                {
                    listViewUdpListeners.Items.RemoveAt(0);
                }
            }
        }

        private async Task UpdateNetworkDataAsync()
        {
            await Task.Run(async () =>
            {
                UpdateNetworkActivity();
                UpdateNetworkTraffic();
                await UpdateTcpConnections();
            });
        }

        private void UpdateNetworkActivity()
        {
            try
            {
                long totalBytesSent = 0, totalBytesReceived = 0;
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var nic in interfaces)
                {
                    var stats = nic.GetIPStatistics();
                    totalBytesSent += stats.BytesSent;
                    totalBytesReceived += stats.BytesReceived;
                }

                double sentBs = (totalBytesSent - previousTotalTraffic.BytesSent) / 5.0;
                double receivedBs = (totalBytesReceived - previousTotalTraffic.BytesReceived) / 5.0;

                var tcpConnections = TcpTableHelper.GetTcpConnections();
                var udpConnections = UdpTableHelper.GetUdpConnections();
                var allConnections = new List<(string Protocol, string LocalAddress, string RemoteAddress, string Type, int Pid, string ProcessName, string State)>();

                foreach (var conn in tcpConnections)
                {
                    string connectionType = conn.State == "Listening" ? "Listening" : "Incoming";
                    if (conn.State != "Listening" && conn.RemoteAddress != "0.0.0.0" && conn.RemoteAddress != "127.0.0.1" && !IPAddress.IsLoopback(IPAddress.Parse(conn.RemoteAddress)))
                    {
                        connectionType = "Outgoing";
                    }

                    string protocol = "TCP";
                    if (conn.LocalPort == 80 || conn.RemotePort == 80)
                    {
                        protocol = "HTTP";
                    }

                    string processName = "Unknown";
                    if (processNameCache.ContainsKey(conn.Pid))
                    {
                        processName = processNameCache[conn.Pid];
                    }
                    else
                    {
                        try
                        {
                            processName = Process.GetProcessById(conn.Pid)?.ProcessName ?? "Unknown";
                            processNameCache[conn.Pid] = processName;
                        }
                        catch { }
                    }

                    allConnections.Add((protocol, $"{conn.LocalAddress}:{conn.LocalPort}", $"{conn.RemoteAddress}:{conn.RemotePort}", connectionType, conn.Pid, processName, conn.State));
                }

                foreach (var conn in udpConnections)
                {
                    string processName = "Unknown";
                    if (processNameCache.ContainsKey(conn.Pid))
                    {
                        processName = processNameCache[conn.Pid];
                    }
                    else
                    {
                        try
                        {
                            processName = Process.GetProcessById(conn.Pid)?.ProcessName ?? "Unknown";
                            processNameCache[conn.Pid] = processName;
                        }
                        catch { }
                    }

                    allConnections.Add(("UDP", $"{conn.LocalAddress}:{conn.LocalPort}", "N/A", "Listening", conn.Pid, processName, "Listening"));
                }

                var filteredConnections = showListeningPortsOnly
                    ? allConnections.Where(c => c.Type == "Listening").ToList()
                    : allConnections;

                var processTrafficData = new Dictionary<int, (double SentBs, double ReceivedBs)>();
                lock (processTrafficHistory)
                {
                    foreach (var pid in processTrafficHistory.Keys.ToList())
                    {
                        var queue = processTrafficHistory[pid];
                        while (queue.Count > 0 && (DateTime.Now - queue.Peek().Timestamp).TotalSeconds > MinuteInSeconds)
                        {
                            queue.Dequeue();
                        }

                        if (queue.Count > 0)
                        {
                            long totalSent = queue.Sum(x => x.SentBytes);
                            long totalReceived = queue.Sum(x => x.ReceivedBytes);
                            double totalTime = Math.Min(MinuteInSeconds, (DateTime.Now - queue.Min(x => x.Timestamp)).TotalSeconds);
                            double avgSentBs = totalTime > 0 ? totalSent / totalTime : 0;
                            double avgReceivedBs = totalTime > 0 ? totalReceived / totalTime : 0;
                            processTrafficData[pid] = (avgSentBs, avgReceivedBs);

                            double totalTrafficBs = avgSentBs + avgReceivedBs;
                            if (totalTrafficBs > TrafficThresholdMBps)
                            {
                                SaveSuspiciousProcess(pid, processNameCache.ContainsKey(pid) ? processNameCache[pid] : "Unknown", totalTrafficBs, DateTime.Now.ToString("HH:mm:ss"), $"Traffic exceeds {TrafficThresholdMBps / (1024 * 1024)} MB/s");
                            }
                        }
                    }
                }

                var pidGroups = filteredConnections.GroupBy(c => c.Pid);
                foreach (var group in pidGroups)
                {
                    int pid = group.Key;
                    int connectionCount = group.Count();

                    double processSentBs = processTrafficData.ContainsKey(pid) ? processTrafficData[pid].SentBs : 0;
                    double processReceivedBs = processTrafficData.ContainsKey(pid) ? processTrafficData[pid].ReceivedBs : 0;

                    processTrafficData[pid] = (processSentBs, processReceivedBs);
                }

                var newNetworkActivityCache = new Dictionary<string, (string Protocol, string LocalAddress, string RemoteAddress, string Type, int Pid, string ProcessName, string State, double LoadBs)>();

                foreach (var conn in filteredConnections)
                {
                    double loadBs = 0;
                    if (processTrafficData.ContainsKey(conn.Pid))
                    {
                        var traffic = processTrafficData[conn.Pid];
                        loadBs = traffic.SentBs + traffic.ReceivedBs;
                    }

                    string key = $"{conn.Protocol}|{conn.LocalAddress}|{conn.RemoteAddress}|{conn.Pid}";
                    newNetworkActivityCache[key] = (conn.Protocol, conn.LocalAddress, conn.RemoteAddress, conn.Type, conn.Pid, conn.ProcessName, conn.State, loadBs);
                }

                listViewNetworkActivity.BeginUpdate();
                foreach (ListViewItem item in listViewNetworkActivity.Items.Cast<ListViewItem>().ToList())
                {
                    string key = $"{item.SubItems[0].Text}|{item.SubItems[1].Text}|{item.SubItems[2].Text}|{item.Tag}";
                    if (!newNetworkActivityCache.ContainsKey(key))
                    {
                        listViewNetworkActivity.Items.Remove(item);
                    }
                }

                foreach (var entry in newNetworkActivityCache)
                {
                    string key = entry.Key;
                    var conn = entry.Value;

                    ListViewItem existingItem = null;
                    foreach (ListViewItem item in listViewNetworkActivity.Items)
                    {
                        string itemKey = $"{item.SubItems[0].Text}|{item.SubItems[1].Text}|{item.SubItems[2].Text}|{item.Tag}";
                        if (itemKey == key)
                        {
                            existingItem = item;
                            break;
                        }
                    }

                    if (existingItem != null)
                    {
                        existingItem.SubItems[5].Text = conn.LoadBs.ToString("F0");
                    }
                    else
                    {
                        var item = new ListViewItem(new[]
                        {
                            conn.Protocol,
                            conn.LocalAddress,
                            conn.RemoteAddress,
                            conn.Type,
                            conn.ProcessName,
                            conn.LoadBs.ToString("F0")
                        })
                        {
                            Tag = conn.Pid
                        };
                        listViewNetworkActivity.Items.Add(item);
                    }
                }
                SortNetworkActivity();
                listViewNetworkActivity.EndUpdate();

                networkActivityCache = newNetworkActivityCache;
                previousTotalTraffic = (totalBytesSent, totalBytesReceived);
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Error updating network activity: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private void SaveSuspiciousProcess(int pid, string processName, double totalTrafficBs, string timestamp, string reason)
        {
            try
            {
                using (var cmd = new SQLiteCommand("INSERT INTO SuspiciousProcesses (Pid, ProcessName, AvgTrafficBs, Timestamp, Reason) VALUES (@pid, @processName, @traffic, @timestamp, @reason)", dbConnection))
                {
                    cmd.Parameters.AddWithValue("@pid", pid);
                    cmd.Parameters.AddWithValue("@processName", processName);
                    cmd.Parameters.AddWithValue("@traffic", totalTrafficBs);
                    cmd.Parameters.AddWithValue("@timestamp", timestamp);
                    cmd.Parameters.AddWithValue("@reason", reason);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveSuspiciousProcess] Error: {ex.Message}");
            }
        }

        private void UpdateNetworkTraffic()
        {
            try
            {
                listViewNetworkTraffic.Items.Clear();
                var tcpConnections = TcpTableHelper.GetTcpConnections();
                var allConnections = new List<(string RemoteIp, string Type, int Pid, string ProcessName, string Protocol)>();

                foreach (var conn in tcpConnections)
                {
                    if (conn.State == "Listening" || conn.RemoteAddress == "0.0.0.0" || conn.RemoteAddress == "127.0.0.1" || IPAddress.IsLoopback(IPAddress.Parse(conn.RemoteAddress)))
                    {
                        continue;
                    }

                    string connectionType = "Incoming";
                    if (conn.RemoteAddress != "0.0.0.0" && conn.RemoteAddress != "127.0.0.1" && !IPAddress.IsLoopback(IPAddress.Parse(conn.RemoteAddress)))
                    {
                        connectionType = "Outgoing";
                    }

                    string protocol = "TCP";
                    if (conn.LocalPort == 80 || conn.RemotePort == 80)
                    {
                        protocol = "HTTP";
                    }

                    string processName = "Unknown";
                    if (processNameCache.ContainsKey(conn.Pid))
                    {
                        processName = processNameCache[conn.Pid];
                    }
                    else
                    {
                        try
                        {
                            processName = Process.GetProcessById(conn.Pid)?.ProcessName ?? "Unknown";
                            processNameCache[conn.Pid] = processName;
                        }
                        catch { }
                    }

                    allConnections.Add((conn.RemoteAddress, connectionType, conn.Pid, processName, protocol));
                }

                var pidGroups = allConnections
                    .GroupBy(c => c.Pid)
                    .Select(g => new
                    {
                        Pid = g.Key,
                        ProcessName = g.First().ProcessName,
                        RemoteIps = g.Select(c => c.RemoteIp).Distinct().ToList(),
                        Protocols = g.Select(c => c.Protocol).Distinct().ToList()
                    })
                    .ToList();

                var processTrafficData = new Dictionary<int, (double AvgSentBs, double AvgReceivedBs)>();
                lock (processTrafficHistory)
                {
                    foreach (var pid in processTrafficHistory.Keys.ToList())
                    {
                        var queue = processTrafficHistory[pid];
                        while (queue.Count > 0 && (DateTime.Now - queue.Peek().Timestamp).TotalSeconds > MinuteInSeconds)
                        {
                            queue.Dequeue();
                        }

                        if (queue.Count > 0)
                        {
                            long totalSent = queue.Sum(x => x.SentBytes);
                            long totalReceived = queue.Sum(x => x.ReceivedBytes);
                            double totalTime = Math.Min(MinuteInSeconds, (DateTime.Now - queue.Min(x => x.Timestamp)).TotalSeconds);
                            double avgSentBs = totalTime > 0 ? totalSent / totalTime : 0;
                            double avgReceivedBs = totalTime > 0 ? totalReceived / totalTime : 0;
                            processTrafficData[pid] = (avgSentBs, avgReceivedBs);
                        }
                    }
                }

                foreach (var group in pidGroups)
                {
                    if (processTrafficData.ContainsKey(group.Pid))
                    {
                        var (avgSentBs, avgReceivedBs) = processTrafficData[group.Pid];
                        double avgTotalBs = avgSentBs + avgReceivedBs;

                        if (avgTotalBs > 0)
                        {
                            string remoteIps = string.Join(", ", group.RemoteIps);
                            string protocols = string.Join(", ", group.Protocols);

                            var item = new ListViewItem(new[]
                            {
                                group.ProcessName,
                                avgReceivedBs.ToString("F0"),
                                avgSentBs.ToString("F0"),
                                avgTotalBs.ToString("F0"),
                                $"{remoteIps} ({protocols})"
                            })
                            {
                                Tag = group.Pid
                            };
                            listViewNetworkTraffic.Items.Add(item);
                        }
                    }
                }

                SortNetworkTraffic();
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Error updating network traffic: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private async Task UpdateTcpConnections()
        {
            try
            {
                listViewTcpConnections.BeginUpdate();
                listViewTcpConnections.Items.Clear();
                var connections = TcpTableHelper.GetTcpConnections();
                foreach (var conn in connections)
                {
                    string processName = "Unknown";
                    if (processNameCache.ContainsKey(conn.Pid))
                    {
                        processName = processNameCache[conn.Pid];
                    }
                    else
                    {
                        try
                        {
                            processName = Process.GetProcessById(conn.Pid)?.ProcessName ?? "Unknown";
                            processNameCache[conn.Pid] = processName;
                        }
                        catch { }
                    }

                    var item = new ListViewItem(new[]
                    {
                        $"{conn.LocalAddress}:{conn.LocalPort}",
                        $"{conn.RemoteAddress}:{conn.RemotePort}",
                        conn.State,
                        processName
                    });
                    listViewTcpConnections.Items.Add(item);
                }
                listViewTcpConnections.EndUpdate();
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Error updating TCP connections: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private void LoadSuspiciousData()
        {
            try
            {
                listViewSuspicious.BeginUpdate();
                listViewSuspicious.Items.Clear();

                using (var cmd = new SQLiteCommand("SELECT * FROM SuspiciousPackets ORDER BY Timestamp DESC", dbConnection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new ListViewItem(new[]
                        {
                            "Packet",
                            reader["Source"].ToString(),
                            reader["Destination"].ToString(),
                            reader["Protocol"].ToString(),
                            reader["Length"].ToString(),
                            reader["Timestamp"].ToString(),
                            reader["SourcePort"].ToString(),
                            reader["DestPort"].ToString(),
                            reader["Reason"].ToString()
                        });
                        listViewSuspicious.Items.Add(item);
                    }
                }

                using (var cmd = new SQLiteCommand("SELECT * FROM SuspiciousProcesses ORDER BY Timestamp DESC", dbConnection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new ListViewItem(new[]
                        {
                            "Process",
                            reader["Pid"].ToString(),
                            reader["ProcessName"].ToString(),
                            reader["AvgTrafficBs"].ToString(),
                            reader["Timestamp"].ToString(),
                            "",
                            "",
                            reader["Reason"].ToString()
                        });
                        listViewSuspicious.Items.Add(item);
                    }
                }

                listViewSuspicious.EndUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suspicious data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SortNetworkActivity()
        {
            if (networkActivitySortColumn == -1) return;

            listViewNetworkActivity.ListViewItemSorter = new ListViewItemComparer(networkActivitySortColumn, networkActivitySortOrder);
            listViewNetworkActivity.Sort();
        }

        private void SortNetworkTraffic()
        {
            if (networkTrafficSortColumn == -1) return;

            listViewNetworkTraffic.ListViewItemSorter = new ListViewItemComparer(networkTrafficSortColumn, networkTrafficSortOrder);
            listViewNetworkTraffic.Sort();
        }

        private void ListViewNetworkActivity_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 5)
            {
                if (networkActivitySortColumn == e.Column)
                {
                    networkActivitySortOrder = networkActivitySortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
                }
                else
                {
                    networkActivitySortOrder = SortOrder.Descending;
                    networkActivitySortColumn = e.Column;
                }
                SortNetworkActivity();
            }
        }

        private void ListViewNetworkTraffic_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column >= 1 && e.Column <= 3)
            {
                if (networkTrafficSortColumn == e.Column)
                {
                    networkTrafficSortOrder = networkTrafficSortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
                }
                else
                {
                    networkTrafficSortOrder = SortOrder.Descending;
                    networkTrafficSortColumn = e.Column;
                }
                SortNetworkTraffic();
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            updateTimer.Start();
            packetUpdateTimer.Start();
            chartUpdateTimer.Start();
            UpdateNetworkDataAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"Error starting monitoring: {t.Exception?.InnerException?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });

            if (captureDevice != null && !isCapturing)
            {
                try
                {
                    captureDevice.Open(DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, 1000);
                    captureDevice.StartCapture();
                    isCapturing = true;
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"Error starting packet capture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            updateTimer.Stop();
            packetUpdateTimer.Stop();
            chartUpdateTimer.Stop();
            if (isCapturing)
            {
                isCapturing = false;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            updateTimer.Stop();
            packetUpdateTimer.Stop();
            chartUpdateTimer.Stop();
            if (captureDevice != null && isCapturing)
            {
                try
                {
                    captureDevice.StopCapture();
                    captureDevice.Close();
                    isCapturing = false;
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"Error stopping packet capture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }

            listViewNetworkActivity.Items.Clear();
            listViewNetworkTraffic.Items.Clear();
            listViewTcpConnections.Items.Clear();
            listViewUdpListeners.Items.Clear();
            listViewSuspicious.Items.Clear();
            lock (packetQueue)
            {
                packetQueue.Clear();
            }
            lock (processTrafficHistory)
            {
                processTrafficHistory.Clear();
            }
            sentTrafficValues.Clear();
            receivedTrafficValues.Clear();
            timeStamps.Clear();
            networkActivityCache.Clear();
            processNameCache.Clear();
        }

        private void ChkShowListeningPorts_CheckedChanged(object sender, EventArgs e)
        {
            showListeningPortsOnly = chkShowListeningPorts.Checked;
            UpdateNetworkActivity();
        }

        private void BtnSaveToFile_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                saveFileDialog.DefaultExt = "txt";
                saveFileDialog.FileName = $"NetworkMonitor_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        writer.WriteLine($"Network Monitor Log - {DateTime.Now}");
                        writer.WriteLine();

                        writer.WriteLine("Packets:");
                        foreach (ListViewItem item in listViewUdpListeners.Items)
                        {
                            writer.WriteLine($"Source: {item.SubItems[0].Text} | Destination: {item.SubItems[1].Text} | Protocol: {item.SubItems[2].Text} | Length: {item.SubItems[3].Text} | Timestamp: {item.SubItems[4].Text} | Src Port: {item.SubItems[5].Text} | Dst Port: {item.SubItems[6].Text}");
                        }
                        writer.WriteLine();

                        writer.WriteLine("Network Activity:");
                        foreach (ListViewItem item in listViewNetworkActivity.Items)
                        {
                            writer.WriteLine($"{item.SubItems[0].Text} | Local: {item.SubItems[1].Text} | Remote: {item.SubItems[2].Text} | Type: {item.SubItems[3].Text} | Process: {item.SubItems[4].Text} | Load: {item.SubItems[5].Text} B/s");
                        }
                        writer.WriteLine();

                        writer.WriteLine("Network Traffic:");
                        foreach (ListViewItem item in listViewNetworkTraffic.Items)
                        {
                            writer.WriteLine($"Process: {item.SubItems[0].Text} | Avg Incoming: {item.SubItems[1].Text} B/s | Avg Outgoing: {item.SubItems[2].Text} B/s | Avg Total: {item.SubItems[3].Text} B/s | Remote IPs: {item.SubItems[4].Text}");
                        }
                        writer.WriteLine();

                        writer.WriteLine("TCP Connections:");
                        foreach (ListViewItem item in listViewTcpConnections.Items)
                        {
                            writer.WriteLine($"{item.SubItems[0].Text} | {item.SubItems[1].Text} | {item.SubItems[2].Text} | {item.SubItems[3].Text}");
                        }
                        writer.WriteLine();

                        writer.WriteLine("Suspicious Activity:");
                        foreach (ListViewItem item in listViewSuspicious.Items)
                        {
                            writer.WriteLine($"Type: {item.SubItems[0].Text} | Source/PID: {item.SubItems[1].Text} | Dest/Process: {item.SubItems[2].Text} | Protocol/Traffic: {item.SubItems[3].Text} | Length: {item.SubItems[4].Text} | Timestamp: {item.SubItems[5].Text} | Src Port: {item.SubItems[6].Text} | Dst Port: {item.SubItems[7].Text} | Reason: {item.SubItems[8].Text}");
                        }

                        writer.WriteLine();
                        writer.WriteLine("Traffic Graph Data:");
                        for (int i = 0; i < timeStamps.Count; i++)
                        {
                            writer.WriteLine($"Timestamp: {timeStamps[i]:HH:mm:ss}, Sent: {sentTrafficValues[i]:F0} B/s, Received: {receivedTrafficValues[i]:F0} B/s");
                        }
                    }
                    MessageBox.Show($"Data saved to {saveFileDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnRefreshSuspicious_Click(object sender, EventArgs e)
        {
            LoadSuspiciousData();
        }

        private void BtnClearDatabase_Click(object sender, EventArgs e)
        {
            try
            {
                // Подтверждение действия от пользователя
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to clear all data from the database? This action cannot be undone.",
                    "Confirm Database Clear",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    // Очистка таблиц SuspiciousPackets и SuspiciousProcesses
                    using (var cmd = new SQLiteCommand("DELETE FROM SuspiciousPackets", dbConnection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM SuspiciousProcesses", dbConnection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Очистка отображаемого списка подозрительной активности
                    listViewSuspicious.Items.Clear();

                    MessageBox.Show("Database has been successfully cleared.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class ListViewItemComparer : IComparer
    {
        private int column;
        private SortOrder sortOrder;

        public ListViewItemComparer(int column, SortOrder sortOrder)
        {
            this.column = column;
            this.sortOrder = sortOrder;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = x as ListViewItem;
            ListViewItem itemY = y as ListViewItem;

            double valueX, valueY;
            if (!double.TryParse(itemX.SubItems[column].Text, out valueX) || !double.TryParse(itemY.SubItems[column].Text, out valueY))
            {
                return 0;
            }

            int result = valueX.CompareTo(valueY);
            return sortOrder == SortOrder.Ascending ? result : -result;
        }
    }

    public class TcpTableHelper
    {
        private const int AF_INET = 2;
        private const int TCP_TABLE_OWNER_PID_ALL = 5;

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint State;
            public uint LocalAddr;
            public uint LocalPort;
            public uint RemoteAddr;
            public uint RemotePort;
            public uint OwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE_OWNER_PID
        {
            public uint DwNumEntries;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public MIB_TCPROW_OWNER_PID[] Table;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder, int ulAf, int tableClass, uint reserved = 0);

        public static List<(string LocalAddress, int LocalPort, string RemoteAddress, int RemotePort, int Pid, string State)> GetTcpConnections()
        {
            var connections = new List<(string, int, string, int, int, string)>();
            int bufferSize = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, TCP_TABLE_OWNER_PID_ALL);

            IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
            try
            {
                uint result = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, false, AF_INET, TCP_TABLE_OWNER_PID_ALL);
                if (result != 0) return connections;

                var tcpTable = (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(tcpTablePtr, typeof(MIB_TCPTABLE_OWNER_PID));
                IntPtr rowPtr = (IntPtr)((long)tcpTablePtr + Marshal.SizeOf(tcpTable.DwNumEntries));

                for (int i = 0; i < tcpTable.DwNumEntries; i++)
                {
                    var row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    var localAddress = new IPAddress(row.LocalAddr).ToString();
                    var localPort = (int)(row.LocalPort >> 8 | (row.LocalPort & 0xff) << 8);
                    var remoteAddress = new IPAddress(row.RemoteAddr).ToString();
                    var remotePort = (int)(row.RemotePort >> 8 | (row.RemotePort & 0xff) << 8);
                    var state = ((TcpState)row.State).ToString();
                    connections.Add((localAddress, localPort, remoteAddress, remotePort, (int)row.OwningPid, state));
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(row));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTablePtr);
            }
            return connections;
        }
    }

    public class UdpTableHelper
    {
        private const int AF_INET = 2;
        private const int UDP_TABLE_OWNER_PID = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint LocalAddr;
            public uint LocalPort;
            public uint OwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPTABLE_OWNER_PID
        {
            public uint DwNumEntries;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public MIB_UDPROW_OWNER_PID[] Table;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize, bool bOrder, int ulAf, int tableClass, uint reserved = 0);

        public static List<(string LocalAddress, int LocalPort, int Pid)> GetUdpConnections()
        {
            var connections = new List<(string, int, int)>();
            int bufferSize = 0;
            GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, UDP_TABLE_OWNER_PID);

            IntPtr udpTablePtr = Marshal.AllocHGlobal(bufferSize);
            try
            {
                uint result = GetExtendedUdpTable(udpTablePtr, ref bufferSize, false, AF_INET, UDP_TABLE_OWNER_PID);
                if (result != 0) return connections;

                var udpTable = (MIB_UDPTABLE_OWNER_PID)Marshal.PtrToStructure(udpTablePtr, typeof(MIB_UDPTABLE_OWNER_PID));
                IntPtr rowPtr = (IntPtr)((long)udpTablePtr + Marshal.SizeOf(udpTable.DwNumEntries));

                for (int i = 0; i < udpTable.DwNumEntries; i++)
                {
                    var row = (MIB_UDPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_PID));
                    var localAddress = new IPAddress(row.LocalAddr).ToString();
                    var localPort = (int)(row.LocalPort >> 8 | (row.LocalPort & 0xff) << 8);
                    connections.Add((localAddress, localPort, (int)row.OwningPid));
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(row));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(udpTablePtr);
            }
            return connections;
        }
    }

    public enum TcpState : uint
    {
        Closed = 1,
        Listening = 2,
        SynSent = 3,
        SynReceived = 4,
        Established = 5,
        FinWait1 = 6,
        FinWait2 = 7,
        CloseWait = 8,
        Closing = 9,
        LastAck = 10,
        TimeWait = 11,
        DeleteTcb = 12
    }
}