using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace ClipRecver {

    public partial class MainForm : Form {

        #region 窗体行为控制（最小化按钮、关闭按钮、托盘图标）
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_CLOSE = 0xF060;

        private const int WM_SYSCOMMAND = 0x0112;

        public const int WM_APP = 0x8000;
        public const int WM_SENDTOCLIPBOARD = WM_APP + 1;

        protected override void WndProc(ref Message msg) {
            switch (msg.Msg) {
            case WM_SYSCOMMAND:
                switch (msg.WParam.ToInt32()) {
                case SC_MINIMIZE:
                case SC_CLOSE:
                    tsmi显示隐藏.Text = "显示设置(&S)";
                    this.ShowInTaskbar = false;
                    Program.MainFormHandle = this.Handle;
                    this.Hide();
                    return;
                }
                break;
            case WM_SENDTOCLIPBOARD:
                var gcHandle = GCHandle.FromIntPtr(msg.LParam);
                var obj = gcHandle.Target;
                gcHandle.Free();
                try {
                    Clipboard.SetDataObject(obj);
                } catch (Exception ex) {
                    Logger.Error("Clipboard write failed: {0}", ex.ToString());
                    break;
                }
                nic托盘图标.ShowBalloonTip(
                    3000,
                    "提示",
                    "传输数据已复制到剪贴板",
                    ToolTipIcon.Info
                );
                break;
            }
            base.WndProc(ref msg);
        }

        private void tsmi显示隐藏_Click(object sender, EventArgs e) {
            窗体显隐();
        }

        private void nic托盘图标_DoubleClick(object sender, EventArgs e) {
            窗体显隐();
        }

        private void 窗体显隐() {
            if (this.ShowInTaskbar == false) {
                tsmi显示隐藏.Text = "隐藏设置(&H)";
                this.ShowInTaskbar = true;
                Program.MainFormHandle = this.Handle;
                this.Show();
                this.Activate();
            } else {
                tsmi显示隐藏.Text = "显示设置(&S)";
                this.ShowInTaskbar = false;
                Program.MainFormHandle = this.Handle;
                this.Hide();
            }
        }

        private void tsmi退出_Click(object sender, EventArgs e) {
            this.Enabled = false;
            this.Close();
        }
        #endregion

        private readonly BarcodeWriter QRWriter;

        private int PrevAddrIdx = -1;
        private decimal PrevPortNumber = 0m;
        private string PrevPassword = null;
        private Image PrevQRCode = null;

        public MainForm() {
            InitializeComponent();
            this.Location = new Point(
                Screen.PrimaryScreen.WorkingArea.Width - this.Width,
                Screen.PrimaryScreen.WorkingArea.Height - this.Height
            );
            QRWriter = new BarcodeWriter {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions {
                    Width = pbx配置QR码.Width,
                    Height = pbx配置QR码.Height,
                    Margin = 1,
                    PureBarcode = true,
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    ErrorCorrection = ErrorCorrectionLevel.L,
                },
            };
            var addrList = LocalIPAddressInfo.PrvAddrs.ToList().Concat(
                LocalIPAddressInfo.PubAddrs.ToList()
            ).ToList();
            // 允许全网络监听
            addrList.Insert(0, IPAddress.Any);
            int selectedIdx = -1;
            if (Config.ListenAddr != null) {
                var idx =
                    addrList
                        .Select((addr, i) => (i + 1, addr))
                        .FirstOrDefault(v => v.addr.Equals(Config.ListenAddr))
                        .Item1 - 1;
                if (idx != -1) {
                    selectedIdx = idx;
                }
            }
            var dropdownItemList =
                addrList
                    .Select(addr =>
                        new KeyValuePair<string, IPAddress>(addr.ToString(), addr)
                    )
                    .ToList();
            dropdownItemList[0] =
                new KeyValuePair<string, IPAddress>("全部地址", IPAddress.Any);
            cbx监听IP.DisplayMember = "Key";
            cbx监听IP.ValueMember = "Value";
            cbx监听IP.DataSource = dropdownItemList;
            if (selectedIdx != -1) {
                cbx监听IP.SelectedIndex = selectedIdx;
                PrevAddrIdx = selectedIdx;
            } else {
                cbx监听IP.SelectedIndex = 0;
                btn保存设置.Enabled = true;
            }
            if (Config.ListenPort != 0) {
                nud端口.Value = Config.ListenPort;
                PrevPortNumber = nud端口.Value;
            } else {
                btn保存设置.Enabled = true;
            }
            if (!string.IsNullOrWhiteSpace(Config.SharingPassword)) {
                tbx密码.Text = Config.SharingPassword;
                PrevPassword = tbx密码.Text;
            } else {
                tbx密码.Text = 生成随机密码();
                btn保存设置.Enabled = true;
            }
            if (btn保存设置.Enabled) {
                btn保存设置.Focus();
            } else {
                更新QR码();
            }
            nud端口.ValueChanged += new EventHandler(nud端口_ValueChanged);
            cbx监听IP.SelectedIndexChanged +=
                new EventHandler(cbx监听IP_SelectedIndexChanged);
            tbx密码.TextChanged += new EventHandler(tbx密码_TextChanged);
            btn开始停止.Text = ClipRecver.IsRunning ? "停止" : "开始";
        }

        private void MainForm_Shown(object sender, EventArgs e) {
            Program.MainFormHandle = this.Handle;
            if (!btn保存设置.Enabled) {
                窗体显隐();
                ClipRecver.StartRecver();
            }
        }

        private static string 生成随机密码() {
            return (new Random(
                (int)(DateTime.Now - new DateTime(1970, 1, 1))
                        .TotalSeconds
            )).Next(0, 100000000).ToString("00000000");
        }

        private void 更新QR码() {
            var configStr = JsonConvert.SerializeObject(new {
                Scheme = "ClipConfig",
                ListenAddr = cbx监听IP.Text,
                ListenPort = ((ushort)nud端口.Value).ToString(),
                SharingPassword = tbx密码.Text,
            });
            var img = QRWriter.Write(configStr);
            pbx配置QR码.Image = img;
        }

        private void cbx监听IP_SelectedIndexChanged(object sender, EventArgs e) {
            if (配置已修改()) {
                确认已修改();
            } else {
                if (btn撤销改动.Enabled) {
                    取消已修改();
                }
            }
        }

        private void nud端口_ValueChanged(object sender, EventArgs e) {
            if (配置已修改()) {
                确认已修改();
            } else {
                if (btn撤销改动.Enabled) {
                    取消已修改();
                }
            }
        }

        private void tbx密码_TextChanged(object sender, EventArgs e) {
            if (配置已修改()) {
                确认已修改();
            } else {
                if (btn撤销改动.Enabled) {
                    取消已修改();
                }
            }
        }

        private bool 配置已修改() {
            if (cbx监听IP.SelectedIndex != PrevAddrIdx ||
                nud端口.Value != PrevPortNumber ||
                tbx密码.Text != PrevPassword
            ) {
                return true;
            }
            return false;
        }

        private void 确认已修改() {
            if (pbx配置QR码.Image != null) {
                PrevQRCode = pbx配置QR码.Image;
                pbx配置QR码.Image = null;
            }
            btn保存设置.Enabled = true;
            btn撤销改动.Enabled = true;
        }

        private void 取消已修改() {
            Debug.Assert(PrevAddrIdx != -1);
            Debug.Assert(PrevPortNumber != 0m);
            Debug.Assert(PrevPassword != null);
            Debug.Assert(PrevQRCode != null);
            cbx监听IP.SelectedIndex = PrevAddrIdx;
            nud端口.Value = PrevPortNumber;
            tbx密码.Text = PrevPassword;
            pbx配置QR码.Image = PrevQRCode;
            btn保存设置.Enabled = false;
            btn撤销改动.Enabled = false;
        }

        private void tbx密码_Leave(object sender, EventArgs e) {
            if (string.IsNullOrWhiteSpace(tbx密码.Text)) {
                tbx密码.Text = !string.IsNullOrWhiteSpace(PrevPassword)
                                    ? PrevPassword
                                    : 生成随机密码();
            }
        }

        private void btn保存设置_Click(object sender, EventArgs e) {
            var newListenAddr = (IPAddress)cbx监听IP.SelectedValue;
            var newListenPort = (ushort)nud端口.Value;
            var newPassword = tbx密码.Text;
            var caseVal = !newListenAddr.Equals(Config.ListenAddr) ? 2 : 0;
            caseVal += Config.ListenPort != newListenPort ? 1 : 0;
            switch (caseVal) {
            case 3:
                Config.SetListenAddrAndPort(newListenAddr, newListenPort);
                break;
            case 2:
                Config.SetListenAddr(newListenAddr);
                break;
            case 1:
                Config.SetListenPort(newListenPort);
                break;
            case 0:
                break;
            }
            if (Config.SharingPassword != newPassword) {
                Config.SetSharingPassword(newPassword);
            }
            Config.SaveConfig();
            PrevAddrIdx = cbx监听IP.SelectedIndex;
            PrevPortNumber = nud端口.Value;
            PrevPassword = tbx密码.Text;
            if (PrevQRCode != null) {
                PrevQRCode.Dispose();
            }
            更新QR码();
            if (PrevQRCode != null) {
                PrevQRCode.Dispose();
            }
            PrevQRCode = pbx配置QR码.Image;
            btn保存设置.Enabled = false;
            btn撤销改动.Enabled = false;
        }

        private void btn撤销改动_Click(object sender, EventArgs e) {
            取消已修改();
        }

        private void btn开始停止_Click(object sender, EventArgs e) {
            if (btn开始停止.Text == "开始") {
                try {　ClipRecver.StartRecver();　} catch { }
            } else { // btn开始停止.Text == "停止"
                ClipRecver.StopRecver();
            }
            btn开始停止.Text = ClipRecver.IsRunning ? "停止" : "开始";
        }
    }
}
