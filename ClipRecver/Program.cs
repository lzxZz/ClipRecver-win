using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace ClipRecver {

    internal static class Program {

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += (_o, _e) => Logger.RequestExit();
            Application.Run(new MainForm());
            ClipRecver.StopRecver();
        }

        internal static IntPtr MainFormHandle;

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static UInt32 GetLastError();

        [DllImport(
            "user32.dll",
            CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode
        )]
        public extern static Int32 PostMessageW(
            IntPtr hWnd,
            UInt32 msg,
            IntPtr wParam,
            IntPtr lParam
        );

        internal static string ToLiteral(this string input) {
            using (var w = new StringWriter()) {
                using (var p = CodeDomProvider.CreateProvider("CSharp")) {
                    p.GenerateCodeFromExpression(
                        new CodePrimitiveExpression(input),
                        w,
                        null
                    );
                    return w.ToString();
                }
            }
        }

        private static readonly char[] B2H = new char[] {
            '0', '1', '2', '3', '4', '5', '6', '7',
            '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
        };

        internal static string ToHex(this byte[] ba) {
            var sb = new StringBuilder(ba.Length * 3);
            foreach (var b in ba) {
                sb.Append(' ');
                sb.Append(B2H[b >> 4]);
                sb.Append(B2H[b & 15]);
            }
            return sb.ToString();
        }

        internal static uint GetUnixTimestamp() =>
            (uint)(DateTimeOffset.UtcNow -
                    new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)
            ).TotalSeconds;

        internal static uint ModFloor(this uint val, uint mod) =>
            val - (val % mod);

        internal static void WriteUInt(this byte[] arr, int start, uint val) {
            arr[start + 0] = (byte)(val >>  0);
            arr[start + 1] = (byte)(val >>  8);
            arr[start + 2] = (byte)(val >> 16);
            arr[start + 3] = (byte)(val >> 24);
        }
    }

    public static class Config {

        static Config() {
            ListenAddr = null;
            ListenPort = 0;
            SharingPassword = null;
            try {
                var content = File.ReadAllText("config.toml.txt");
                var config = Parser.Parse(content);
                var addrObj = config.GetOrDefault(nameof(ListenAddr));
                if (!(addrObj is string addrStr) || addrStr == default) {
                    throw new ArgumentException(nameof(ListenAddr));
                }
                ListenAddr = IPAddress.Parse(addrStr);
                var portObj = config.GetOrDefault(nameof(ListenPort));
                if (!(portObj is string portStr)) {
                    throw new ArgumentException(nameof(ListenPort));
                }
                if (!string.IsNullOrWhiteSpace(portStr) &&
                    ushort.TryParse(portStr, out ushort port) &&
                    port != 0
                ) {
                    ListenPort = port;
                }
                var passwordObj = config.GetOrDefault(nameof(SharingPassword));
                if (!(passwordObj is string password)) {
                    throw new ArgumentException(nameof(SharingPassword));
                }
                if (!string.IsNullOrWhiteSpace(password)) {
                    if (password.Length > 16) {
                        password = password.Substring(0, 16);
                    }
                    SharingPassword = Regex.Unescape(password);
                    var spbaOrig = Encoding.GetBytes(password);
                    var spba = new byte[12];
                    if (spbaOrig.Length > 12) {
                        using (var md5 = new MD5CryptoServiceProvider()) {
                            var hash = md5.ComputeHash(spbaOrig);
                            Array.Copy(hash, spba, spba.Length);
                        }
                    } else {
                        Array.Copy(spbaOrig, spba, spbaOrig.Length);
                        for (var i = spbaOrig.Length; i < spba.Length; ++i) {
                            spba[i] = 0;
                        }
                    }
                    SPBA = spba;
                }
            } catch { }
        }

        public static IPAddress ListenAddr { get; private set; }
        public static ushort ListenPort { get; private set; }
        public static string SharingPassword { get; private set; }
        public static byte[] SPBA { get; private set; }

        public static void SetListenAddr(IPAddress listenAddr) {
            var listenAddrChanged = !listenAddr.Equals(ListenAddr);
            ListenAddr = listenAddr;
            if (listenAddrChanged) {
                CheckCurrentConfigValidityAndRestartRecver();
            }
        }

        public static void SetListenPort(ushort listenPort) {
            var listenPortChanged = ListenPort != listenPort;
            ListenPort = listenPort;
            if (listenPortChanged) {
                CheckCurrentConfigValidityAndRestartRecver();
            }
        }

        public static void SetListenAddrAndPort(IPAddress addr, ushort port) {
            var listenAddrChanged = !addr.Equals(ListenAddr);
            var listenPortChanged = ListenPort != port;
            ListenAddr = addr;
            ListenPort = port;
            if (listenAddrChanged || listenPortChanged) {
                CheckCurrentConfigValidityAndRestartRecver();
            }
        }

        public static void SetSharingPassword(string sharingPassword) {
            var sharingPasswordChanged = SharingPassword != sharingPassword;
            SharingPassword = sharingPassword;
            if (sharingPasswordChanged) {
                var spbaOrig = Encoding.GetBytes(sharingPassword);
                var spba = new byte[12];
                if (spbaOrig.Length > 12) {
                    using (var md5 = new MD5CryptoServiceProvider()) {
                        var hash = md5.ComputeHash(spbaOrig);
                        Array.Copy(hash, spba, spba.Length);
                    }
                } else {
                    Array.Copy(spbaOrig, spba, spbaOrig.Length);
                    for (var i = spbaOrig.Length; i < spba.Length; ++i) {
                        spba[i] = 0;
                    }
                }
                SPBA = spba;
                CheckCurrentConfigValidityAndRestartRecver();
            }
        }

        private static void CheckCurrentConfigValidityAndRestartRecver() {
            if (ListenAddr == null ||
                ListenPort == 0 ||
                string.IsNullOrWhiteSpace(SharingPassword)
            ) {
                return;
            }
            ClipRecver.StopRecver();
            ClipRecver.StartRecver();
        }

        public static void SaveConfig() {
            var content = string.Format(
@"ListenAddr = ""{0}""
ListenPort = ""{1}""
SharingPassword = {2}
",
                ListenAddr,
                ListenPort,
                SharingPassword.ToLiteral()
            );
            File.WriteAllText("config.toml.txt", content, Encoding);
        }

        public static readonly UTF8Encoding Encoding = new UTF8Encoding(false);
    }

    public static class LocalIPAddressInfo {

        public class IPNetwork {

            public IPNetwork(IPAddress prefix, int prefixLength) {
                if (prefix.AddressFamily != AddressFamily.InterNetwork) {
                    throw new NotImplementedException();
                }
                if (prefixLength < 0 || prefixLength > 32) {
                    throw new ArgumentException(nameof(prefixLength));
                }
                Prefix = prefix;
                PrefixLength = prefixLength;
                long mask = 0;
                for (int c = 0; c < prefixLength; ++c) {
                    mask |= 1L << c;
                }
                Mask = new IPAddress(mask);
            }

            public IPAddress Prefix { get; private set; }
            public int PrefixLength { get; private set; }
            private readonly IPAddress Mask;

            public bool Contains(IPAddress addr) {
                var addrba = addr.GetAddressBytes();
                var maskba = Mask.GetAddressBytes();
                addrba[0] &= maskba[0];
                addrba[1] &= maskba[1];
                addrba[2] &= maskba[2];
                addrba[3] &= maskba[3];
                var prfxba = Prefix.GetAddressBytes();
                return addrba[0] == prfxba[0] && addrba[1] == prfxba[1] &&
                        addrba[2] == prfxba[2] && addrba[3] == prfxba[3];
            }
        }
        
        internal static int IPv4MaskToCIDRPrefix(IPAddress mask) {
            var maskba = mask.GetAddressBytes();
            var length = 32;
            var bits = (((uint)maskba[0]) << 24) |
                        (((uint)maskba[1]) << 16) |
                        (((uint)maskba[2]) << 8) |
                        maskba[3];
            while ((bits & 1) == 0) {
                --length;
                bits >>= 1;
            }
            return length;
        }

        public static readonly IPAddress[] PrvAddrs;
        public static readonly IPAddress[] PubAddrs;
        public static readonly IPNetwork[] PrvNetworks;
        public static readonly IPNetwork[] PubNetworks;

        static LocalIPAddressInfo() {
            var list =
                NetworkInterface
                    .GetAllNetworkInterfaces()
                    .SelectMany(intf =>
                        intf.GetIPProperties()
                            .UnicastAddresses
                            .Where(uniaddr => {
                                var addr = uniaddr.Address;
                                if (addr.AddressFamily != AddressFamily.InterNetwork
                                ) {
                                    return false;
                                }
                                var ba = addr.GetAddressBytes();
                                if (ba[0] == 0 ||
                                    (ba[0] == 100 && ba[1] >= 64 && ba[1] <= 127) ||
                                    ba[0] == 127 ||
                                    (ba[0] == 169 && ba[1] == 254) ||
                                    (ba[0] == 192 && ba[1] == 0 && ba[2] == 0) ||
                                    (ba[0] == 192 && ba[1] == 0 && ba[2] == 2) ||
                                    (ba[0] == 192 && ba[1] == 88 && ba[2] == 99) ||
                                    (ba[0] == 198 && ba[1] >= 18 && ba[1] <= 19) ||
                                    (ba[0] == 198 && ba[1] == 51 && ba[2] == 100) ||
                                    (ba[0] == 203 && ba[1] == 0 && ba[3] == 113) ||
                                    ba[0] >= 224
                                ) {
                                    return false;
                                }
                                return true;
                            })
                            .Select(uniaddr =>(uniaddr.Address, uniaddr.IPv4Mask))
                    );
            bool IsPrivateAddress(IPAddress addr) {
                var aba = addr.GetAddressBytes();
                if (aba[0] == 10 ||
                    (aba[0] == 172 && (aba[1] >> 4) == 1) ||
                    (aba[0] == 192 && aba[1] == 168)
                ) {
                    return true;
                }
                return false;
            }
            IPAddress GetPrefixAddr(IPAddress addr, IPAddress mask) {
                var addrba = addr.GetAddressBytes();
                var maskba = mask.GetAddressBytes();
                addrba[0] &= maskba[0];
                addrba[1] &= maskba[1];
                addrba[2] &= maskba[2];
                addrba[3] &= maskba[3];
                return new IPAddress(addrba);
            }
            var prvAddrs = new List<IPAddress>();
            var pubAddrs = new List<IPAddress>();
            var prvNetworks = new List<IPNetwork>();
            var pubNetworks = new List<IPNetwork>();
            foreach (var (addr, mask) in list) {
                var prefixAddr = GetPrefixAddr(addr, mask);
                var prefix = IPv4MaskToCIDRPrefix(mask);
                var net = new IPNetwork(prefixAddr, prefix);
                if (IsPrivateAddress(addr)) {
                    prvAddrs.Add(addr);
                    prvNetworks.Add(net);
                } else {
                    pubAddrs.Add(addr);
                    pubNetworks.Add(net);
                }
            }
            PrvAddrs = prvAddrs.ToArray();
            PubAddrs = pubAddrs.ToArray();
            PrvNetworks = prvNetworks.ToArray();
            PubNetworks = pubNetworks.ToArray();
        }

        public static bool IsInSameSubnetIPv4(IPAddress r) {
            if (r.AddressFamily != AddressFamily.InterNetwork) {
                return false;
            }
            if (r.Equals(IPAddress.Any)) {
                return true;
            }
            foreach (var network in PrvNetworks) {
                if (network.Contains(r)) {
                    return true;
                }
            }
            return false;
        }
    }

    public static class Logger {

        private static Queue<string> msgQueue;
        private static readonly object queueLock;
        private static readonly AutoResetEvent avlEvt;
        private static readonly string filePath;
        private static int exit;

        static Logger() {
            msgQueue = new Queue<string>();
            queueLock = new object();
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "log");
            avlEvt = new AutoResetEvent(false);
            exit = 0;

            (new Thread(MsgWriter)).Start();
        }

        public static void RequestExit() {
            Interlocked.Exchange(ref exit, 1);
            avlEvt.Set();
        }

        private static void EnqueueMsg(string msg) {
            lock (queueLock) {
                msgQueue.Enqueue(msg);
                avlEvt.Set();
            }
        }

        private static string FormatLogMsg(string lvl, string msg) {
            return string.Format(
                "{0}[{1}] {2}\r\n",
                lvl,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"),
                msg
            );
        }

        private static string FormatCustomLog(string format, params object[] objs) {
            string msg;
            if (objs == null || objs.Length == 0) {
                msg = format;
            } else {
                msg = String.Format(format, objs);
            }
            return msg;
        }

        public static void Info(string msg) {
            EnqueueMsg(FormatLogMsg("Info ", msg));
        }
        public static void Info(string format, params object[] objs) {
            EnqueueMsg(FormatLogMsg("Info ", FormatCustomLog(format, objs)));
        }
        public static void Debug(string msg) {
            EnqueueMsg(FormatLogMsg("Debug", msg));
        }
        public static void Debug(string format, params object[] objs) {
            EnqueueMsg(FormatLogMsg("Debug", FormatCustomLog(format, objs)));
        }
        public static void Error(string msg) {
            var sf = new StackFrame(1, true);
            var prefix = sf.GetFileName() + ':' + sf.GetFileLineNumber() +
                            " -> " + sf.GetMethod().Name + " : ";
            EnqueueMsg(FormatLogMsg("Error", prefix + msg));
        }
        public static void Error(string format, params object[] objs) {
            EnqueueMsg(FormatLogMsg("Error", FormatCustomLog(format, objs)));
        }

        private static void MsgWriter() {
            var queue = new Queue<string>();

            while (avlEvt.WaitOne() && Interlocked.Exchange(ref exit, 0) != 1) {
                lock (queueLock) {
                    queue = Interlocked.Exchange(ref msgQueue, queue);
                }
                foreach (var msg in queue) {
                    string _path = Path.Combine(
                        filePath,
                        msg.Substring(0, 5).Trim(),
                        DateTime.Now.ToString("yyyy-MM")
                    );
                    if (!Directory.Exists(_path)) {
                        Directory.CreateDirectory(_path);
                    }
                    var fileName = Path.Combine(
                        _path,
                        DateTime.Now.ToString("yyyy-MM-dd") + ".log"
                    );
                    var msg2 = msg.Remove(0, 5);
                    long length;
                    using (var f = new FileStream(fileName, FileMode.Append)) {
                        using (var w = new StreamWriter(f, Encoding.UTF8, 4096)) {
                            w.Write(msg2);
                            w.Flush();
                            length = f.Length;
                        }
                    }
                    if (length > 2 * 1024 * 1024) {
                        int c = 0;
                        string newName;
                        do {
                            ++c;
                            newName = fileName + "." + c + ".bak";
                        } while (File.Exists(newName));
                        File.Copy(fileName, newName);
                        File.Delete(fileName);
                    }
                }
                queue.Clear();
            }
        }
    }
}
