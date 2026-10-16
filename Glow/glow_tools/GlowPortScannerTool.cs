using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
//
using static Glow.TSModules;

namespace Glow.glow_tools
{
    public partial class GlowPortScannerTool : Form
    {
        public GlowPortScannerTool()
        {
            InitializeComponent();
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(PST_DataTable, true, null);
            }
            catch { }
            PST_OpenPortsBtn.Click += PST_OpenPortsBtn_Click;
            PST_RemotePortBtn.Click += PST_RemotePortBtn_Click;
            PST_ExportBtn.Click += PST_ExportBtn_Click;
            PST_AutoRefreshBtn.Click += PST_AutoRefreshBtn_Click;
            PST_IpInfoBtn.Click += PST_IpInfoBtn_Click;
            PST_DataTable.SelectionChanged += PST_DataTable_SelectionChanged;
            PST_TextBox.TextChanged += PST_TextBox_TextChanged;
            PST_TextBoxClearBtn.Click += PST_TextBoxClearBtn_Click;
            PST_SortMode.CheckedChanged += PST_SortMode_CheckedChanged;
            PST_OpenOnly.CheckedChanged += PST_OpenOnly_CheckedChanged;
            PST_DataTable.CellDoubleClick += PST_DataTable_CellDoubleClick;
            PST_RemoteTimeoutTextBox.KeyPress += PST_DigitsOnly_KeyPress;
            PST_RemotePortsTextBox.KeyPress += PST_PortsInput_KeyPress;
        }
        private void PST_ToolTip_Draw(object sender, DrawToolTipEventArgs e) { e.DrawBackground(); e.DrawBorder(); e.DrawText(); }
        private void PST_DigitsOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (e.KeyChar < '0' || e.KeyChar > '9') e.Handled = true;
        }
        private void PST_PortsInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            char c = e.KeyChar;
            if (!((c >= '0' && c <= '9') || c == '-' || c == ',' || c == ';' || c == ' ')) e.Handled = true;
        }
        // Scan state
        private volatile bool PST_isRunning = false;
        private CancellationTokenSource PST_cts;
        private PST_Mode PST_currentMode = PST_Mode.None;
        private volatile bool PST_autoRefresh = true;
        private volatile bool PST_autoRefreshEnabled = true;
        private volatile bool PST_refreshRepeat = false;
        private const int PST_REFRESH_INTERVAL = 45;
        private const int PST_MAX_PORTS = 5000;
        private const int PST_MIN_TIMEOUT = 100;
        private const int PST_MAX_TIMEOUT = 10000;
        private const int PST_SEM_LIMIT = 100;
        private static readonly byte[] PST_DefaultMask = new byte[] { 255, 255, 255, 0 };
        private string PST_baseTitle = "";
        private string PST_lastHost = "127.0.0.1";
        private bool PST_lastHostIsPublic = false;
        private volatile int PST_scanGen = 0;
        private string PST_localBtnText = null;
        private string PST_remoteBtnText = null;
        private string PST_snapshotHost = null;
        private int PST_snapTotal = 0;
        private int PST_snapAdded = 0;
        private int PST_snapRemoved = 0;
        private bool PST_snapIsBaseline = true;
        private readonly List<object[]> PST_masterRows = new List<object[]>();
        private readonly Dictionary<int, int> PST_portToMasterIndex = new Dictionary<int, int>();
        private readonly Dictionary<int, DataGridViewRow> PST_portToGridRow = new Dictionary<int, DataGridViewRow>();
        private System.Windows.Forms.Timer PST_filterTimer;
        private int PST_MasterCount()
        {
            try { lock (PST_masterRows) { return PST_masterRows.Count; } } catch { }
            return 0;
        }
        private void PST_RebuildPortIndex()
        {
            try
            {
                lock (PST_masterRows)
                {
                    PST_portToMasterIndex.Clear();
                    for (int i = 0; i < PST_masterRows.Count; i++)
                    {
                        var r = PST_masterRows[i];
                        if (r != null && r.Length > 0)
                        {
                            int port = PST_SafePort(r[0]);
                            if (port >= 0 && !PST_portToMasterIndex.ContainsKey(port)) PST_portToMasterIndex[port] = i;
                        }
                    }
                }
            }
            catch { }
        }
        private readonly ConcurrentDictionary<int, string> PST_banners = new ConcurrentDictionary<int, string>();
        private DateTime PST_scanStart = DateTime.MinValue;
        private enum PST_Mode { None, Local, Remote }
        private static int PST_SafePort(object value, int fallback = -1)
        {
            try
            {
                if (value is int i) return (i >= 1 && i <= 65535) ? i : fallback;
                if (value is long l) return (l >= 1 && l <= 65535) ? (int)l : fallback;
                if (value != null && int.TryParse(Convert.ToString(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) && p >= 1 && p <= 65535) return p;
            }
            catch { }
            return fallback;
        }
        // Well-known service names (not localized)
        private static readonly Dictionary<int, string> PST_Services = new Dictionary<int, string>{
            {20,"FTP-Data"}, {21,"FTP"}, {22,"SSH"}, {23,"Telnet"}, {25,"SMTP"},
            {37,"Time"}, {42,"WINS"}, {43,"Whois"}, {49,"TACACS"}, {53,"DNS"},
            {70,"Gopher"}, {79,"Finger"}, {80,"HTTP"}, {88,"Kerberos"}, {102,"ISO-TSAP"},
            {110,"POP3"}, {111,"RPCbind"}, {113,"Ident"}, {119,"NNTP"}, {135,"RPC"},
            {139,"NetBIOS"}, {143,"IMAP"}, {179,"BGP"}, {194,"IRC"}, {389,"LDAP"},
            {443,"HTTPS"}, {445,"SMB"}, {464,"Kerberos-PW"}, {465,"SMTPS"}, {554,"RTSP"},
            {563,"NNTPS"}, {587,"SMTP-Sub"}, {593,"RPC-HTTP"}, {631,"IPP"}, {636,"LDAPS"},
            {853,"DoT"}, {990,"FTPS"}, {993,"IMAPS"}, {995,"POP3S"}, {1080,"SOCKS"}, {1194,"OpenVPN"},
            {1433,"MSSQL"}, {1434,"MSSQL-Mon"}, {1521,"Oracle"}, {1723,"PPTP"}, {2049,"NFS"},
            {2082,"cPanel"}, {2083,"cPanel-SSL"}, {2086,"WHM"}, {2087,"WHM-SSL"}, {2375,"Docker"},
            {2376,"Docker-SSL"}, {3306,"MySQL"}, {3389,"RDP"}, {4369,"EPMD"}, {5432,"PostgreSQL"},
            {5672,"AMQP"}, {5900,"VNC"}, {5985,"WinRM"}, {5986,"WinRM-SSL"}, {6379,"Redis"},
            {6443,"Kubernetes"}, {8000,"HTTP-Alt"}, {8008,"HTTP-Alt"}, {8080,"HTTP-Alt"}, {8443,"HTTPS-Alt"},
            {8888,"HTTP-Alt"}, {9042,"Cassandra"}, {9090,"Prometheus"}, {9092,"Kafka"}, {9200,"Elastic"},
            {9300,"Elastic"}, {11211,"Memcached"}, {15672,"RabbitMQ"}, {27017,"MongoDB"}, {33060,"MySQL-X"},
            {5601,"Kibana"}
        };
        private static string PST_GetServiceName(int port)
        {
            return PST_Services.TryGetValue(port, out string s) ? s : "-";
        }
        // Language lookup with a small static cache (TSGetLangs only caches per instance).
        private static readonly object PST_langLock = new object();
        private static readonly Dictionary<string, string> PST_langCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string PST_lastLangPath = null;
        private static string PST_CachedLangs(string langPath, string section, string key)
        {
            string cacheKey = (langPath ?? "") + "|" + section + "|" + key;
            lock (PST_langLock)
            {
                if (PST_lastLangPath == null || !string.Equals(PST_lastLangPath, langPath, StringComparison.OrdinalIgnoreCase))
                {
                    PST_langCache.Clear();
                    PST_lastLangPath = langPath;
                }
                if (PST_langCache.TryGetValue(cacheKey, out string hit)) return hit;
            }
            string val;
            try { val = new TSGetLangs(langPath).TSReadLangs(section, key); }
            catch { val = "N/A Langs"; }
            lock (PST_langLock) { PST_langCache[cacheKey] = val; }
            return val;
        }
        private string PST_Text(string key)
        {
            return PST_CachedLangs(GlowMain.lang_path, "PortScannerTool", key);
        }
        private static string PST_StaticText(string key)
        {
            return PST_CachedLangs(GlowMain.lang_path, "PortScannerTool", key);
        }
        private string PST_StateText(TcpState state)
        {
            switch (state)
            {
                case TcpState.Closed: return PST_Text("pst_state_closed");
                case TcpState.Listen: return PST_Text("pst_state_listen");
                case TcpState.SynSent: return PST_Text("pst_state_syn_sent");
                case TcpState.SynReceived: return PST_Text("pst_state_syn_received");
                case TcpState.Established: return PST_Text("pst_state_established");
                case TcpState.FinWait1: return PST_Text("pst_state_fin_wait_1");
                case TcpState.FinWait2: return PST_Text("pst_state_fin_wait_2");
                case TcpState.CloseWait: return PST_Text("pst_state_close_wait");
                case TcpState.Closing: return PST_Text("pst_state_closing");
                case TcpState.LastAck: return PST_Text("pst_state_last_ack");
                case TcpState.TimeWait: return PST_Text("pst_state_time_wait");
                case TcpState.DeleteTcb: return PST_Text("pst_state_delete_tcb");
                default: return PST_Text("pst_state_unknown");
            }
        }
        public void GTool_PortScanner_Preloader()
        {
            try
            {
                TSThemeModeHelper.InitializeThemeForForm(this);
                BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor");
                PST_RemotePanel.BackColor = BackColor;
                PST_FilterPanel.BackColor = BackColor;
                PST_BottomPanel.BackColor = BackColor;
                PST_DataTable.BackgroundColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor");
                PST_DataTable.GridColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "SelectBoxBorderColor");
                PST_DataTable.DefaultCellStyle.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor");
                PST_DataTable.DefaultCellStyle.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1");
                PST_DataTable.AlternatingRowsDefaultCellStyle.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                PST_DataTable.ColumnHeadersDefaultCellStyle.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                PST_DataTable.ColumnHeadersDefaultCellStyle.SelectionBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                PST_DataTable.ColumnHeadersDefaultCellStyle.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                PST_DataTable.DefaultCellStyle.SelectionBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                PST_DataTable.DefaultCellStyle.SelectionForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                foreach (var btn in new[] { PST_OpenPortsBtn, PST_RemotePortBtn, PST_ExportBtn, PST_AutoRefreshBtn, PST_IpInfoBtn })
                {
                    btn.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                    btn.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                    btn.FlatAppearance.BorderColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                    btn.FlatAppearance.MouseDownBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                    btn.FlatAppearance.MouseOverBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "AccentColorHover");
                }
                TSImageRenderer(PST_OpenPortsBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_start_light : Properties.Resources.ct_test_start_dark, 18, ContentAlignment.MiddleRight);
                TSImageRenderer(PST_RemotePortBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_start_light : Properties.Resources.ct_test_start_dark, 18, ContentAlignment.MiddleRight);
                TSImageRenderer(PST_ExportBtn, GlowMain.theme == 1 ? Properties.Resources.ct_export_light : Properties.Resources.ct_export_dark, 17, ContentAlignment.MiddleRight);
                TSImageRenderer(PST_IpInfoBtn, GlowMain.theme == 1 ? Properties.Resources.ct_link_mc_light : Properties.Resources.ct_link_mc_dark, 13, ContentAlignment.MiddleRight);
                Text = string.Format(PST_Text("pst_title"), Application.ProductName);
                PST_baseTitle = Text;
                PST_OpenPortsBtn.Text = " " + PST_Text("pst_local_btn");
                PST_RemotePortBtn.Text = " " + PST_Text("pst_remote_btn");
                PST_ExportBtn.Text = " " + PST_Text("pst_export_btn");
                PST_UpdateAutoRefreshBtn();
                PST_IpInfoBtn.Text = " " + PST_Text("pst_ipinfo_btn");
                TS_AdjustButtonWidth(PST_IpInfoBtn);
                // If a scan is running, keep the cancel affordance with the fresh language.
                if (PST_isRunning)
                {
                    PST_ApplyStopLook();
                    // Restore targets must be the fresh scan labels, not the pre-switch text.
                    PST_localBtnText = " " + PST_Text("pst_local_btn");
                    PST_remoteBtnText = " " + PST_Text("pst_remote_btn");
                }
                else
                {
                    PST_localBtnText = null;
                    PST_remoteBtnText = null;
                }
                PST_SearchLabel.Text = PST_Text("pst_search_label");
                PST_TotalCount.Text = PST_Text("pst_total_count");
                PST_SortMode.Text = PST_Text("pst_order_in_reverse");
                PST_OpenOnly.Text = PST_Text("pst_open_only");
                PST_RemoteHostLabel.Text = PST_Text("pst_host_label");
                PST_RemotePortsLabel.Text = PST_Text("pst_ports_label");
                PST_RemoteTimeoutLabel.Text = PST_Text("pst_timeout_label");
                try
                {
                    if (PST_ToolTip != null)
                    {
                        PST_ToolTip.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1");
                        PST_ToolTip.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor");
                        PST_ToolTip.SetToolTip(PST_RemoteHostTextBox, PST_Text("pst_host_hint"));
                        PST_ToolTip.SetToolTip(PST_RemotePortsTextBox, PST_Text("pst_ports_hint"));
                        PST_ToolTip.SetToolTip(PST_IpInfoBtn, PST_Text("pst_ipinfo_hint"));
                    }
                }
                catch { }
                Color inputBg = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                Color inputFg = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1");
                foreach (var tb in new[] { PST_TextBox, PST_RemoteHostTextBox, PST_RemotePortsTextBox, PST_RemoteTimeoutTextBox })
                {
                    tb.BackColor = inputBg;
                    tb.ForeColor = inputFg;
                }
                foreach (var lbl in new[] { PST_SearchLabel, PST_TotalCount, PST_RemoteHostLabel, PST_RemotePortsLabel, PST_RemoteTimeoutLabel })
                    lbl.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1");
                foreach (var chk in new[] { PST_SortMode, PST_OpenOnly })
                {
                    chk.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1");
                    chk.CheckedColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                    chk.CheckMarkColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                    chk.UncheckedBorderColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "CheckBoxUnCheckBorderColor");
                }
                PST_TextBoxClearBtn.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                PST_TextBoxClearBtn.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                PST_TextBoxClearBtn.FlatAppearance.BorderColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                PST_TextBoxClearBtn.FlatAppearance.MouseDownBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                PST_TextBoxClearBtn.FlatAppearance.MouseOverBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "AccentColorHover");
                TSImageRenderer(PST_TextBoxClearBtn, GlowMain.theme == 1 ? Properties.Resources.ct_clear_light : Properties.Resources.ct_clear_dark, 15, ContentAlignment.MiddleCenter);
                PST_TotalCount_V.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                PST_ApplyColumnHeaders();
                try
                {
                    if (PST_hasSnapshot) PST_RefreshSnapshotText();
                    if (PST_MasterCount() > 0) PST_RefreshView();
                    PST_UpdateIpInfoBtn();
                }
                catch { }
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "GTool_PortScanner_Preloader()"); }
            }
        }
        private void PST_ApplyColumnHeaders()
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { Invoke(new Action(PST_ApplyColumnHeaders)); } catch { }
                    return;
                }
                if (!IsHandleCreated) return;
                if (PST_currentMode == PST_Mode.Remote && PST_DataTable.Columns.Count == 5)
                {
                    PST_DataTable.Columns[0].HeaderText = PST_Text("col_port");
                    PST_DataTable.Columns[1].HeaderText = PST_Text("col_status");
                    PST_DataTable.Columns[2].HeaderText = PST_Text("col_service");
                    PST_DataTable.Columns[3].HeaderText = PST_Text("col_banner");
                    PST_DataTable.Columns[4].HeaderText = PST_Text("col_risk");
                }
                else if (PST_currentMode == PST_Mode.Local && PST_DataTable.Columns.Count == 9)
                {
                    PST_DataTable.Columns[0].HeaderText = PST_Text("col_proto");
                    PST_DataTable.Columns[1].HeaderText = PST_Text("col_local");
                    PST_DataTable.Columns[2].HeaderText = PST_Text("col_port");
                    PST_DataTable.Columns[3].HeaderText = PST_Text("col_remote");
                    PST_DataTable.Columns[4].HeaderText = PST_Text("col_state");
                    PST_DataTable.Columns[5].HeaderText = PST_Text("col_pid");
                    PST_DataTable.Columns[6].HeaderText = PST_Text("col_program");
                    PST_DataTable.Columns[7].HeaderText = PST_Text("col_exposure");
                    PST_DataTable.Columns[8].HeaderText = PST_Text("col_risk");
                }
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_ApplyColumnHeaders()"); }
            }
        }
        private void PST_SetupColumns(PST_Mode mode)
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { Invoke(new Action(() => PST_SetupColumns(mode))); } catch { }
                    return;
                }
                if (!IsHandleCreated) return;
                PST_DataTable.Columns.Clear();
                PST_DataTable.Rows.Clear();
                PST_currentMode = mode;
                if (mode == PST_Mode.Remote)
                {
                    PST_DataTable.Columns.Add("col_port", PST_Text("col_port"));
                    PST_DataTable.Columns.Add("col_status", PST_Text("col_status"));
                    PST_DataTable.Columns.Add("col_service", PST_Text("col_service"));
                    PST_DataTable.Columns.Add("col_banner", PST_Text("col_banner"));
                    PST_DataTable.Columns.Add("col_risk", PST_Text("col_risk"));
                }
                else
                {
                    PST_DataTable.Columns.Add("col_proto", PST_Text("col_proto"));
                    PST_DataTable.Columns.Add("col_local", PST_Text("col_local"));
                    PST_DataTable.Columns.Add("col_port", PST_Text("col_port"));
                    PST_DataTable.Columns.Add("col_remote", PST_Text("col_remote"));
                    PST_DataTable.Columns.Add("col_state", PST_Text("col_state"));
                    PST_DataTable.Columns.Add("col_pid", PST_Text("col_pid"));
                    PST_DataTable.Columns.Add("col_program", PST_Text("col_program"));
                    PST_DataTable.Columns.Add("col_exposure", PST_Text("col_exposure"));
                    PST_DataTable.Columns.Add("col_risk", PST_Text("col_risk"));
                }
                TSDpiHelper.ScaleDataGridView(PST_DataTable, null, 32);
                foreach (DataGridViewColumn col in PST_DataTable.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                try
                {
                    // The open-only filter applies to Local mode (remote lists open ports by definition).
                    // Preserve the user preference when switching modes; it is ignored in Remote mode.
                    PST_OpenOnly.Enabled = (mode != PST_Mode.Remote);
                }
                catch { }
                PST_ApplyColumnHeaders();
                PST_UpdateIpInfoBtn();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_SetupColumns()"); }
            }
        }
        private void GlowPortScannerTool_Load(object sender, EventArgs e)
        {
            try
            {
                GTool_PortScanner_Preloader();
                PST_InitRemoteInputs();
                PST_SetupColumns(PST_Mode.Local);
                PST_DataTable.ClearSelection();
                PST_UpdateIpInfoBtn();
                this.FormClosing += new FormClosingEventHandler(GlowPortScannerTool_FormClosing);
                _ = PST_LoadLocalAsync();
                _ = PST_AutoRefreshLoopAsync();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "GlowPortScannerTool_Load()"); }
            }
        }
        // UI DPI (shared standard: TSDpiHelper; columns are not rebuilt, data is preserved)
        // ======================================================================================================
        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            try
            {
                TSDpiHelper.ScaleDataGridView(PST_DataTable, null, 32);
                GTool_PortScanner_Preloader();
                this.PerformLayout();
                this.Invalidate(true);
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "OnDpiChanged()"); }
            }
        }
        // Built-in input defaults (kept in code, not in the designer).
        private const string PST_DEFAULT_HOST = "127.0.0.1";
        private const string PST_DEFAULT_PORTS = "1-1024";
        private const int PST_DEFAULT_TIMEOUT = 500;
        private void PST_InitRemoteInputs()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PST_RemoteHostTextBox.Text)) PST_RemoteHostTextBox.Text = PST_DEFAULT_HOST;
                if (string.IsNullOrWhiteSpace(PST_RemotePortsTextBox.Text)) PST_RemotePortsTextBox.Text = PST_DEFAULT_PORTS;
                if (!int.TryParse(PST_RemoteTimeoutTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) || t <= 0)
                    t = PST_DEFAULT_TIMEOUT;
                if (t < PST_MIN_TIMEOUT) t = PST_MIN_TIMEOUT;
                if (t > PST_MAX_TIMEOUT) t = PST_MAX_TIMEOUT;
                PST_RemoteTimeoutTextBox.Text = t.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_InitRemoteInputs()"); }
            }
        }
        private void GlowPortScannerTool_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cancel the scan but never block closing; background tasks check IsDisposed/IsHandleCreated.
            PST_autoRefresh = false;
            PST_refreshRepeat = false;
            try { PST_cts?.Cancel(); } catch { }
            try { PST_cts?.Dispose(); } catch { }
            PST_cts = null;
        }
        // Open-port snapshot for change detection.
        private readonly HashSet<string> PST_lastOpenKeys = new HashSet<string>();
        private bool PST_hasSnapshot = false;
        private PST_Mode PST_snapshotMode = PST_Mode.None;
        private string PST_SnapshotSummary = "";
        private DateTime PST_lastSnapDialog = DateTime.MinValue;
        private static string PST_OpenKey(object[] raw)
        {
            try
            {
                // Listen-only; ephemeral ports churn constantly and would add noise.
                if (raw == null || raw.Length < 5) return null;
                if (!(raw[4] is TcpState state) || state != TcpState.Listen) return null;
                int port = PST_SafePort(raw[2]);
                if (port < 0 || PST_IsEphemeralPort(port)) return null;
                string proto = Convert.ToString(raw[0]) ?? "";
                string addr = Convert.ToString(raw[1]) ?? "";
                try { addr = addr.Trim().ToLowerInvariant(); } catch { }
                return proto + "|" + addr + "|" + port.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
        private string PST_RemoteOpenKey(object[] raw)
        {
            try
            {
                // Remote row layout: port, open flag, service, banner, risk placeholder.
                if (raw == null || raw.Length < 1) return null;
                int port = PST_SafePort(raw[0]);
                if (port < 0) return null;
                return "R|" + (PST_lastHost ?? "").Trim().ToLowerInvariant() + "|" + port.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
        private static string PST_KeyToText(string key)
        {
            try
            {
                var parts = key.Split('|');
                if (parts.Length == 3 && parts[0] == "R")
                {
                    if (int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rport))
                    {
                        string rservice = PST_GetServiceName(rport);
                        string suffix = string.IsNullOrEmpty(rservice) || rservice == "-" ? "" : "  (" + rservice + ")";
                        return string.Format(CultureInfo.InvariantCulture, "{0}{1}  [{2}]", rport, suffix, parts[1]);
                    }
                    return key;
                }
                if (parts.Length == 3)
                {
                    string proto3 = parts[0];
                    string addr3 = parts[1];
                    if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int port3)) return key;
                    string service3 = PST_GetServiceName(port3);
                    string sfx3 = string.IsNullOrEmpty(service3) || service3 == "-" ? "" : "  (" + service3 + ")";
                    if (string.IsNullOrEmpty(addr3)) return string.Format(CultureInfo.InvariantCulture, "{0}  {1}{2}", port3, proto3, sfx3);
                    return string.Format(CultureInfo.InvariantCulture, "{0}  {1}  [{2}]{3}", port3, proto3, addr3, sfx3);
                }
                return key;
            }
            catch
            {
                return key;
            }
        }
        private string PST_AppendSnapList(string detail, string labelKey, List<string> keys)
        {
            const int cap = 12;
            var sb = new StringBuilder(detail);
            sb.Append(string.Format(PST_Text(labelKey), keys.Count)).AppendLine();
            for (int i = 0; i < keys.Count && i < cap; i++) sb.Append("  ").Append(PST_KeyToText(keys[i])).AppendLine();
            if (keys.Count > cap) sb.Append("  ...").AppendLine();
            sb.AppendLine();
            return sb.ToString();
        }
        private void PST_RefreshSnapshotText()
        {
            try
            {
                if (PST_snapIsBaseline) PST_SnapshotSummary = string.Format(PST_Text("pst_snap_baseline"), PST_snapTotal);
                else PST_SnapshotSummary = string.Format(PST_Text("pst_snap_summary"), PST_snapAdded, PST_snapRemoved, PST_snapTotal);
            }
            catch { }
        }
        private void PST_ApplySnapshot()
        {
            try
            {
                var current = new HashSet<string>();
                // Displayed total counts every Listen row so it matches the open-only grid view.
                // Added/removed use the stable subset (distinct non-ephemeral endpoints) so
                // ephemeral churn cannot flap the diff or spam popups.
                int listenRows = 0;
                string curHost = (PST_lastHost ?? "").Trim().ToLowerInvariant();
                lock (PST_masterRows)
                {
                    for (int i = 0; i < PST_masterRows.Count; i++)
                    {
                        var rawRow = PST_masterRows[i];
                        string key = PST_currentMode == PST_Mode.Remote ? PST_RemoteOpenKey(rawRow) : PST_OpenKey(rawRow);
                        if (key != null) current.Add(key);
                        if (PST_currentMode == PST_Mode.Remote)
                        {
                            if (rawRow != null) listenRows++;
                        }
                        else if (rawRow != null && rawRow.Length > 4 && rawRow[4] is TcpState state && state == TcpState.Listen)
                        {
                            listenRows++;
                        }
                    }
                }
                bool hostChanged = PST_currentMode == PST_Mode.Remote && !string.Equals(PST_snapshotHost ?? "", curHost, StringComparison.OrdinalIgnoreCase);
                // First scan, mode switch or remote host change silently records the baseline.
                if (!PST_hasSnapshot || PST_snapshotMode != PST_currentMode || hostChanged)
                {
                    PST_lastOpenKeys.Clear();
                    foreach (string k in current) PST_lastOpenKeys.Add(k);
                    PST_hasSnapshot = true;
                    PST_snapshotMode = PST_currentMode;
                    PST_snapshotHost = PST_currentMode == PST_Mode.Remote ? curHost : null;
                    PST_snapTotal = listenRows;
                    PST_snapAdded = 0;
                    PST_snapRemoved = 0;
                    PST_snapIsBaseline = true;
                    PST_RefreshSnapshotText();
                    return;
                }
                var added = new List<string>();
                var removed = new List<string>();
                foreach (string k in current) if (!PST_lastOpenKeys.Contains(k)) added.Add(k);
                foreach (string k in PST_lastOpenKeys) if (!current.Contains(k)) removed.Add(k);
                PST_lastOpenKeys.Clear();
                foreach (string k in current) PST_lastOpenKeys.Add(k);
                PST_snapTotal = listenRows;
                PST_snapAdded = added.Count;
                PST_snapRemoved = removed.Count;
                PST_snapIsBaseline = false;
                PST_RefreshSnapshotText();
                if (added.Count == 0 && removed.Count == 0) return;
                // The summary stays in the title; the popup only fires for newly opened ports, throttled.
                if (added.Count == 0) return;
                DateTime now = DateTime.Now;
                if (now - PST_lastSnapDialog < TimeSpan.FromMinutes(5)) return;
                PST_lastSnapDialog = now;
                string header = string.Format(PST_Text("pst_snap_header"), listenRows) + Environment.NewLine + Environment.NewLine;
                string detail = header;
                if (added.Count > 0) detail = PST_AppendSnapList(detail, "pst_snap_added", added);
                if (removed.Count > 0) detail = PST_AppendSnapList(detail, "pst_snap_removed", removed);
                TS_MessageBoxEngine.TS_MessageBox(this, added.Count > 0 ? 2 : 1, detail.TrimEnd());
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_ApplySnapshot()"); }
            }
        }
        // Exposure and risk rating.
        private enum PST_Risk { None, Low, Medium, High }
        private static readonly Dictionary<int, PST_Risk> PST_RiskPorts = new Dictionary<int, PST_Risk>{
            {21, PST_Risk.High}, {22, PST_Risk.Medium}, {23, PST_Risk.High}, {25, PST_Risk.Medium},
            {53, PST_Risk.Medium}, {69, PST_Risk.High}, {80, PST_Risk.Low}, {88, PST_Risk.Medium}, {110, PST_Risk.Medium}, {111, PST_Risk.Medium}, {135, PST_Risk.Medium},
            {137, PST_Risk.Medium}, {138, PST_Risk.Medium}, {139, PST_Risk.Medium}, {143, PST_Risk.Medium},
            {161, PST_Risk.High},
            {389, PST_Risk.Medium}, {445, PST_Risk.High}, {464, PST_Risk.Medium}, {465, PST_Risk.Medium}, {512, PST_Risk.High}, {513, PST_Risk.High},
            {514, PST_Risk.High}, {587, PST_Risk.Medium}, {636, PST_Risk.Low}, {990, PST_Risk.Medium},
            {993, PST_Risk.Low}, {995, PST_Risk.Low}, {1433, PST_Risk.Medium}, {1434, PST_Risk.High},
            {1521, PST_Risk.Medium}, {2049, PST_Risk.High}, {3306, PST_Risk.Medium}, {3389, PST_Risk.High}, {5432, PST_Risk.Medium},
            {5900, PST_Risk.High}, {5985, PST_Risk.Medium}, {5986, PST_Risk.Low}, {6379, PST_Risk.High},
            {9200, PST_Risk.Medium}, {9300, PST_Risk.High}, {11211, PST_Risk.High}, {27017, PST_Risk.High},
            {2375, PST_Risk.High}
        };
        private static bool PST_IsLoopbackText(string address)
        {
            if (string.IsNullOrWhiteSpace(address) || address == "-") return false;
            address = address.Trim();
            // Strips zone ids ("::1%12") and brackets ("[::1]").
            int pct = address.IndexOf('%');
            if (pct >= 0) address = address.Substring(0, pct);
            if (address.StartsWith("[", StringComparison.Ordinal) && address.EndsWith("]", StringComparison.Ordinal) && address.Length > 2)
                address = address.Substring(1, address.Length - 2);
            // 127.0.0.0/8 is all loopback (RFC 1122 3.2.1.3).
            if (address == "127.0.0.1" || address == "::1") return true;
            return address.StartsWith("127.", StringComparison.Ordinal);
        }
        private static bool PST_IsEphemeralPort(int port)
        {
            // Known services on high ports must still trigger diffs (e.g. custom DB, proxy).
            // Only unknown high-port churn is treated as ephemeral noise.
            if (PST_Services.ContainsKey(port) || PST_RiskPorts.ContainsKey(port)) return false;
            PST_GetEphemeralRange(out int start, out int end);
            return port >= start && port <= end;
        }
        private static int PST_ephStart = 49152;
        private static int PST_ephEnd = 65535;
        private static int PST_ephLoaded = 0;
        private static void PST_GetEphemeralRange(out int start, out int end)
        {
            if (PST_ephLoaded == 1) { start = PST_ephStart; end = PST_ephEnd; return; }
            start = 49152; end = 65535;
            try
            {
                // Best effort: honor a customized Windows dynamic port range.
                // Registry values are DWORDs when customized; otherwise fall back to defaults.
                object rs = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DynamicPortRangeStart", null);
                object rn = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DynamicPortRangeNumberOfPorts", null);
                int s = PST_RegistryInt(rs);
                int n = PST_RegistryInt(rn);
                if (s >= 1024 && s <= 65535 && n > 0 && n <= 65535 - s + 1) { start = s; end = s + n - 1; }
            }
            catch { }
            PST_ephStart = start; PST_ephEnd = end;
            Interlocked.Exchange(ref PST_ephLoaded, 1);
        }
        private static int PST_RegistryInt(object value)
        {
            if (value is int i) return i;
            if (value != null && int.TryParse(Convert.ToString(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) return parsed;
            return 0;
        }
        private static bool PST_IsWildcardText(string address)
        {
            if (string.IsNullOrWhiteSpace(address) || address == "-") return false;
            return address == "0.0.0.0" || address == "::" || address == "::0" || address == "0:0:0:0:0:0:0:0";
        }
        private string PST_ExposureText(object[] raw)
        {
            try
            {
                if (raw == null || raw.Length < 2) return "-";
                string address = Convert.ToString(raw[1]);
                if (PST_IsLoopbackText(address)) return PST_Text("pst_exp_local");
                if (PST_IsWildcardText(address)) return PST_Text("pst_exp_public");
                return PST_Text("pst_exp_network");
            }
            catch
            {
                return "-";
            }
        }
        // Not reachable externally, so one notch lower; unknown stays unrated.
        private static bool PST_LoopbackDiscount(ref PST_Risk risk)
        {
            if (risk == PST_Risk.High) risk = PST_Risk.Medium;
            else if (risk == PST_Risk.Medium) risk = PST_Risk.Low;
            else if (risk == PST_Risk.None) return false;
            else risk = PST_Risk.Low;
            return true;
        }
        private string PST_RiskText(object[] raw, int port)
        {
            try
            {
                bool isRemote = raw != null && raw.Length > 1 && raw[1] is bool;
                bool loopback = raw != null && raw.Length > 1 && PST_IsLoopbackText(Convert.ToString(raw[1]));
                if (!PST_RiskPorts.TryGetValue(port, out PST_Risk baseRisk)) baseRisk = PST_Risk.None;
                // Remote layout has no address column (raw[1] is the open flag); a loopback
                // target is still local-only. An open remote port or any non-loopback local
                // listener is reachable by definition, so floor unknown at Low.
                bool effectiveLoopback = isRemote ? (PST_IsLoopbackText(PST_lastHost) || string.Equals((PST_lastHost ?? "").Trim(), "localhost", StringComparison.OrdinalIgnoreCase)) : loopback;
                if (effectiveLoopback)
                {
                    if (!PST_LoopbackDiscount(ref baseRisk)) return "-";
                }
                else if (baseRisk == PST_Risk.None) baseRisk = PST_Risk.Low;
                switch (baseRisk)
                {
                    case PST_Risk.High: return PST_Text("pst_risk_high");
                    case PST_Risk.Medium: return PST_Text("pst_risk_medium");
                    case PST_Risk.Low: return PST_Text("pst_risk_low");
                    default: return "-";
                }
            }
            catch
            {
                return "-";
            }
        }
        // Remote target checks (subnet and NAT awareness).
        // Up, non-loopback local IPv4 addresses with their masks.
        private sealed class PST_IPv4Entry
        {
            public byte[] Addr;
            public byte[] Mask;
            public string Text;
        }
        private static List<PST_IPv4Entry> PST_GetLocalIPv4()
        {
            var list = new List<PST_IPv4Entry>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        list.Add(new PST_IPv4Entry
                        {
                            Addr = ua.Address.GetAddressBytes(),
                            Mask = ua.IPv4Mask != null ? ua.IPv4Mask.GetAddressBytes() : PST_DefaultMask,
                            Text = ua.Address.ToString()
                        });
                    }
                }
            }
            catch { }
            return list;
        }
        private static string PST_LocalIPv4For(IPAddress target)
        {
            try
            {
                byte[] t = (target != null && target.AddressFamily == AddressFamily.InterNetwork) ? target.GetAddressBytes() : null;
                string first = null;
                foreach (var e in PST_GetLocalIPv4())
                {
                    if (first == null) first = e.Text;
                    if (t != null)
                    {
                        bool match = true;
                        for (int i = 0; i < 4; i++)
                        {
                            if ((e.Addr[i] & e.Mask[i]) != (t[i] & e.Mask[i])) { match = false; break; }
                        }
                        if (match) return e.Text;
                    }
                }
                return first;
            }
            catch { }
            return null;
        }
        private static bool PST_IsPrivateAddress(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return true;
            byte[] b = ip.GetAddressBytes();
            if (b.Length != 4) return true;
            if (b[0] == 10) return true;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            if (b[0] == 192 && b[1] == 168) return true;
            if (b[0] == 169 && b[1] == 254) return true;
            if (b[0] == 127) return true;
            return false;
        }
        private static bool PST_IsIPv6Global(IPAddress ip)
        {
            try
            {
                if (ip == null || ip.AddressFamily != AddressFamily.InterNetworkV6) return false;
                if (IPAddress.IsLoopback(ip)) return false;
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Teredo) return false;
                if (ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.IPv6None)) return false;
                byte[] b = ip.GetAddressBytes();
                if (b == null || b.Length != 16) return false;
                // IPv4-mapped (::ffff:0:0/96): judge by the embedded IPv4 address.
                if (b[0] == 0 && b[1] == 0 && b[2] == 0 && b[3] == 0 && b[4] == 0
                    && b[5] == 0 && b[6] == 0 && b[7] == 0 && b[8] == 0 && b[9] == 0
                    && b[10] == 0xFF && b[11] == 0xFF)
                {
                    if (b[12] == 10) return false;
                    if (b[12] == 172 && b[13] >= 16 && b[13] <= 31) return false;
                    if (b[12] == 192 && b[13] == 168) return false;
                    if (b[12] == 169 && b[13] == 254) return false;
                    if (b[12] == 127) return false;
                    return true;
                }
                // ULA fc00::/7, documentation 2001:db8::/32, discard fec0::/10, benchmarking 2001:2::/48.
                if ((b[0] & 0xFE) == 0xFC) return false;
                if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8) return false;
                if (b[0] == 0xFE && (b[1] & 0xC0) == 0xC0) return false;
                if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x00 && b[3] == 0x02) return false;
                // Multicast ff00::/8 is not a scannable unicast target.
                if (b[0] == 0xFF) return false;
                return true;
            }
            catch { return false; }
        }
        private static bool PST_IsLoopbackHost(string host)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(host)) return false;
                host = host.Trim();
                if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
                if (PST_IsLoopbackText(host)) return true;
                if (IPAddress.TryParse(host, out IPAddress ip) && IPAddress.IsLoopback(ip)) return true;
            }
            catch { }
            return false;
        }
        private static bool PST_IsOnMySubnet(IPAddress target)
        {
            try
            {
                if (target == null || target.AddressFamily != AddressFamily.InterNetwork) return false;
                byte[] t = target.GetAddressBytes();
                foreach (var e in PST_GetLocalIPv4())
                {
                    bool match = true;
                    for (int i = 0; i < 4; i++)
                    {
                        if ((e.Addr[i] & e.Mask[i]) != (t[i] & e.Mask[i])) { match = false; break; }
                    }
                    if (match) return true;
                }
            }
            catch { }
            return false;
        }
        private async Task<bool> PST_ConfirmRemoteTargetAsync(string host, int portCount)
        {
            IPAddress[] addrs;
            try
            {
                // Resolve off the UI thread; the dialogs stay on the UI thread.
                addrs = await Task.Run(() => Dns.GetHostAddresses(host));
            }
            catch
            {
                addrs = new IPAddress[0];
            }
            if (addrs == null || addrs.Length == 0)
            {
                PST_lastHostIsPublic = false;
                TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(PST_Text("pst_host_invalid"), host));
                return false;
            }
            // Every resolved address counts: a dual-stack name with a private A record
            // but a global AAAA record still reaches the public internet.
            bool anyPublicV4 = false;
            bool anyGlobalV6 = false;
            IPAddress firstPrivateV4 = null;
            foreach (var a in addrs)
            {
                if (a.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (IPAddress.IsLoopback(a)) continue;
                    if (!PST_IsPrivateAddress(a)) anyPublicV4 = true;
                    else if (firstPrivateV4 == null) firstPrivateV4 = a;
                }
                else if (a.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (PST_IsIPv6Global(a)) anyGlobalV6 = true;
                }
            }
            PST_lastHostIsPublic = (anyPublicV4 || anyGlobalV6);
            if (anyPublicV4 || anyGlobalV6)
            {
                TS_MessageBoxEngine.TS_MessageBox(this, 2, string.Format(PST_Text("pst_public_warn"), host, "\n\n", "\n\n"));
            }
            else if (firstPrivateV4 != null && !PST_IsOnMySubnet(firstPrivateV4))
            {
                string local = PST_LocalIPv4For(firstPrivateV4);
                TS_MessageBoxEngine.TS_MessageBox(this, 2, string.Format(PST_Text("pst_other_subnet"), host, string.IsNullOrEmpty(local) ? "-" : local, "\n"));
            }
            return TS_MessageBoxEngine.TS_MessageBox(this, 6, string.Format(PST_Text("pst_remote_confirm"), host, portCount, "\n\n", "\n\n")) == DialogResult.Yes;
        }
        // Table view: search, reverse order, counter, copy.
        private static string[] PST_SplitWords(string q)
        {
            if (string.IsNullOrEmpty(q)) return new string[0];
            return q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
        private static bool PST_RowMatches(object[] row, string[] searchWords)
        {
            if (searchWords.Length == 0) return true;
            if (row == null) return false;
            string[] cellTexts = new string[row.Length];
            for (int i = 0; i < row.Length; i++)
            {
                try { cellTexts[i] = row[i]?.ToString() ?? string.Empty; } catch { cellTexts[i] = string.Empty; }
            }
            foreach (var searchWord in searchWords)
            {
                if (string.IsNullOrEmpty(searchWord)) continue;
                bool wordFound = false;
                foreach (var cellText in cellTexts)
                {
                    if (cellText.IndexOf(searchWord, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        wordFound = true;
                        break;
                    }
                }
                if (!wordFound) return false;
            }
            return true;
        }
        private object[] PST_RenderRow(object[] raw)
        {
            try
            {
                if (raw == null) return new object[0];
                int extra = 0;
                if (PST_currentMode == PST_Mode.Remote && raw.Length < 5) extra = 5 - raw.Length;
                else if (PST_currentMode == PST_Mode.Local && raw.Length < 9) extra = 9 - raw.Length;
                var disp = new object[raw.Length + extra];
                for (int i = 0; i < raw.Length; i++)
                {
                    if (raw[i] is TcpState st) disp[i] = PST_StateText(st);
                    else if (raw[i] is bool b && b) disp[i] = PST_Text("pst_open");
                    else disp[i] = raw[i];
                }
                for (int i = raw.Length; i < disp.Length; i++) disp[i] = "-";
                if (PST_currentMode == PST_Mode.Remote && disp.Length > 4)
                {
                    int port = PST_SafePort(disp[0]);
                    if (port < 0) port = PST_SafePort(raw.Length > 0 ? raw[0] : null);
                    if (port < 0) return disp;
                    disp[3] = PST_banners.TryGetValue(port, out string banner) && !string.IsNullOrEmpty(banner) ? banner : "-";
                    disp[4] = PST_RiskText(raw, port);
                }
                else if (PST_currentMode == PST_Mode.Local && disp.Length > 8)
                {
                    disp[7] = PST_ExposureText(raw);
                    int localPort = PST_SafePort(disp[2]);
                    if (localPort < 0) disp[8] = "-";
                    else
                    {
                        bool isListening = raw.Length > 4 && raw[4] is TcpState ls && ls == TcpState.Listen;
                        disp[8] = isListening ? PST_RiskText(raw, localPort) : "-";
                    }
                }
                return disp;
            }
            catch
            {
                try { return (object[])raw.Clone(); } catch { return new object[0]; }
            }
        }
        private void PST_RefreshView()
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { Invoke(new Action(PST_RefreshView)); } catch { }
                    return;
                }
                if (!IsHandleCreated) return;
                string searchText = "";
                try { searchText = PST_TextBox.Text.Trim(); } catch { }
                bool empty = string.IsNullOrEmpty(searchText);
                try { PST_TextBoxClearBtn.Enabled = !empty; } catch { }
                string[] words = empty ? new string[0] : PST_SplitWords(searchText);
                var rows = new List<object[]>();
                bool reverse = false;
                bool openOnly = false;
                try { reverse = PST_SortMode.Checked; } catch { }
                try { openOnly = PST_OpenOnly.Checked; } catch { }
                PST_Mode mode = PST_currentMode;
                lock (PST_masterRows)
                {
                    for (int i = 0; i < PST_masterRows.Count; i++)
                    {
                        int idx = reverse ? PST_masterRows.Count - 1 - i : i;
                        var rawRow = PST_masterRows[idx];
                        if (openOnly && mode == PST_Mode.Local)
                        {
                            if (rawRow == null || rawRow.Length < 5) continue;
                            if (!(rawRow[4] is TcpState st && st == TcpState.Listen)) continue;
                        }
                        var disp = PST_RenderRow(rawRow);
                        if (disp.Length != PST_DataTable.Columns.Count) continue;
                        if (PST_RowMatches(disp, words)) rows.Add(disp);
                    }
                }
                try { PST_DataTable.SuspendLayout(); } catch { }
                try
                {
                    PST_DataTable.Rows.Clear();
                    foreach (var r in rows)
                    {
                        try { PST_DataTable.Rows.Add(r); }
                        catch
                        {
                            // Skip rows that no longer fit the column layout instead of crashing.
                            continue;
                        }
                    }
                }
                finally
                {
                    try { PST_DataTable.ResumeLayout(); } catch { }
                }
                try
                {
                    PST_portToGridRow.Clear();
                    foreach (DataGridViewRow gr in PST_DataTable.Rows)
                    {
                        if (gr != null && gr.Cells.Count > 0)
                        {
                            int gp = PST_SafePort(gr.Cells[0].Value);
                            if (gp >= 0 && !PST_portToGridRow.ContainsKey(gp)) PST_portToGridRow[gp] = gr;
                        }
                    }
                }
                catch { }
                PST_ClearGridSelection();
                PST_ColorRiskCells();
                PST_UpdateCount(rows.Count, PST_MasterCount());
                PST_UpdateIpInfoBtn();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_RefreshView()"); }
            }
        }
        private void PST_PaintRiskCell(DataGridViewRow row, int riskColumn, string high, string medium, Color normal)
        {
            try
            {
                if (row == null || row.Cells.Count <= riskColumn) return;
                string value = Convert.ToString(row.Cells[riskColumn].Value);
                if (value == high) row.Cells[riskColumn].Style.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "AccentRed");
                else if (value == medium) row.Cells[riskColumn].Style.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "AccentOrange");
                else row.Cells[riskColumn].Style.ForeColor = normal;
            }
            catch { }
        }
        private void PST_ColorRiskCells()
        {
            try
            {
                int riskColumn = PST_currentMode == PST_Mode.Remote ? 4 : 8;
                if (PST_DataTable.Columns.Count <= riskColumn) return;
                string high = PST_Text("pst_risk_high");
                string medium = PST_Text("pst_risk_medium");
                Color normal = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1");
                foreach (DataGridViewRow row in PST_DataTable.Rows)
                {
                    PST_PaintRiskCell(row, riskColumn, high, medium, normal);
                }
            }
            catch { }
        }
        private void PST_UpdateCount(int visible, int total)
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { Invoke(new Action(() => PST_UpdateCount(visible, total))); } catch { }
                    return;
                }
                if (!IsHandleCreated) return;
                PST_TotalCount_V.Text = visible + " / " + total;
            }
            catch { }
        }
        private void PST_ClearGridSelection()
        {
            try { PST_DataTable.ClearSelection(); } catch { }
            try { PST_DataTable.CurrentCell = null; } catch { }
        }
        // ESC anywhere on the form drops the grid selection (button state follows via SelectionChanged).
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                try { PST_ClearGridSelection(); } catch { }
                try { PST_UpdateIpInfoBtn(); } catch { }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void PST_TextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (PST_filterTimer == null)
                {
                    PST_filterTimer = this.components != null ? new System.Windows.Forms.Timer(this.components) : new System.Windows.Forms.Timer();
                    PST_filterTimer.Interval = 300;
                    PST_filterTimer.Tick += (s, ev) =>
                    {
                        try
                        {
                            PST_filterTimer.Stop();
                            PST_DataTable.CurrentCell = null;
                            PST_RefreshView();
                            if (PST_DataTable.Rows.Count > 0)
                            {
                                try { PST_DataTable.FirstDisplayedScrollingRowIndex = 0; } catch { }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_TextBox_TextChanged()"); }
                        }
                    };
                }
                PST_filterTimer.Stop();
                PST_filterTimer.Start();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_TextBox_TextChanged()"); }
            }
        }
        private void PST_TextBoxClearBtn_Click(object sender, EventArgs e)
        {
            try
            {
                PST_TextBox.Text = string.Empty;
                PST_TextBox.Focus();
                if (PST_DataTable.Rows.Count > 0) PST_DataTable.FirstDisplayedScrollingRowIndex = 0;
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_TextBoxClearBtn_Click()"); }
            }
        }
        private void PST_SortMode_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                PST_RefreshView();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_SortMode_CheckedChanged()"); }
            }
        }
        private void PST_OpenOnly_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                PST_RefreshView();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_OpenOnly_CheckedChanged()"); }
            }
        }
        private void PST_DataTable_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= PST_DataTable.Rows.Count) return;
                DataGridViewRow row = PST_DataTable.Rows[e.RowIndex];
                if (row == null) return;
                var parts = new List<string>();
                foreach (DataGridViewCell cell in row.Cells) parts.Add(Convert.ToString(cell.Value));
                string id = parts.Count > 0 ? parts[0] : string.Empty;
                Clipboard.SetText(string.Join(" | ", parts));
                TS_MessageBoxEngine.TS_MessageBox(this, 1, string.Format(PST_Text("pst_copy_success"), id));
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_DataTable_CellDoubleClick()"); }
                try
                {
                    TS_MessageBoxEngine.TS_MessageBox(this, 3, PST_Text("pst_copy_failed"));
                }
                catch { }
            }
        }
        private void PST_DataTable_SelectionChanged(object sender, EventArgs e)
        {
            try { PST_UpdateIpInfoBtn(); }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_DataTable_SelectionChanged()"); }
            }
        }
        // IP info button: enabled only when the selected row resolves to an external
        // (public) address. Loopback, private, link-local and other reserved ranges
        // stay disabled, since a public lookup service has no info for them.
        // Opens the same secure provider the Network section uses (ipinfo.io)
        // in the default browser.
        private const string PST_IPINFO_BASE = "https://ipinfo.io/";
        private void PST_UpdateIpInfoBtn()
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { Invoke(new Action(PST_UpdateIpInfoBtn)); } catch { }
                    return;
                }
                if (!IsHandleCreated) return;
                try { PST_IpInfoBtn.Enabled = !string.IsNullOrEmpty(PST_GetSelectedRemoteIp()); }
                catch { try { PST_IpInfoBtn.Enabled = false; } catch { } }
            }
            catch { }
        }
        private string PST_GetSelectedRemoteIp()
        {
            try
            {
                if (PST_DataTable == null || PST_DataTable.Rows.Count == 0) return null;
                DataGridViewRow row = null;
                try
                {
                    if (PST_DataTable.SelectedRows.Count > 0) row = PST_DataTable.SelectedRows[0];
                    else row = PST_DataTable.CurrentRow;
                }
                catch { row = null; }
                if (row == null || row.Index < 0) return null;
                if (PST_currentMode == PST_Mode.Remote)
                {
                    string host = (PST_lastHost ?? "").Trim();
                    if (string.IsNullOrEmpty(host)) return null;
                    if (IPAddress.TryParse(host, out IPAddress hip))
                    {
                        return PST_IsPublicIP(hip) ? hip.ToString() : null;
                    }
                    // Hostname target: public verdict was computed at scan-confirm time
                    // (resolving here would block the UI thread on every selection).
                    if (!PST_lastHostIsPublic) return null;
                    if (host.IndexOf(' ') >= 0 || host.Length < 3) return null;
                    return host;
                }
                // Local mode, priority chain: remote endpoint -> bound local address.
                // Listeners report remote as "0.0.0.0:0" and UDP rows as "-", so the
                // remote cell alone would leave the button dead on most rows.
                if (row.Cells.Count > 3)
                {
                    string remoteCell = Convert.ToString(row.Cells[3].Value);
                    if (PST_TryParseRemoteCell(remoteCell, out string rip) && IPAddress.TryParse(rip, out IPAddress ra) && PST_IsPublicIP(ra)) return rip;
                }
                if (row.Cells.Count > 1)
                {
                    string localCell = Convert.ToString(row.Cells[1].Value);
                    if (PST_TryParseBareIp(localCell, false, out string lip) && IPAddress.TryParse(lip, out IPAddress la) && PST_IsPublicIP(la)) return lip;
                }
                return null;
            }
            catch { return null; }
        }
        // External (globally routable) addresses only: loopback, private, link-local,
        // CGNAT, documentation, multicast and reserved ranges are not public.
        private static bool PST_IsPublicIP(IPAddress ip)
        {
            try
            {
                if (ip == null) return false;
                if (ip.AddressFamily == AddressFamily.InterNetworkV6) return PST_IsIPv6Global(ip);
                if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
                byte[] b = ip.GetAddressBytes();
                if (b == null || b.Length != 4) return false;
                if (b[0] == 0 || b[0] == 127 || b[0] >= 224) return false;
                if (b[0] == 10) return false;
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;
                if (b[0] == 192 && b[1] == 168) return false;
                if (b[0] == 169 && b[1] == 254) return false;
                if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return false;
                if (b[0] == 192 && b[1] == 0 && b[2] == 2) return false;
                if (b[0] == 198 && b[1] == 51 && b[2] == 100) return false;
                if (b[0] == 203 && b[1] == 0 && b[2] == 113) return false;
                if (b[0] == 192 && b[1] == 88 && b[2] == 99) return false;
                return true;
            }
            catch { return false; }
        }
        private static bool PST_TryParseBareIp(string text, bool allowWildcard, out string ip)
        {
            ip = null;
            try
            {
                if (string.IsNullOrWhiteSpace(text)) return false;
                text = text.Trim();
                if (text == "-") return false;
                int pct = text.IndexOf('%');
                if (pct >= 0) text = text.Substring(0, pct);
                text = text.Trim().Trim('[', ']').Trim();
                if (!IPAddress.TryParse(text, out IPAddress parsed)) return false;
                if (!allowWildcard && (parsed.Equals(IPAddress.Any) || parsed.Equals(IPAddress.IPv6Any) || parsed.Equals(IPAddress.None) || parsed.Equals(IPAddress.IPv6None))) return false;
                ip = parsed.ToString();
                return true;
            }
            catch { ip = null; return false; }
        }
        private static bool PST_TryParseRemoteCell(string cell, out string ip)
        {
            ip = null;
            try
            {
                if (string.IsNullOrWhiteSpace(cell)) return false;
                cell = cell.Trim();
                if (cell == "-") return false;
                string hostPart = cell;
                // Strip brackets ("[::1]:80" -> "::1") then split off the trailing ":port".
                if (hostPart.StartsWith("[", StringComparison.Ordinal))
                {
                    int end = hostPart.IndexOf(']');
                    if (end > 1) hostPart = hostPart.Substring(1, end - 1).Trim();
                }
                else
                {
                    int lastColon = hostPart.LastIndexOf(':');
                    if (lastColon > 0 && lastColon == hostPart.IndexOf(':'))
                    {
                        // Single colon: IPv4 "IP:port".
                        hostPart = hostPart.Substring(0, lastColon).Trim();
                    }
                    else if (lastColon > 0)
                    {
                        // Multiple colons: IPv6 with trailing port ("fe80::1:443").
                        // Try the part before the last colon first; fall back to the full text.
                        string cut = hostPart.Substring(0, lastColon).TrimEnd(':').Trim();
                        if (IPAddress.TryParse(cut, out IPAddress cutIp)) hostPart = cut;
                    }
                }
                int pct = hostPart.IndexOf('%');
                if (pct >= 0) hostPart = hostPart.Substring(0, pct);
                hostPart = hostPart.Trim().Trim('[', ']').Trim();
                if (!IPAddress.TryParse(hostPart, out IPAddress parsed)) return false;
                if (parsed.Equals(IPAddress.Any) || parsed.Equals(IPAddress.IPv6Any) || parsed.Equals(IPAddress.None) || parsed.Equals(IPAddress.IPv6None)) return false;
                ip = parsed.ToString();
                return true;
            }
            catch { ip = null; return false; }
        }
        private void PST_IpInfoBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string target = PST_GetSelectedRemoteIp();
                if (string.IsNullOrEmpty(target))
                {
                    TS_MessageBoxEngine.TS_MessageBox(this, 2, PST_Text("pst_ipinfo_invalid"));
                    PST_UpdateIpInfoBtn();
                    return;
                }
                string url = PST_IPINFO_BASE + Uri.EscapeDataString(target);
                DialogResult confirm = TS_MessageBoxEngine.TS_MessageBox(this, 6, string.Format(PST_Text("pst_ipinfo_confirm"), target, url, "\n\n"));
                if (confirm != DialogResult.Yes) return;
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_IpInfoBtn_Click()"); }
                    try { TS_MessageBoxEngine.TS_MessageBox(this, 3, PST_Text("pst_ipinfo_open_failed") + "\n\n" + ex.Message); } catch { }
                }
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_IpInfoBtn_Click()"); }
            }
        }
        // Local scan.
        private async void PST_OpenPortsBtn_Click(object sender, EventArgs e)
        {
            if (PST_isRunning)
            {
                // Second click while busy cancels the running scan (no extra stop button/strings).
                // Bump the generation first so late in-flight callbacks cannot paint stale rows.
                try { Interlocked.Increment(ref PST_scanGen); } catch { }
                try { PST_cts?.Cancel(); } catch { }
                return;
            }
            PST_InitRemoteInputs();
            try { if (!string.IsNullOrEmpty(PST_TextBox.Text)) PST_TextBox.Text = string.Empty; } catch { }
            PST_refreshRepeat = true;
            await PST_LoadLocalAsync();
        }
        private async Task PST_LoadLocalAsync()
        {
            if (PST_isRunning) return;
            PST_isRunning = true;
            GlowMain.PortScannerMode = true;
            Interlocked.Increment(ref PST_scanGen);
            PST_SetBusy(true);
            PST_StartScanClock();
            CancellationToken ct = PST_NewToken();
            try
            {
                // Fetch first so the grid keeps old data instead of flashing empty.
                var result = await Task.Run(() =>
                {
                    var tcp = PST_PortHelper.GetAllTcpConnections();
                    var udp = PST_PortHelper.GetAllUdpListeners();
                    return new { tcp, udp };
                }, ct);
                if (IsDisposed || !IsHandleCreated) return;
                ct.ThrowIfCancellationRequested();
                var rows = new List<object[]>();
                foreach (var c in result.tcp.OrderBy(x => x.LocalPort))
                {
                    rows.Add(new object[] { "TCP", c.LocalAddress.ToString(), c.LocalPort, c.RemoteAddress.ToString() + ":" + c.RemotePort, c.State, c.Pid, c.ProcessName });
                }
                foreach (var u in result.udp.OrderBy(x => x.LocalPort))
                {
                    rows.Add(new object[] { "UDP", u.LocalAddress.ToString(), u.LocalPort, "-", TcpState.Listen, u.Pid, u.ProcessName });
                }
                PST_SetupColumns(PST_Mode.Local);
                lock (PST_masterRows)
                {
                    PST_masterRows.Clear();
                    PST_masterRows.AddRange(rows);
                    PST_RebuildPortIndex();
                }
                PST_ApplySnapshot();
                PST_RefreshView();
            }
            catch (OperationCanceledException)
            {
                // Cancelled on close, stay silent.
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_LoadLocalAsync()"); }
                if (!IsDisposed && IsHandleCreated)
                    TS_MessageBoxEngine.TS_MessageBox(this, 3, PST_Text("pst_list_error") + "\n\n" + ex.Message);
            }
            finally
            {
                PST_FinishScan();
            }
        }
        // Remote scan.
        private async void PST_RemotePortBtn_Click(object sender, EventArgs e)
        {
            if (PST_isRunning)
            {
                try { Interlocked.Increment(ref PST_scanGen); } catch { }
                try { PST_cts?.Cancel(); } catch { }
                return;
            }
            string host = PST_NormalizeHost(PST_ReadText(PST_RemoteHostTextBox, PST_DEFAULT_HOST));
            string portsText = PST_ReadText(PST_RemotePortsTextBox, PST_DEFAULT_PORTS);
            int timeout;
            {
                if (!int.TryParse(PST_ReadText(PST_RemoteTimeoutTextBox, PST_DEFAULT_TIMEOUT.ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedTimeout) || parsedTimeout <= 0)
                    parsedTimeout = PST_DEFAULT_TIMEOUT;
                if (parsedTimeout < PST_MIN_TIMEOUT) parsedTimeout = PST_MIN_TIMEOUT;
                if (parsedTimeout > PST_MAX_TIMEOUT) parsedTimeout = PST_MAX_TIMEOUT;
                timeout = parsedTimeout;
                try { PST_RemoteTimeoutTextBox.Text = timeout.ToString(CultureInfo.InvariantCulture); } catch { }
            }
            List<int> parsed;
            try
            {
                parsed = PST_ParsePorts(portsText);
            }
            catch (Exception ex)
            {
                TS_MessageBoxEngine.TS_MessageBox(this, 2, PST_Text("pst_invalid_ports") + "\n\n" + ex.Message);
                return;
            }
            int portCount = parsed.Count;
            try
            {
                // Show the effective values: refill blanks, reflect normalization.
                if (PST_RemoteHostTextBox.Text != host) PST_RemoteHostTextBox.Text = host;
                if (PST_RemotePortsTextBox.Text != portsText) PST_RemotePortsTextBox.Text = portsText;
            }
            catch { }
            if (portCount > PST_MAX_PORTS)
            {
                string msg;
                try { msg = string.Format(PST_Text("pst_err_too_many"), portCount, PST_MAX_PORTS); }
                catch { msg = PST_Text("pst_err_too_many"); }
                TS_MessageBoxEngine.TS_MessageBox(this, 2, msg);
                return;
            }
            if (!await PST_ConfirmRemoteTargetAsync(host, portCount)) return;
            // Loopback targets work offline; only gate non-loopback scans on OS network status.
            if (!PST_IsLoopbackHost(host) && !PST_IsNetworkAvailable())
            {
                TS_MessageBoxEngine.TS_MessageBox(this, 2, PST_Text("pst_no_network"));
                return;
            }
            PST_refreshRepeat = true;
            PST_lastHost = host;
            try { if (!string.IsNullOrEmpty(PST_TextBox.Text)) PST_TextBox.Text = string.Empty; } catch { }
            await PST_RunRemoteAsync(host, portsText, timeout);
        }
        private static string PST_NormalizeHost(string host)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(host)) return PST_DEFAULT_HOST;
                host = host.Trim();
                // Bracketed IPv6, optionally with port: "[::1]" or "[::1]:80".
                if (host.StartsWith("[", StringComparison.Ordinal))
                {
                    int end = host.IndexOf(']');
                    if (end > 1) host = host.Substring(1, end - 1).Trim();
                }
                // Pasted URLs: strip the "scheme://" prefix and any path/query/fragment.
                int scheme = host.IndexOf("://", StringComparison.Ordinal);
                if (scheme >= 0) host = host.Substring(scheme + 3);
                int cut = host.IndexOfAny(new[] { '/', '?', '#' });
                if (cut >= 0) host = host.Substring(0, cut);
                // "host:port" with a single colon only, so IPv6 stays untouched.
                int firstColon = host.IndexOf(':');
                if (firstColon >= 0 && firstColon == host.LastIndexOf(':'))
                {
                    if (int.TryParse(host.Substring(firstColon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int dummy))
                        host = host.Substring(0, firstColon).Trim();
                }
                host = host.Trim();
                return string.IsNullOrEmpty(host) ? PST_DEFAULT_HOST : host;
            }
            catch { return PST_DEFAULT_HOST; }
        }
        private static string PST_ReadText(TextBox box, string fallback)
        {
            try
            {
                string v = box.Text.Trim();
                return string.IsNullOrEmpty(v) ? fallback : v;
            }
            catch
            {
                return fallback;
            }
        }
        private bool PST_IsNetworkAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return true;
            }
        }
        private async Task PST_RunRemoteAsync(string host, string portsText, int timeout)
        {
            if (PST_isRunning) return;
            List<int> ports;
            try
            {
                ports = PST_ParsePorts(portsText);
            }
            catch (Exception ex)
            {
                if (!IsDisposed && IsHandleCreated)
                    TS_MessageBoxEngine.TS_MessageBox(this, 2, PST_Text("pst_invalid_ports") + "\n\n" + ex.Message);
                return;
            }
            PST_isRunning = true;
            GlowMain.PortScannerMode = true;
            int myGen = Interlocked.Increment(ref PST_scanGen);
            string myHost = host;
            PST_SetBusy(true);
            PST_StartScanClock();
            CancellationToken ct = PST_NewToken();
            try
            {
                PST_SetupColumns(PST_Mode.Remote);
                // Do NOT reset the diff snapshot here: PST_ApplySnapshot() already baselines
                // on first scan / mode switch / host change. Resetting every run forces the
                // baseline branch and makes added/removed unreachable.
                lock (PST_masterRows) { PST_masterRows.Clear(); PST_RebuildPortIndex(); }
                PST_banners.Clear();
                var sem = new SemaphoreSlim(PST_SEM_LIMIT);
                try
                {
                    var tasks = new List<Task>();
                    foreach (int port in ports)
                    {
                        int p = port;
                        await sem.WaitAsync(ct);
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                if (await PST_IsOpenAsync(host, p, timeout, ct))
                                {
                                    if (IsDisposed || !IsHandleCreated) return;
                                    ct.ThrowIfCancellationRequested();
                                    int genCopy = myGen;
                                    string hostCopy = myHost;
                                    _ = PST_GrabBannerAsync(hostCopy, p, Math.Min(timeout, 2000), ct, genCopy);
                                    try
                                    {
                                        BeginInvoke(new Action(() =>
                                        {
                                            try
                                            {
                                                if (IsDisposed || !IsHandleCreated) return;
                                                if (PST_currentMode != PST_Mode.Remote) return;
                                                if (genCopy != PST_scanGen) return;
                                                if (!string.Equals((PST_lastHost ?? "").Trim(), (hostCopy ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) return;
                                                object[] raw = new object[] { p, true, PST_GetServiceName(p), null, "-" };
                                                lock (PST_masterRows) { PST_masterRows.Add(raw); try { PST_portToMasterIndex[p] = PST_masterRows.Count - 1; } catch { } }
                                                string q = "";
                                                try { q = PST_TextBox.Text.Trim(); } catch { }
                                                string[] words = PST_SplitWords(q);
                                                object[] disp = PST_RenderRow(raw);
                                                if (disp.Length != PST_DataTable.Columns.Count) return;
                                                if (PST_RowMatches(disp, words))
                                                {
                                                    try
                                                    {
                                                        int newIdx;
                                                        if (PST_SortMode.Checked) { PST_DataTable.Rows.Insert(0, disp); newIdx = 0; }
                                                        else newIdx = PST_DataTable.Rows.Add(disp);
                                                        try { PST_PaintRiskCell(PST_DataTable.Rows[newIdx], 4, PST_Text("pst_risk_high"), PST_Text("pst_risk_medium"), TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1")); } catch { }
                                                    }
                                                    catch { return; }
                                                    PST_ClearGridSelection();
                                                    PST_UpdateCount(PST_DataTable.Rows.Count, PST_MasterCount());
                                                }
                                                else
                                                {
                                                    PST_UpdateCount(PST_DataTable.Rows.Count, PST_MasterCount());
                                                }
                                            }
                                            catch { }
                                        }));
                                    }
                                    catch { }
                                }
                            }
                            catch (OperationCanceledException) { }
                            catch { }
                            finally
                            {
                                try { sem.Release(); } catch { }
                            }
                        }, ct));
                    }
                    await Task.WhenAll(tasks);
                }
                finally
                {
                    try { sem.Dispose(); } catch { }
                }
                if (IsDisposed || !IsHandleCreated) return;
                ct.ThrowIfCancellationRequested();
                lock (PST_masterRows)
                {
                    PST_masterRows.Sort((a, b) => PST_SafePort(a != null && a.Length > 0 ? a[0] : null).CompareTo(PST_SafePort(b != null && b.Length > 0 ? b[0] : null)));
                    PST_RebuildPortIndex();
                }
                PST_ApplySnapshot();
                PST_RefreshView();
            }
            catch (OperationCanceledException)
            {
                // Cancelled, stay silent.
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_RunRemoteAsync()"); }
                if (!IsDisposed && IsHandleCreated)
                    TS_MessageBoxEngine.TS_MessageBox(this, 3, PST_Text("pst_scan_error") + "\n\n" + ex.Message);
            }
            finally
            {
                PST_FinishScan();
            }
        }
        private void PST_FinishScan()
        {
            PST_isRunning = false;
            GlowMain.PortScannerMode = false;
            PST_refreshRepeat = true;
            PST_StopScanClock();
            if (!IsDisposed && IsHandleCreated) PST_SetBusy(false);
        }
        private void PST_ApplyStopLook()
        {
            // Shared by PST_SetBusy(true) and the Preloader's mid-scan language refresh.
            try
            {
                string stopLabel = " " + PST_Text("pst_stop");
                PST_OpenPortsBtn.Text = stopLabel;
                PST_RemotePortBtn.Text = stopLabel;
                TSImageRenderer(PST_OpenPortsBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_stop_light : Properties.Resources.ct_test_stop_dark, 18, ContentAlignment.MiddleRight);
                TSImageRenderer(PST_RemotePortBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_stop_light : Properties.Resources.ct_test_stop_dark, 18, ContentAlignment.MiddleRight);
            }
            catch { }
        }
        private void PST_ApplyScanLook()
        {
            try
            {
                PST_OpenPortsBtn.Text = " " + PST_Text("pst_local_btn");
                PST_RemotePortBtn.Text = " " + PST_Text("pst_remote_btn");
                TSImageRenderer(PST_OpenPortsBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_start_light : Properties.Resources.ct_test_start_dark, 18, ContentAlignment.MiddleRight);
                TSImageRenderer(PST_RemotePortBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_start_light : Properties.Resources.ct_test_start_dark, 18, ContentAlignment.MiddleRight);
            }
            catch { }
        }
        private void PST_UpdateAutoRefreshBtn()
        {
            try
            {
                if (PST_AutoRefreshBtn == null || PST_AutoRefreshBtn.IsDisposed) return;
                if (InvokeRequired)
                {
                    try { Invoke(new Action(PST_UpdateAutoRefreshBtn)); } catch { }
                    return;
                }
                if (PST_autoRefreshEnabled)
                {
                    PST_AutoRefreshBtn.Text = " " + PST_Text("pst_autorefresh_stop");
                    TSImageRenderer(PST_AutoRefreshBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_stop_light : Properties.Resources.ct_test_stop_dark, 18, ContentAlignment.MiddleRight);
                }
                else
                {
                    PST_AutoRefreshBtn.Text = " " + PST_Text("pst_autorefresh_start");
                    TSImageRenderer(PST_AutoRefreshBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_start_light : Properties.Resources.ct_test_start_dark, 18, ContentAlignment.MiddleRight);
                }
            }
            catch { }
        }
        private void PST_AutoRefreshBtn_Click(object sender, EventArgs e)
        {
            try
            {
                PST_autoRefreshEnabled = !PST_autoRefreshEnabled;
                if (PST_autoRefreshEnabled)
                {
                    PST_refreshRepeat = true;
                }
                else
                {
                    PST_refreshRepeat = false;
                    try
                    {
                        string t = PST_baseTitle;
                        string snap = PST_SnapshotSummary;
                        if (!IsDisposed && IsHandleCreated)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                if (IsDisposed || !IsHandleCreated) return;
                                if (PST_isRunning) return;
                                Text = string.IsNullOrEmpty(snap) ? t : t + " | " + snap;
                            }));
                        }
                    }
                    catch { }
                }
                PST_UpdateAutoRefreshBtn();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_AutoRefreshBtn_Click()"); }
            }
        }
        private void PST_SetBusy(bool busy)
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { Invoke(new Action(() => PST_SetBusy(busy))); } catch { }
                    return;
                }
                if (!IsHandleCreated) return;
                if (busy)
                {
                    try
                    {
                        if (PST_localBtnText == null) PST_localBtnText = PST_OpenPortsBtn.Text;
                        if (PST_remoteBtnText == null) PST_remoteBtnText = PST_RemotePortBtn.Text;
                    }
                    catch { }
                    // Second click cancels: show a real localized Stop label + stop icon.
                    PST_ApplyStopLook();
                }
                else
                {
                    try
                    {
                        if (PST_localBtnText == null && PST_remoteBtnText == null)
                        {
                            PST_ApplyScanLook();
                        }
                        else
                        {
                            if (PST_localBtnText != null) { PST_OpenPortsBtn.Text = PST_localBtnText; PST_localBtnText = null; }
                            if (PST_remoteBtnText != null) { PST_RemotePortBtn.Text = PST_remoteBtnText; PST_remoteBtnText = null; }
                            TSImageRenderer(PST_OpenPortsBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_start_light : Properties.Resources.ct_test_start_dark, 18, ContentAlignment.MiddleRight);
                            TSImageRenderer(PST_RemotePortBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_start_light : Properties.Resources.ct_test_start_dark, 18, ContentAlignment.MiddleRight);
                        }
                    }
                    catch { }
                }
                PST_OpenPortsBtn.Enabled = true;
                PST_RemotePortBtn.Enabled = true;
                PST_ExportBtn.Enabled = !busy;
                // Scan parameters are captured at start; changing them mid-scan has no effect.
                try
                {
                    PST_RemoteHostTextBox.Enabled = !busy;
                    PST_RemotePortsTextBox.Enabled = !busy;
                    PST_RemoteTimeoutTextBox.Enabled = !busy;
                }
                catch { }
            }
            catch { }
        }
        // Scan elapsed time shown in the title.
        private void PST_SetTitle(string suffix)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    Text = string.IsNullOrEmpty(suffix) ? PST_baseTitle : PST_baseTitle + " | " + suffix;
                }));
            }
            catch { }
        }
        private void PST_StartScanClock()
        {
            PST_scanStart = DateTime.Now;
        }
        private void PST_StopScanClock()
        {
            PST_scanStart = DateTime.MinValue;
        }
        private string PST_GetElapsedText()
        {
            try
            {
                if (PST_scanStart == DateTime.MinValue) return string.Empty;
                int seconds = (int)(DateTime.Now - PST_scanStart).TotalSeconds;
                if (seconds < 0) seconds = 0;
                return " " + string.Format(PST_Text("pst_elapsed"), seconds);
            }
            catch
            {
                return string.Empty;
            }
        }
        private CancellationToken PST_NewToken()
        {
            // Disposing a CTS while its token is still in use throws, so cancel first.
            // In-flight connect/banner tasks are invalidated separately via the
            // cancellation token and the scan generation id.
            try
            {
                var old = PST_cts;
                PST_cts = null;
                if (old != null)
                {
                    try { old.Cancel(); } catch { }
                    try { old.Dispose(); } catch { }
                }
            }
            catch { }
            PST_cts = new CancellationTokenSource();
            return PST_cts.Token;
        }
        // Auto-refresh: re-runs the local scan on a countdown; remote targets never auto-run.
        // The user toggle (PST_autoRefreshEnabled) pauses the countdown; PST_autoRefresh stays
        // as the loop lifetime flag (false only on form close).
        private async Task PST_AutoRefreshLoopAsync()
        {
            try
            {
                while (PST_autoRefresh)
                {
                    int counter = PST_REFRESH_INTERVAL;
                    while (counter >= 0 && PST_autoRefresh)
                    {
                        if (PST_refreshRepeat) { PST_refreshRepeat = false; break; }
                        if (!PST_isRunning && (PST_currentMode == PST_Mode.Remote || !PST_autoRefreshEnabled))
                        {
                            // Remote never auto-scans and a disabled toggle shows a steady title
                            // instead of a misleading countdown.
                            if (!IsDisposed && IsHandleCreated)
                            {
                                string t = PST_baseTitle;
                                string snap = PST_SnapshotSummary;
                                BeginInvoke(new Action(() =>
                                {
                                    if (IsDisposed || !IsHandleCreated) return;
                                    Text = string.IsNullOrEmpty(snap) ? t : t + " | " + snap;
                                }));
                            }
                            await Task.Delay(1000);
                            continue;
                        }
                        string fmt = PST_Text("pst_refresh_title").Trim();
                        string refreshing = PST_Text("pst_refreshing_title").Trim();
                        if (!PST_isRunning)
                        {
                            if (!IsDisposed && IsHandleCreated)
                            {
                                int n = counter;
                                string t = PST_baseTitle;
                                string snap = PST_SnapshotSummary;
                                BeginInvoke(new Action(() =>
                                {
                                    if (IsDisposed || !IsHandleCreated) return;
                                    string head = n != 0 ? t + " | " + string.Format(fmt, n) : t + " | " + refreshing;
                                    if (!string.IsNullOrEmpty(snap)) head += " | " + snap;
                                    Text = head;
                                }));
                            }
                            if (counter == 0)
                            {
                                if (PST_autoRefreshEnabled && !PST_isRunning && PST_currentMode == PST_Mode.Local)
                                {
                                    // Remote targets are never re-scanned automatically (IDS/rate-limit risk).
                                    _ = PST_LoadLocalAsync();
                                }
                                break;
                            }
                            counter--;
                        }
                        else
                        {
                            PST_SetTitle(PST_Text("pst_scanning") + PST_GetElapsedText());
                        }
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_AutoRefreshLoopAsync()"); }
            }
        }
        // TXT export: always the full result (master rows), never just the filtered grid view.
        private void PST_ExportBtn_Click(object sender, EventArgs e)
        {
            try
            {
                List<object[]> snapshot;
                lock (PST_masterRows) { snapshot = PST_masterRows.ToList(); }
                if (snapshot.Count == 0)
                {
                    TS_MessageBoxEngine.TS_MessageBox(this, 2, PST_Text("pst_export_empty"));
                    return;
                }
                bool filtered = false;
                try
                {
                    filtered = !string.IsNullOrWhiteSpace(PST_TextBox.Text)
                        || (PST_currentMode == PST_Mode.Local && PST_OpenOnly.Checked);
                }
                catch { }
                var langPrint = new TSGetLangs(GlowMain.lang_path);
                var lines = new List<string>();
                string exportName = PST_Text("pst_export_name");
                string reportTitle = string.Format(langPrint.TSReadLangs("PrintEngine", "pe_save_name"), exportName);
                lines.Add(Application.ProductName + " - " + reportTitle);
                lines.Add(string.Empty);
                lines.Add(new string('-', 60));
                lines.Add(string.Empty);
                if (PST_currentMode == PST_Mode.Remote) lines.Add(PST_Text("pst_host_label") + " " + (PST_lastHost ?? "-"));
                string modeName = PST_currentMode == PST_Mode.Remote ? PST_Text("pst_exp_mode_remote") : PST_Text("pst_exp_mode_local");
                lines.Add(PST_Text("pst_exp_mode") + " " + modeName);
                if (!string.IsNullOrEmpty(PST_SnapshotSummary)) lines.Add(PST_SnapshotSummary);
                lines.Add(PST_Text("pst_exp_rows") + " " + snapshot.Count + (filtered ? " " + PST_Text("pst_exp_rows_note") : ""));
                lines.Add(string.Empty);
                var headers = new List<string>();
                try
                {
                    foreach (DataGridViewColumn col in PST_DataTable.Columns) headers.Add(col.HeaderText);
                }
                catch { }
                foreach (var raw in snapshot)
                {
                    object[] disp;
                    try { disp = PST_RenderRow(raw); }
                    catch { continue; }
                    var cells = new List<string>();
                    for (int i = 0; i < disp.Length; i++)
                    {
                        string h = (i < headers.Count && !string.IsNullOrEmpty(headers[i])) ? headers[i] : ("c" + i);
                        string v = Convert.ToString(disp[i]);
                        if (string.IsNullOrEmpty(v)) v = "-";
                        cells.Add(h + ": " + v);
                    }
                    lines.Add(string.Join(" | ", cells));
                }
                lines.Add(string.Empty);
                lines.Add(new string('-', 60));
                lines.Add(string.Empty);
                // Same footer shape as the main Glow export.
                string versionLabel = Application.ProductName + " " + langPrint.TSReadLangs("PrintEngine", "pe_version");
                string versionValue = TS_VersionEngine.TS_SoftwareVersion(1);
                if (!string.IsNullOrWhiteSpace(versionValue)) lines.Add(versionLabel + " " + versionValue);
                lines.Add(TS_SoftwareCopyrightDate.ts_scd_preloader);
                string websiteLabel = langPrint.TSReadLangs("PrintEngine", "pe_website");
                string githubLabel = langPrint.TSReadLangs("PrintEngine", "pe_github");
                string donateLabel = langPrint.TSReadLangs("PrintEngine", "pe_donate");
                if (!string.IsNullOrWhiteSpace(websiteLabel)) lines.Add(websiteLabel + " " + TS_LinkSystem.website_link);
                if (!string.IsNullOrWhiteSpace(githubLabel)) lines.Add(githubLabel + " " + TS_LinkSystem.github_link);
                if (!string.IsNullOrWhiteSpace(donateLabel)) lines.Add(donateLabel + " " + TS_LinkSystem.ts_donate);
                string dirTitle = langPrint.TSReadLangs("PrintEngine", "pe_save_directory");
                string txtFilter = langPrint.TSReadLangs("PrintEngine", "pe_save_txt");
                using (SaveFileDialog saveDlg = new SaveFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Title = Application.ProductName + " - " + dirTitle,
                    DefaultExt = "txt",
                    FileName = Application.ProductName + " - " + reportTitle + " - " + DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss"),
                    Filter = txtFilter + " (*.txt)|*.txt"
                })
                {
                    if (saveDlg.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveDlg.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
                        string okMsg = langPrint.TSReadLangs("PrintEngine", "pe_save_success");
                        string openMsg = langPrint.TSReadLangs("PrintEngine", "pe_save_info_open");
                        var res = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(okMsg + Environment.NewLine + Environment.NewLine + openMsg, Application.ProductName, saveDlg.FileName));
                        if (res == DialogResult.Yes)
                            Process.Start(saveDlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "PST_ExportBtn_Click()"); }
            }
        }
        // Port list parsing (no external dependencies).
        private static List<int> PST_ParsePorts(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(PST_StaticText("pst_err_ports_empty"));
            var result = new SortedSet<int>();
            var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawPart in parts)
            {
                string part = rawPart.Trim();
                if (part.Contains("-"))
                {
                    var r = part.Split('-');
                    if (r.Length != 2)
                        throw new ArgumentException(string.Format(PST_StaticText("pst_err_range"), part));
                    if (!int.TryParse(r[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int start)
                        || !int.TryParse(r[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int end))
                        throw new ArgumentException(string.Format(PST_StaticText("pst_err_range"), part));
                    if (start < 1 || start > 65535 || end < 1 || end > 65535)
                        throw new ArgumentException(string.Format(PST_StaticText("pst_err_range"), part));
                    if (start > end)
                    {
                        throw new ArgumentException(string.Format(PST_StaticText("pst_err_range"), part));
                    }
                    // No pre-estimate: overlapping ranges would over-count and falsely reject.
                    // The loop below caps at PST_MAX_PORTS on distinct ports instead.
                    for (int i = start; i <= end; i++)
                    {
                        result.Add(i);
                        if (result.Count > PST_MAX_PORTS)
                            throw new ArgumentException(string.Format(PST_StaticText("pst_err_too_many"), result.Count, PST_MAX_PORTS));
                    }
                }
                else
                {
                    if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int p))
                        throw new ArgumentException(string.Format(PST_StaticText("pst_err_port"), part));
                    if (p < 1 || p > 65535)
                        throw new ArgumentException(string.Format(PST_StaticText("pst_err_port"), part));
                    result.Add(p);
                    if (result.Count > PST_MAX_PORTS)
                    {
                        throw new ArgumentException(string.Format(PST_StaticText("pst_err_too_many"), result.Count, PST_MAX_PORTS));
                    }
                }
            }
            if (result.Count == 0)
                throw new ArgumentException(PST_StaticText("pst_err_none"));
            return result.ToList();
        }
        // Best-effort service banner grab.
        private static readonly int[] PST_HttpPorts = new int[] { 80, 443, 8080, 8000, 8008, 8888, 8443, 8081, 8082, 8083, 8084, 8085, 8086, 8087, 8088, 8089, 8090 };
        private static async Task<TcpClient> PST_ConnectAsync(string host, int port, int timeoutMs, CancellationToken ct)
        {
            var client = new TcpClient();
            try
            {
                var connectTask = client.ConnectAsync(host, port);
                // Observe the loser so disposal cannot surface an unobserved exception.
                _ = connectTask.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                var delayTask = Task.Delay(timeoutMs, ct);
                var winner = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);
                if (winner == delayTask) { PST_CloseClient(client); return null; }
                ct.ThrowIfCancellationRequested();
                await connectTask.ConfigureAwait(false);
                if (!client.Connected) { PST_CloseClient(client); return null; }
                return client;
            }
            catch
            {
                PST_CloseClient(client);
                return null;
            }
        }
        private static void PST_CloseClient(TcpClient client)
        {
            try { client?.Close(); } catch { }
            try { if (client != null) ((IDisposable)client).Dispose(); } catch { }
        }
        private static async Task<string> PST_ReadBannerAsync(string host, int port, int timeoutMs, CancellationToken ct)
        {
            TcpClient client = null;
            try
            {
                client = await PST_ConnectAsync(host, port, timeoutMs, ct).ConfigureAwait(false);
                if (client == null) return null;
                client.ReceiveTimeout = timeoutMs;
                client.SendTimeout = timeoutMs;
                NetworkStream stream = client.GetStream();
                if (Array.IndexOf(PST_HttpPorts, port) >= 0)
                {
                    byte[] req = Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\nHost: " + host + "\r\n\r\n");
                    await stream.WriteAsync(req, 0, req.Length, ct).ConfigureAwait(false);
                }
                byte[] buf = new byte[1024];
                var readTask = stream.ReadAsync(buf, 0, buf.Length, ct);
                _ = readTask.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                var readDelay = Task.Delay(timeoutMs, ct);
                var readWinner = await Task.WhenAny(readTask, readDelay).ConfigureAwait(false);
                if (readWinner == readDelay) return null;
                int n = await readTask.ConfigureAwait(false);
                if (n <= 0) return null;
                string text = Encoding.UTF8.GetString(buf, 0, n);
                text = text.Replace("\r\n", " | ").Replace("\r", " |").Replace("\n", " |");
                var sb = new StringBuilder(text.Length);
                foreach (char ch in text)
                {
                    if (ch >= 32 && ch < 127) sb.Append(ch);
                    else if (ch == ' ' || ch == '\t') sb.Append(' ');
                }
                string clean = sb.ToString().Trim();
                if (clean.Length > 120) clean = clean.Substring(0, 120) + "...";
                return clean.Length == 0 ? null : clean;
            }
            catch
            {
                return null;
            }
            finally
            {
                PST_CloseClient(client);
            }
        }
        private async Task PST_GrabBannerAsync(string host, int port, int timeoutMs, CancellationToken ct, int gen)
        {
            try
            {
                string banner = await PST_ReadBannerAsync(host, port, timeoutMs, ct).ConfigureAwait(false);
                if (string.IsNullOrEmpty(banner)) return;
                if (ct.IsCancellationRequested) return;
                if (gen != PST_scanGen) return;
                if (IsDisposed || !IsHandleCreated) return;
                if (PST_currentMode != PST_Mode.Remote) return;
                if (!string.Equals((PST_lastHost ?? "").Trim(), (host ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) return;
                PST_banners[port] = banner;
                // Mirror the banner into the raw row so refresh and export stay consistent.
                lock (PST_masterRows)
                {
                    bool bannerPlaced = false;
                    if (PST_portToMasterIndex.TryGetValue(port, out int hitIdx) && hitIdx >= 0 && hitIdx < PST_masterRows.Count)
                    {
                        var cand = PST_masterRows[hitIdx];
                        if (cand != null && cand.Length >= 4 && PST_SafePort(cand[0]) == port) { cand[3] = banner; bannerPlaced = true; }
                    }
                    if (!bannerPlaced)
                    {
                        for (int i = 0; i < PST_masterRows.Count; i++)
                        {
                            var r = PST_masterRows[i];
                            if (r != null && r.Length >= 4 && PST_SafePort(r[0]) == port) { r[3] = banner; try { PST_portToMasterIndex[port] = i; } catch { } }
                        }
                    }
                }
                int targetPort = port;
                string bannerCopy = banner;
                int genCopy = gen;
                string hostCopy = host;
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (IsDisposed || !IsHandleCreated) return;
                            if (PST_currentMode != PST_Mode.Remote) return;
                            if (genCopy != PST_scanGen) return;
                            if (!string.Equals((PST_lastHost ?? "").Trim(), (hostCopy ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) return;
                            if (!PST_portToGridRow.TryGetValue(targetPort, out DataGridViewRow gridHit) || gridHit == null || gridHit.Cells.Count <= 3 || PST_SafePort(gridHit.Cells[0].Value) != targetPort) gridHit = null;
                            if (gridHit != null)
                            {
                                gridHit.Cells[3].Value = bannerCopy;
                            }
                            else
                            {
                                foreach (DataGridViewRow row in PST_DataTable.Rows)
                                {
                                    if (row.Cells.Count > 3 && PST_SafePort(row.Cells[0].Value) == targetPort)
                                    {
                                        row.Cells[3].Value = bannerCopy;
                                        try { PST_portToGridRow[targetPort] = row; } catch { }
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }));
                }
                catch { }
            }
            catch { }
        }
        private static async Task<bool> PST_IsOpenAsync(string host, int port, int timeoutMs, CancellationToken ct)
        {
            var client = await PST_ConnectAsync(host, port, timeoutMs, ct).ConfigureAwait(false);
            if (client == null) return false;
            try { return client.Connected; }
            finally { PST_CloseClient(client); }
        }
        // Local TCP/UDP inventory via iphlpapi (no external dependencies).
        private static class PST_PortHelper
        {
            private const int AF_INET = 2;
            private const int AF_INET6 = 23;
            private enum TCP_TABLE_CLASS
            {
                BASIC_LISTENER = 0,
                BASIC_CONNECTIONS = 1,
                BASIC_ALL = 2,
                OWNER_PID_LISTENER = 3,
                OWNER_PID_CONNECTIONS = 4,
                OWNER_PID_ALL = 5
            }
            private enum UDP_TABLE_CLASS
            {
                BASIC = 0,
                OWNER_PID = 1
            }
            [StructLayout(LayoutKind.Sequential)]
            private struct MIB_TCPROW_OWNER_PID
            {
                public uint State;
                public uint LocalAddr;
                public uint LocalPort;
                public uint RemoteAddr;
                public uint RemotePort;
                public int OwningPid;
            }
            [StructLayout(LayoutKind.Sequential)]
            private struct MIB_UDPROW_OWNER_PID
            {
                public uint LocalAddr;
                public uint LocalPort;
                public int OwningPid;
            }
            [StructLayout(LayoutKind.Sequential)]
            private struct MIB_TCP6ROW_OWNER_PID
            {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] LocalAddr;
                public uint LocalScopeId;
                public uint LocalPort;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] RemoteAddr;
                public uint RemoteScopeId;
                public uint RemotePort;
                public uint State;
                public int OwningPid;
            }
            [StructLayout(LayoutKind.Sequential)]
            private struct MIB_UDP6ROW_OWNER_PID
            {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] LocalAddr;
                public uint LocalScopeId;
                public uint LocalPort;
                public int OwningPid;
            }
            [DllImport("iphlpapi.dll", SetLastError = true)]
            private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, int tblClass, int reserved);
            [DllImport("iphlpapi.dll", SetLastError = true)]
            private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, int tblClass, int reserved);
            public class TcpInfo
            {
                public IPAddress LocalAddress { get; set; }
                public int LocalPort { get; set; }
                public IPAddress RemoteAddress { get; set; }
                public int RemotePort { get; set; }
                public TcpState State { get; set; }
                public int Pid { get; set; }
                public string ProcessName { get; set; }
            }
            public class UdpInfo
            {
                public IPAddress LocalAddress { get; set; }
                public int LocalPort { get; set; }
                public int Pid { get; set; }
                public string ProcessName { get; set; }
            }
            public static List<TcpInfo> GetAllTcpConnections()
            {
                var list = new List<TcpInfo>();
                var nameCache = new Dictionary<int, string>();
                list.AddRange(GetTcp4(nameCache));
                list.AddRange(GetTcp6(nameCache));
                return list;
            }
            public static List<UdpInfo> GetAllUdpListeners()
            {
                var list = new List<UdpInfo>();
                var nameCache = new Dictionary<int, string>();
                list.AddRange(GetUdp4(nameCache));
                list.AddRange(GetUdp6(nameCache));
                return list;
            }
            private static List<TcpInfo> GetTcp4(Dictionary<int, string> nameCache)
            {
                var list = new List<TcpInfo>();
                int size = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref size, true, AF_INET, (int)TCP_TABLE_CLASS.OWNER_PID_ALL, 0);
                if (size == 0) return list;
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    uint ret = GetExtendedTcpTable(ptr, ref size, true, AF_INET, (int)TCP_TABLE_CLASS.OWNER_PID_ALL, 0);
                    if (ret != 0) return list;
                    int count = Marshal.ReadInt32(ptr);
                    IntPtr rowPtr = IntPtr.Add(ptr, 4);
                    int rowSize = Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));
                    for (int i = 0; i < count; i++)
                    {
                        var row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                        list.Add(new TcpInfo
                        {
                            LocalAddress = new IPAddress(BitConverter.GetBytes(row.LocalAddr)),
                            LocalPort = ConvertPort(row.LocalPort),
                            RemoteAddress = new IPAddress(BitConverter.GetBytes(row.RemoteAddr)),
                            RemotePort = ConvertPort(row.RemotePort),
                            State = (TcpState)row.State,
                            Pid = row.OwningPid,
                            ProcessName = GetProcessName(row.OwningPid, nameCache)
                        });
                        rowPtr = IntPtr.Add(rowPtr, rowSize);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
                return list;
            }
            private static List<TcpInfo> GetTcp6(Dictionary<int, string> nameCache)
            {
                var list = new List<TcpInfo>();
                int size = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref size, true, AF_INET6, (int)TCP_TABLE_CLASS.OWNER_PID_ALL, 0);
                if (size == 0) return list;
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    uint ret = GetExtendedTcpTable(ptr, ref size, true, AF_INET6, (int)TCP_TABLE_CLASS.OWNER_PID_ALL, 0);
                    if (ret != 0) return list;
                    int count = Marshal.ReadInt32(ptr);
                    IntPtr rowPtr = IntPtr.Add(ptr, 4);
                    int rowSize = Marshal.SizeOf(typeof(MIB_TCP6ROW_OWNER_PID));
                    for (int i = 0; i < count; i++)
                    {
                        var row = (MIB_TCP6ROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCP6ROW_OWNER_PID));
                        list.Add(new TcpInfo
                        {
                            LocalAddress = new IPAddress(row.LocalAddr),
                            LocalPort = ConvertPort(row.LocalPort),
                            RemoteAddress = new IPAddress(row.RemoteAddr),
                            RemotePort = ConvertPort(row.RemotePort),
                            State = (TcpState)row.State,
                            Pid = row.OwningPid,
                            ProcessName = GetProcessName(row.OwningPid, nameCache)
                        });
                        rowPtr = IntPtr.Add(rowPtr, rowSize);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
                return list;
            }
            private static List<UdpInfo> GetUdp4(Dictionary<int, string> nameCache)
            {
                var list = new List<UdpInfo>();
                int size = 0;
                GetExtendedUdpTable(IntPtr.Zero, ref size, true, AF_INET, (int)UDP_TABLE_CLASS.OWNER_PID, 0);
                if (size == 0) return list;
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    uint ret = GetExtendedUdpTable(ptr, ref size, true, AF_INET, (int)UDP_TABLE_CLASS.OWNER_PID, 0);
                    if (ret != 0) return list;
                    int count = Marshal.ReadInt32(ptr);
                    IntPtr rowPtr = IntPtr.Add(ptr, 4);
                    int rowSize = Marshal.SizeOf(typeof(MIB_UDPROW_OWNER_PID));
                    for (int i = 0; i < count; i++)
                    {
                        var row = (MIB_UDPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_PID));
                        list.Add(new UdpInfo
                        {
                            LocalAddress = new IPAddress(BitConverter.GetBytes(row.LocalAddr)),
                            LocalPort = ConvertPort(row.LocalPort),
                            Pid = row.OwningPid,
                            ProcessName = GetProcessName(row.OwningPid, nameCache)
                        });
                        rowPtr = IntPtr.Add(rowPtr, rowSize);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
                return list;
            }
            private static List<UdpInfo> GetUdp6(Dictionary<int, string> nameCache)
            {
                var list = new List<UdpInfo>();
                int size = 0;
                GetExtendedUdpTable(IntPtr.Zero, ref size, true, AF_INET6, (int)UDP_TABLE_CLASS.OWNER_PID, 0);
                if (size == 0) return list;
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    uint ret = GetExtendedUdpTable(ptr, ref size, true, AF_INET6, (int)UDP_TABLE_CLASS.OWNER_PID, 0);
                    if (ret != 0) return list;
                    int count = Marshal.ReadInt32(ptr);
                    IntPtr rowPtr = IntPtr.Add(ptr, 4);
                    int rowSize = Marshal.SizeOf(typeof(MIB_UDP6ROW_OWNER_PID));
                    for (int i = 0; i < count; i++)
                    {
                        var row = (MIB_UDP6ROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDP6ROW_OWNER_PID));
                        list.Add(new UdpInfo
                        {
                            LocalAddress = new IPAddress(row.LocalAddr),
                            LocalPort = ConvertPort(row.LocalPort),
                            Pid = row.OwningPid,
                            ProcessName = GetProcessName(row.OwningPid, nameCache)
                        });
                        rowPtr = IntPtr.Add(rowPtr, rowSize);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
                return list;
            }
            private static int ConvertPort(uint netOrder)
            {
                return (int)(((netOrder >> 8) & 0xFF) | ((netOrder & 0xFF) << 8));
            }
            private static string GetProcessName(int pid, Dictionary<int, string> nameCache)
            {
                if (pid <= 0) return "System";
                if (pid == 4) return "System";
                if (nameCache.TryGetValue(pid, out string cached)) return cached;
                string name;
                try
                {
                    using (var proc = Process.GetProcessById(pid))
                    {
                        name = proc.ProcessName;
                    }
                }
                catch (ArgumentException)
                {
                    name = PST_StaticText("pst_process_unknown");
                }
                catch (InvalidOperationException)
                {
                    name = PST_StaticText("pst_process_unknown");
                }
                catch (Win32Exception)
                {
                    // Protected / elevated process: must not fail the whole local scan.
                    name = PST_StaticText("pst_process_unknown");
                }
                catch (UnauthorizedAccessException)
                {
                    name = PST_StaticText("pst_process_unknown");
                }
                catch (Exception)
                {
                    name = PST_StaticText("pst_process_unknown");
                }
                nameCache[pid] = name;
                return name;
            }
        }
    }
}