using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
//
using static Glow.TSModules;

namespace Glow.glow_tools
{
    public partial class GlowDNSTestTool : Form
    {
        // VARIABLES
        // ======================================================================================================
        private TSGetLangs software_lang = new TSGetLangs(GlowMain.lang_path);
        private string DNSTest_okText, DNSTest_errText, DNSTest_cancelText, DNSTest_noIPv6Text, DNSTest_testingText, DNSTest_cancelledText;
        private bool DNSTest_hasIPv6;
        private List<DnsTestItem> DNSTest_dnsProviders;
        private volatile bool DNSTest_isRunning = false;
        private CancellationTokenSource DNSTest_cts;
        public GlowDNSTestTool()
        {
            InitializeComponent();
            //
            DNSTable.Columns.Add("DNSProvider", "Provider");
            DNSTable.Columns.Add("DNSValueV4", "IPv4");
            DNSTable.Columns.Add("DNSValueV6", "IPv6");
            //
            foreach (DataGridViewColumn col in DNSTable.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            ApplyDpiScaling(this.DeviceDpi);
            //
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(DNSTable, true, null);
            }
            catch { }
            //
            DNS_TestStartBtn.Width = Btn_FLP.Width;
            DNS_TestExportBtn.Width = Btn_FLP.Width;
            //
            InitDnsProviders();
        }
        // UI DPI (shared standard: TSDpiHelper)
        // ======================================================================================================
        private void ApplyDpiScaling(int dpi)
        {
            if (IsDisposed || Disposing) return;
            if (dpi <= 0) dpi = TSDpiHelper.BaseDpi;
            TSDpiHelper.ScaleDataGridView(DNSTable, new[] { 150, -1, -1 }, 30, dpiOverride: dpi);
        }
        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            try
            {
                ApplyDpiScaling(e.DeviceDpiNew);
                GTool_DNSTest_Preloader();
                this.PerformLayout();
                this.Invalidate(true);
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "OnDpiChanged()"); }
            }
        }
        // DNS Provider class
        // ======================================================================================================
        private class DnsTestItem
        {
            public GlowMain.DnsProvider Provider { get; }
            public int RowIndex { get; }
            public bool IsCompleted { get; set; }
            public long? BestMs { get; set; }
            public long?[] V4Ms { get; set; }
            public long?[] V6Ms { get; set; }
            public bool WasCancelled { get; set; }
            public DnsTestItem(GlowMain.DnsProvider provider, int rowIndex)
            {
                Provider = provider;
                RowIndex = rowIndex;
            }
        }
        private void InitDnsProviders()
        {
            DNSTest_dnsProviders = new List<DnsTestItem>();
            int row = 0;
            foreach (var provider in GlowMain.DnsProviders)
            {
                DNSTest_dnsProviders.Add(new DnsTestItem(provider, row));
                row++;
            }
        }
        // PRE-LOAD
        // ======================================================================================================
        public void GTool_DNSTest_Preloader()
        {
            try
            {
                // Re-read: GlowMain.lang_path may point to another file after a language change.
                software_lang = new TSGetLangs(GlowMain.lang_path);
                TSThemeModeHelper.InitializeThemeForForm(this);
                //
                BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor");
                //
                DNSTable.BackgroundColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor");
                DNSTable.GridColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "SelectBoxBorderColor");
                DNSTable.DefaultCellStyle.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor");
                DNSTable.DefaultCellStyle.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1");
                DNSTable.AlternatingRowsDefaultCellStyle.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                DNSTable.ColumnHeadersDefaultCellStyle.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNSTable.ColumnHeadersDefaultCellStyle.SelectionBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNSTable.ColumnHeadersDefaultCellStyle.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                DNSTable.DefaultCellStyle.SelectionBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNSTable.DefaultCellStyle.SelectionForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                //
                DNS_PerfectResultLabel.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                DNS_PerfectResultLabel.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_LabelColor1");
                //
                DNS_TestStartBtn.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNS_TestStartBtn.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                DNS_TestStartBtn.FlatAppearance.BorderColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNS_TestStartBtn.FlatAppearance.MouseDownBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNS_TestStartBtn.FlatAppearance.MouseOverBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "AccentColorHover");
                //
                DNS_TestExportBtn.BackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNS_TestExportBtn.ForeColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_BGColor2");
                DNS_TestExportBtn.FlatAppearance.BorderColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNS_TestExportBtn.FlatAppearance.MouseDownBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "TSBT_AccentColor");
                DNS_TestExportBtn.FlatAppearance.MouseOverBackColor = TS_ThemeEngine.ColorMode(GlowMain.theme, "AccentColorHover");
                //
                TSImageRenderer(DNS_TestStartBtn, GlowMain.theme == 1 ? Properties.Resources.ct_test_start_light : Properties.Resources.ct_test_start_dark, 18, ContentAlignment.MiddleRight);
                TSImageRenderer(DNS_TestExportBtn, GlowMain.theme == 1 ? Properties.Resources.ct_export_light : Properties.Resources.ct_export_dark, 17, ContentAlignment.MiddleRight);
                // TEXT
                // ----------------------
                Text = string.Format(software_lang.TSReadLangs("DNSTestTool", "dtt_title"), Application.ProductName);
                DNSTest_okText = software_lang.TSReadLangs("DNSTestTool", "dtt_success");
                DNSTest_errText = software_lang.TSReadLangs("DNSTestTool", "dtt_error");
                DNSTest_cancelText = software_lang.TSReadLangs("DNSTestTool", "dtt_cancel");
                DNSTest_noIPv6Text = software_lang.TSReadLangs("DNSTestTool", "dtt_no_ipv6");
                DNSTest_testingText = software_lang.TSReadLangs("DNSTestTool", "dtt_testing");
                DNSTest_cancelledText = software_lang.TSReadLangs("DNSTestTool", "dtt_cancelled");
                //
                DNSTable.Columns[0].HeaderText = software_lang.TSReadLangs("DNSTestTool", "dtt_column_server");
                DNSTable.Columns[1].HeaderText = "IPv4";
                DNSTable.Columns[2].HeaderText = "IPv6";
                //
                DNS_TestStartBtn.Text = " " + software_lang.TSReadLangs("DNSTestTool", "dtt_start");
                DNS_TestExportBtn.Text = " " + software_lang.TSReadLangs("DNSTestTool", "dtt_export");
                RefreshDynamicTexts();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "GTool_DNSTest_Preloader()"); }
            }
        }
        // Re-render measured/pending rows + best label in the active language (uses the model, no re-test).
        private void RefreshDynamicTexts()
        {
            try
            {
                if (IsDisposed || Disposing || !IsHandleCreated) return;
                if (DNSTest_dnsProviders == null || DNSTable == null || DNSTable.Rows.Count == 0) return;
                string awaitMsg = software_lang.TSReadLangs("DNSTestTool", "dtt_start_await");
                string idleText = DNSTest_isRunning
                    ? (string.IsNullOrWhiteSpace(DNSTest_testingText) ? awaitMsg : DNSTest_testingText)
                    : awaitMsg;
                bool anyCompleted = false;
                foreach (var item in DNSTest_dnsProviders)
                {
                    if (item.RowIndex < 0 || item.RowIndex >= DNSTable.Rows.Count) continue;
                    if (item.IsCompleted && item.V4Ms != null)
                    {
                        anyCompleted = true;
                        long? dummy = null;
                        string errSel = (item.WasCancelled && !string.IsNullOrWhiteSpace(DNSTest_cancelledText)) ? DNSTest_cancelledText : DNSTest_errText;
                        string v4 = BuildResultText(item.Provider.IPv4, item.V4Ms, DNSTest_okText, errSel, ref dummy);
                        if (string.IsNullOrEmpty(v4)) v4 = errSel;
                        string v6;
                        if (!DNSTest_hasIPv6)
                        {
                            v6 = DNSTest_noIPv6Text;
                        }
                        else if (item.Provider.IPv6.Count == 0)
                        {
                            v6 = "-";
                        }
                        else
                        {
                            v6 = BuildResultText(item.Provider.IPv6, item.V6Ms, DNSTest_okText, errSel, ref dummy);
                            if (string.IsNullOrEmpty(v6)) v6 = errSel;
                        }
                        DNSTable.Rows[item.RowIndex].Cells[1].Value = v4;
                        DNSTable.Rows[item.RowIndex].Cells[2].Value = v6;
                    }
                    else
                    {
                        DNSTable.Rows[item.RowIndex].Cells[1].Value = idleText;
                        DNSTable.Rows[item.RowIndex].Cells[2].Value = idleText;
                    }
                }
                if (DNSTest_isRunning)
                {
                    string prefix = software_lang.TSReadLangs("DNSTestTool", "dtt_best_result") + " ";
                    string testing = string.IsNullOrWhiteSpace(DNSTest_testingText) ? awaitMsg : DNSTest_testingText;
                    DNS_PerfectResultLabel.Text = prefix + testing;
                }
                else if (anyCompleted)
                {
                    UpdateBestLabel();
                }
            }
            catch { }
        }
        private void UpdateBestLabel()
        {
            try
            {
                if (IsDisposed || Disposing || !IsHandleCreated) return;
                string BestResultPrefix = software_lang.TSReadLangs("DNSTestTool", "dtt_best_result") + " ";
                var bestTwo = DNSTest_dnsProviders.Where(x => x.BestMs.HasValue).OrderBy(x => x.BestMs.Value).Take(2).ToList();
                if (bestTwo.Count == 0)
                {
                    DNS_PerfectResultLabel.Text = "-";
                }
                else if (bestTwo.Count == 1)
                {
                    DNS_PerfectResultLabel.Text = BestResultPrefix + string.Format("{0} ({1} ms)", bestTwo[0].Provider.Name, bestTwo[0].BestMs.Value);
                }
                else
                {
                    DNS_PerfectResultLabel.Text = BestResultPrefix + string.Format("{0} ({1} ms)  |  {2} ({3} ms)", bestTwo[0].Provider.Name, bestTwo[0].BestMs.Value, bestTwo[1].Provider.Name, bestTwo[1].BestMs.Value);
                }
            }
            catch { }
        }
        // LOAD
        // ======================================================================================================
        private void GlowDNSTestTool_Load(object sender, EventArgs e)
        {
            try
            {
                GTool_DNSTest_Preloader();
                string awaitMsg = software_lang.TSReadLangs("DNSTestTool", "dtt_start_await");
                foreach (var item in DNSTest_dnsProviders)
                {
                    DNSTable.Rows.Add(item.Provider.Name, awaitMsg, awaitMsg);
                }
                DNSTable.ClearSelection();
                this.FormClosing += new FormClosingEventHandler(GlowDNSTestTool_FormClosing);
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "GlowDNSTestTool_Load()"); }
            }
        }
        private void GlowDNSTestTool_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!DNSTest_isRunning) return;
            if (e.CloseReason != CloseReason.UserClosing)
            {
                try { DNSTest_cts?.Cancel(); } catch { }
                return;
            }
            var res = TS_MessageBoxEngine.TS_MessageBox(this, 5, software_lang.TSReadLangs("DNSTestTool", "dtt_close_confirm"));
            if (res == DialogResult.Yes)
            {
                try { DNSTest_cts?.Cancel(); } catch { }
            }
            else
            {
                e.Cancel = true;
            }
        }
        // Async DNS Check (real DNS query over UDP/53, IPv4 + IPv6)
        // ======================================================================================================
        private const string DnsTestHost = "example.com";
        private const int DnsTestAttempts = 4;
        private const int DnsTestTimeoutMs = 1500;
        private static int DnsTestQueryId = new Random().Next(1, 60000);
        private static byte[] BuildDnsQuery(ushort id, string host)
        {
            string[] labels = host.Trim('.').Split('.');
            using (var ms = new MemoryStream())
            {
                ms.WriteByte((byte)(id >> 8));
                ms.WriteByte((byte)(id & 0xFF));
                ms.WriteByte(0x01); ms.WriteByte(0x00); // flags: recursion desired
                ms.WriteByte(0x00); ms.WriteByte(0x01); // QDCOUNT = 1
                ms.WriteByte(0x00); ms.WriteByte(0x00); // ANCOUNT = 0
                ms.WriteByte(0x00); ms.WriteByte(0x00); // NSCOUNT = 0
                ms.WriteByte(0x00); ms.WriteByte(0x00); // ARCOUNT = 0
                foreach (string label in labels)
                {
                    byte[] lb = Encoding.ASCII.GetBytes(label);
                    ms.WriteByte((byte)lb.Length);
                    ms.Write(lb, 0, lb.Length);
                }
                ms.WriteByte(0x00); // QNAME end
                ms.WriteByte(0x00); ms.WriteByte(0x01); // QTYPE = A
                ms.WriteByte(0x00); ms.WriteByte(0x01); // QCLASS = IN
                return ms.ToArray();
            }
        }
        private static bool IsDnsSuccessResponse(byte[] buf, int len, ushort id)
        {
            if (buf == null || len < 12) return false;
            if ((ushort)((buf[0] << 8) | buf[1]) != id) return false;
            if ((buf[2] & 0x80) == 0) return false; // QR: must be a response
            if ((buf[3] & 0x0F) != 0) return false; // RCODE: must be NoError
            return ((buf[6] << 8) | buf[7]) > 0;    // ANCOUNT > 0
        }
        // Best of N attempts in ms, or null when every attempt fails/times out.
        private async Task<long?> MeasureDnsAddressAsync(string ip, CancellationToken ct)
        {
            if (!IPAddress.TryParse(ip, out IPAddress addr)) return null;
            AddressFamily fam = addr.AddressFamily;
            if (fam != AddressFamily.InterNetwork && fam != AddressFamily.InterNetworkV6) return null;
            long? best = null;
            for (int a = 0; a < DnsTestAttempts; a++)
            {
                if (ct.IsCancellationRequested) break;
                ushort id = (ushort)(Interlocked.Increment(ref DnsTestQueryId) & 0xFFFF);
                byte[] query = BuildDnsQuery(id, DnsTestHost);
                try
                {
                    using (var udp = new UdpClient(fam))
                    {
                        var remote = new IPEndPoint(addr, 53);
                        var sw = Stopwatch.StartNew();
                        await udp.SendAsync(query, query.Length, remote).ConfigureAwait(false);
                        Task<UdpReceiveResult> recvTask = udp.ReceiveAsync();
                        Task done = await Task.WhenAny(recvTask, Task.Delay(DnsTestTimeoutMs, ct)).ConfigureAwait(false);
                        if (done != recvTask)
                        {
                            try { var _unused = recvTask.ContinueWith(t => GC.KeepAlive(t.Exception), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default); } catch { }
                            sw.Stop();
                            if (ct.IsCancellationRequested) break;
                            continue;
                        }
                        sw.Stop();
                        UdpReceiveResult res;
                        try { res = await recvTask.ConfigureAwait(false); }
                        catch { continue; }
                        if (IsDnsSuccessResponse(res.Buffer, res.Buffer != null ? res.Buffer.Length : 0, id))
                        {
                            long ms = sw.ElapsedMilliseconds;
                            if (!best.HasValue || ms < best.Value) best = ms;
                        }
                    }
                }
                catch
                {
                    if (ct.IsCancellationRequested) break;
                }
            }
            return best;
        }
        // True when this machine has a usable global IPv6 address (link-local, ULA, loopback excluded).
        private static bool HasGlobalIPv6()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (UnicastIPAddressInformation ua in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetworkV6) continue;
                        if (IPAddress.IsLoopback(ua.Address)) continue;
                        if (ua.Address.IsIPv6LinkLocal || ua.Address.IsIPv6SiteLocal) continue;
                        byte[] b = ua.Address.GetAddressBytes();
                        if (b.Length == 16 && (b[0] & 0xFE) == 0xFC) continue; // fc00::/7 unique-local
                        if (ua.Address.Equals(IPAddress.IPv6Any)) continue;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }
        private async Task<long?[]> MeasureAllAsync(List<string> ips, CancellationToken ct)
        {
            if (ips == null || ips.Count == 0) return new long?[0];
            Task<long?>[] tasks = ips.Select(ip => MeasureDnsAddressAsync(ip, ct)).ToArray();
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        private static string BuildResultText(List<string> ips, long?[] ms, string okText, string errText, ref long? best)
        {
            if (ips == null || ips.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < ips.Count; i++)
            {
                long? v = (ms != null && i < ms.Length) ? ms[i] : (long?)null;
                if (v.HasValue)
                {
                    sb.Append(string.Format("{0} - {1} {2} ms", ips[i], okText, v.Value));
                    if (!best.HasValue || v.Value < best.Value) best = v.Value;
                }
                else
                {
                    sb.Append(string.Format("{0} - {1}", ips[i], errText));
                }
                if (i < ips.Count - 1)
                    sb.Append("   |   ");
            }
            return sb.ToString();
        }
        private string DnsSegText(string ip, long? ms, bool cancelled)
        {
            if (ms.HasValue)
                return string.Format("{0} - {1} {2} ms", ip, DNSTest_okText, ms.Value);
            if (cancelled && !string.IsNullOrWhiteSpace(DNSTest_cancelledText))
                return string.Format("{0} - {1}", ip, DNSTest_cancelledText);
            return string.Format("{0} - {1}", ip, DNSTest_errText);
        }
        private async Task CheckDnsAsync(DnsTestItem item, CancellationToken ct)
        {
            string textV4 = null, textV6 = null;
            long? best = null;
            long?[] r4 = null, r6 = null;
            try
            {
                r4 = await MeasureAllAsync(item.Provider.IPv4, ct).ConfigureAwait(false);
                if (DNSTest_hasIPv6 && item.Provider.IPv6.Count > 0)
                    r6 = await MeasureAllAsync(item.Provider.IPv6, ct).ConfigureAwait(false);
                string errSel = (ct.IsCancellationRequested && !string.IsNullOrWhiteSpace(DNSTest_cancelledText)) ? DNSTest_cancelledText : DNSTest_errText;
                textV4 = BuildResultText(item.Provider.IPv4, r4, DNSTest_okText, errSel, ref best);
                if (string.IsNullOrEmpty(textV4)) textV4 = errSel;
                if (DNSTest_hasIPv6)
                {
                    if (item.Provider.IPv6.Count == 0)
                    {
                        textV6 = "-";
                    }
                    else
                    {
                        textV6 = BuildResultText(item.Provider.IPv6, r6, DNSTest_okText, errSel, ref best);
                        if (string.IsNullOrEmpty(textV6)) textV6 = errSel;
                    }
                }
                else
                {
                    textV6 = DNSTest_noIPv6Text;
                }
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "CheckDnsAsync()"); }
                textV4 = DNSTest_errText;
                textV6 = DNSTest_hasIPv6 ? DNSTest_errText : DNSTest_noIPv6Text;
                best = null;
            }
            item.V4Ms = r4;
            item.V6Ms = r6;
            item.WasCancelled = ct.IsCancellationRequested;
            item.BestMs = best;
            item.IsCompleted = true;
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                if (item.RowIndex < 0 || item.RowIndex >= DNSTable.Rows.Count) return;
                string showV4 = string.IsNullOrEmpty(textV4) ? DNSTest_errText : textV4;
                string showV6 = string.IsNullOrEmpty(textV6) ? (DNSTest_hasIPv6 ? DNSTest_errText : DNSTest_noIPv6Text) : textV6;
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (IsDisposed || !IsHandleCreated) return;
                            if (item.RowIndex < 0 || item.RowIndex >= DNSTable.Rows.Count) return;
                            DNSTable.Rows[item.RowIndex].Cells[1].Value = showV4;
                            DNSTable.Rows[item.RowIndex].Cells[2].Value = showV6;
                        }
                        catch { }
                    }));
                }
                else
                {
                    DNSTable.Rows[item.RowIndex].Cells[1].Value = showV4;
                    DNSTable.Rows[item.RowIndex].Cells[2].Value = showV6;
                }
            }
            catch { }
        }
        // START ENGINE
        // ======================================================================================================
        private async void DNS_TestStartBtn_Click(object sender, EventArgs e)
        {
            if (DNSTest_isRunning)
            {
                try { DNSTest_cts?.Cancel(); } catch { }
                DNS_TestStartBtn.Enabled = false;
                return;
            }
            DNS_TestStartBtn.Enabled = false;
            DNS_TestExportBtn.Enabled = false;
            try
            {
                // Cancellation is armed before the pre-check so it can be aborted too.
                DNSTest_isRunning = true;
                GlowMain.DNSTestMode = true;
                if (DNSTest_cts != null) { try { DNSTest_cts.Dispose(); } catch { } }
                DNSTest_cts = new CancellationTokenSource();
                CancellationToken ct = DNSTest_cts.Token;
                DNS_TestStartBtn.Text = " " + DNSTest_cancelText;
                DNS_TestStartBtn.Enabled = true;
                // Check network connection
                if (!await IsNetworkAvailable(ct))
                {
                    if (ct.IsCancellationRequested) return;
                    DNS_PerfectResultLabel.Text = software_lang.TSReadLangs("DNSTestTool", "dtt_no_net");
                    TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("DNSTestTool", "dtt_no_net_no_test"));
                    return;
                }
                DNSTest_hasIPv6 = HasGlobalIPv6();
                string awaitText = software_lang.TSReadLangs("DNSTestTool", "dtt_start_await");
                string testingText = string.IsNullOrWhiteSpace(DNSTest_testingText) ? awaitText : DNSTest_testingText;
                foreach (var item in DNSTest_dnsProviders)
                {
                    item.BestMs = null;
                    item.V4Ms = null;
                    item.V6Ms = null;
                    item.WasCancelled = false;
                    item.IsCompleted = false;
                }
                if (!IsDisposed && IsHandleCreated)
                {
                    foreach (var item in DNSTest_dnsProviders)
                    {
                        if (item.RowIndex >= 0 && item.RowIndex < DNSTable.Rows.Count)
                        {
                            DNSTable.Rows[item.RowIndex].Cells[1].Value = testingText;
                            DNSTable.Rows[item.RowIndex].Cells[2].Value = testingText;
                        }
                    }
                    DNS_PerfectResultLabel.Text = software_lang.TSReadLangs("DNSTestTool", "dtt_best_result") + " " + testingText;
                }
                //
                Text = string.Format(software_lang.TSReadLangs("DNSTestTool", "dtt_title"), Application.ProductName) + " | " + software_lang.TSReadLangs("DNSTestTool", "dtt_title_test_ruining");
                DNS_TestStartBtn.Text = " " + DNSTest_cancelText;
                DNS_TestStartBtn.Enabled = true;
                var tasks = DNSTest_dnsProviders.Select(p => CheckDnsAsync(p, ct)).ToArray();
                await Task.WhenAll(tasks);
                UpdateBestLabel();
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "DNS_TestStartBtn_Click()"); }
            }
            finally
            {
                //
                Text = string.Format(software_lang.TSReadLangs("DNSTestTool", "dtt_title"), Application.ProductName);
                DNSTest_isRunning = false;
                GlowMain.DNSTestMode = false;
                try
                {
                    DNSTest_cts?.Dispose();
                    DNSTest_cts = null;
                }
                catch { }
                try
                {
                    if (!IsDisposed && IsHandleCreated)
                    {
                        DNS_TestStartBtn.Text = " " + software_lang.TSReadLangs("DNSTestTool", "dtt_start");
                        DNS_TestExportBtn.Enabled = true;
                        DNS_TestStartBtn.Enabled = true;
                    }
                }
                catch { }
            }
        }
        // COPY RESULT
        // ======================================================================================================
        private void DNSTable_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= DNSTable.Rows.Count) return;
                if (DNSTest_dnsProviders.All(p => p.IsCompleted))
                {
                    if (DNSTable.SelectedRows.Count > 0)
                    {
                        string name = Convert.ToString(DNSTable.Rows[e.RowIndex].Cells[0].Value);
                        string v4 = Convert.ToString(DNSTable.Rows[e.RowIndex].Cells[1].Value);
                        string v6 = Convert.ToString(DNSTable.Rows[e.RowIndex].Cells[2].Value);
                        Clipboard.SetText(name + ": " + v4 + " | " + v6);
                        TS_MessageBoxEngine.TS_MessageBox(this, 1, string.Format(software_lang.TSReadLangs("DNSTestTool", "dtt_copy_success"), name));
                    }
                }
                else
                {
                    TS_MessageBoxEngine.TS_MessageBox(this, 2, software_lang.TSReadLangs("DNSTestTool", "dtt_copy_info"));
                }
            }
            catch (Exception)
            {
                try
                {
                    string name = e.RowIndex >= 0 && e.RowIndex < DNSTable.Rows.Count ? Convert.ToString(DNSTable.Rows[e.RowIndex].Cells[0].Value) : string.Empty;
                    TS_MessageBoxEngine.TS_MessageBox(this, 3, string.Format(software_lang.TSReadLangs("DNSTestTool", "dtt_copy_failed"), name, "\n"));
                }
                catch { }
            }
        }
        // PRINT ENGINE
        // ======================================================================================================
        readonly List<string> PrintDNSList = new List<string>();
        private void DNS_TestExportBtn_Click(object sender, EventArgs e)
        {
            try
            {
                PrintDNSList.Clear();
                var providers = GlowMain.DnsProviders;
                if (providers == null || providers.Count == 0)
                    return;
                PrintDNSList.Add(Application.ProductName + " - " + string.Format(software_lang.TSReadLangs("PrintEngine", "pe_save_name"), software_lang.TSReadLangs("DNSTestTool", "dtt_export_name")));
                PrintDNSList.Add(Environment.NewLine + new string('-', 125) + Environment.NewLine);
                int maxNameLength = providers.Max(p => p.Name.Length);
                var segA = new List<string>();
                var segB = new List<string>();
                var segC0 = new List<string>();
                var segC1 = new List<string>();
                for (int i = 0; i < providers.Count; i++)
                {
                    DnsTestItem it = (i < DNSTest_dnsProviders.Count) ? DNSTest_dnsProviders[i] : null;
                    string a, b, c0, c1;
                    if (it != null && it.IsCompleted && it.V4Ms != null)
                    {
                        bool canc = it.WasCancelled;
                        List<string> ips4 = it.Provider.IPv4;
                        a = DnsSegText(ips4[0], it.V4Ms.Length > 0 ? it.V4Ms[0] : (long?)null, canc);
                        b = ips4.Count > 1 ? DnsSegText(ips4[1], it.V4Ms.Length > 1 ? it.V4Ms[1] : (long?)null, canc) : "";
                        if (!DNSTest_hasIPv6)
                        {
                            c0 = DNSTest_noIPv6Text; c1 = "";
                        }
                        else if (it.Provider.IPv6.Count == 0)
                        {
                            c0 = "-"; c1 = "";
                        }
                        else if (it.V6Ms != null)
                        {
                            List<string> ips6 = it.Provider.IPv6;
                            c0 = DnsSegText(ips6[0], it.V6Ms.Length > 0 ? it.V6Ms[0] : (long?)null, canc);
                            c1 = ips6.Count > 1 ? DnsSegText(ips6[1], it.V6Ms.Length > 1 ? it.V6Ms[1] : (long?)null, canc) : "";
                        }
                        else
                        {
                            c0 = (i < DNSTable.Rows.Count) ? Convert.ToString(DNSTable.Rows[i].Cells[2].Value) : "-";
                            if (string.IsNullOrWhiteSpace(c0)) c0 = "-";
                            c1 = "";
                        }
                    }
                    else
                    {
                        a = (i < DNSTable.Rows.Count) ? Convert.ToString(DNSTable.Rows[i].Cells[1].Value) : "-";
                        b = "";
                        c0 = (i < DNSTable.Rows.Count) ? Convert.ToString(DNSTable.Rows[i].Cells[2].Value) : "-";
                        if (string.IsNullOrWhiteSpace(a)) a = "-";
                        if (string.IsNullOrWhiteSpace(c0)) c0 = "-";
                        c1 = "";
                    }
                    segA.Add(a); segB.Add(b); segC0.Add(c0); segC1.Add(c1);
                }
                int maxA = segA.Count > 0 ? segA.Max(s => s.Length) : 0;
                int maxB = segB.Count > 0 ? segB.Max(s => s.Length) : 0;
                int maxC0 = segC0.Count > 0 ? segC0.Max(s => s.Length) : 0;
                for (int i = 0; i < providers.Count; i++)
                {
                    string name = providers[i].Name.PadRight(maxNameLength);
                    string line = name + ": " + segA[i].PadRight(maxA);
                    if (!string.IsNullOrEmpty(segB[i]))
                        line += "   |   " + segB[i].PadRight(maxB);
                    line += "   |   " + segC0[i].PadRight(maxC0);
                    if (!string.IsNullOrEmpty(segC1[i]))
                        line += "   |   " + segC1[i];
                    PrintDNSList.Add(line);
                }
                PrintDNSList.Add(Environment.NewLine + new string('-', 125) + Environment.NewLine);
                PrintDNSList.Add(DNS_PerfectResultLabel.Text);
                using (SaveFileDialog saveDlg = new SaveFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Title = Application.ProductName + " - " + software_lang.TSReadLangs("PrintEngine", "pe_save_directory"),
                    DefaultExt = "txt",
                    FileName = Application.ProductName + " - " + string.Format(software_lang.TSReadLangs("PrintEngine", "pe_save_name"), software_lang.TSReadLangs("DNSTestTool", "dtt_export_name")),
                    Filter = software_lang.TSReadLangs("PrintEngine", "pe_save_txt") + " (*.txt)|*.txt"
                })
                {
                    if (saveDlg.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveDlg.FileName, string.Join(Environment.NewLine, PrintDNSList));
                        var res = TS_MessageBoxEngine.TS_MessageBox(this, 5, string.Format(software_lang.TSReadLangs("PrintEngine", "pe_save_success") + Environment.NewLine + Environment.NewLine + software_lang.TSReadLangs("PrintEngine", "pe_save_info_open"), Application.ProductName, saveDlg.FileName));
                        if (res == DialogResult.Yes)
                            Process.Start(saveDlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                if (GlowMain.debug_status) { TSErrorLog.LogException(ex, "DNS_TestExportBtn_Click()"); }
            }
        }
    }
}