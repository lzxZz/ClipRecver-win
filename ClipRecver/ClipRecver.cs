using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ClipRecver {

    public static class ClipRecver {

        public static bool IsRunning { get; private set; } = false;

        private static UdpClient Recver = null;

        private static UdpClient NewRecver() =>
            new UdpClient(AddressFamily.InterNetwork) {
                EnableBroadcast = true,
                ExclusiveAddressUse = true,
            };

        private const string UDPSendSign = "CLIPSEND";

        private static readonly byte[] UDPSendSignByteArr =
            Config.Encoding.GetBytes(UDPSendSign);

        private const string UDPRecvSign = "CLIPRECV";

        private static readonly byte[] UDPRecvSignByteArr =
            Config.Encoding.GetBytes(UDPRecvSign);

        private static readonly byte[] IV = {
            0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,
            0x10, 0x08, 0x20, 0x04, 0x40, 0x02, 0x80, 0x01,
        };

        public class ReqObj {
            public string MimeType;
            public string Content;
            public string Handshake;
        }

        public class HSParams {
            //TODO:
        }

        public static readonly
        Dictionary<string, Func<string, string, Task>>
            MimeTypeSimpleTransferHandlerDict =
                new Dictionary<string, Func<string, string, Task>> {
                    { "text", TextHandler },
                };

        public static readonly
        Dictionary<string, Func<string, HSParams, Task>>
            MimeTypeComplexTransferHandlerDict =
                new Dictionary<string, Func<string, HSParams, Task>> {
                    //TODO:
                };

        public static void StartRecver() {
            if (Recver != null) {
                throw new InvalidOperationException("Recver is not disposed");
            }
            if (Config.ListenAddr == null) {
                throw new ArgumentNullException(nameof(Config.ListenAddr));
            }
            if (Config.ListenPort == 0) {
                throw new ArgumentOutOfRangeException(nameof(Config.ListenPort));
            }
            if (string.IsNullOrWhiteSpace(Config.SharingPassword)) {
                throw new ArgumentOutOfRangeException(
                    nameof(Config.SharingPassword)
                );
            }
            Recver = NewRecver();
            Recver.Client.Bind(
                new IPEndPoint(Config.ListenAddr, Config.ListenPort)
            );
            // 接收处理例程
            _ = TaskEx.Run(async () => {
                // 异常来自于 EndReceive ，不可能来自 TaskCancellationnSource
                //
                //  ArgumentNullException
                //  asyncResult is null.
                //
                //  ArgumentException
                //  asyncResult was not returned by a call to the BeginReceive(AsyncCallback, Object) method.
                //
                //  InvalidOperationException
                //  EndReceive(IAsyncResult, IPEndPoint) was previously called for the asynchronous read.
                //
                // 以上三种不可能出现，由于 ReceiveAsync 的结构
                //
                // 这两种会出现
                //
                //  SocketException
                //  An error occurred when attempting to access the underlying Socket.
                //
                //  ObjectDisposedException
                //  The underlying Socket has been closed.
                //
                // 这两种异常分别处理
                IsRunning = true;
                while (true) {
                    try {
                        var (_sender, data) =
                            await Recver.ReceiveAsync().ConfigureAwait(false);
                        _ = TaskEx.Run(() => DecryptPacket(_sender, data))
                                    .ConfigureAwait(false);
                    } catch (ObjectDisposedException) {
                        // 出现 ObjectDisposedException 说明是被关了，退出
                        break;
                    } catch (Exception ex) {
                        //TODO: 妥善处理各种异常
                        if (ex is SocketException sockEx) {
                            Logger.Error(sockEx.ToString());
                        } else {
                            Logger.Error(ex.ToString());
                        }
                    }
                }
            }).ConfigureAwait(false);
        }

        public static void StopRecver() {
            if (Recver != null) {
                IsRunning = false;
                try {
                    Recver.Close();
                } catch (Exception ex) {
                    //TODO: 妥善处理各种异常
                    Logger.Error(ex.ToString());
                }
                Recver = null;
            }
        }

        private static async Task DecryptPacket(IPEndPoint sender, byte[] data) {
            if (data.Length < UDPSendSignByteArr.Length) {
                Logger.Error("sender: {0} , data: {1}", sender, data.ToHex());
                return;
            }
            try {
                var udpSendSign = Config.Encoding.GetString(
                    data,
                    0,
                    UDPSendSignByteArr.Length
                );
                if (udpSendSign != UDPSendSign) {
                    Logger.Error("sender: {0} , data: {1}", sender, data.ToHex());
                    return;
                }
            } catch (Exception ex) {
                Logger.Error(
                    "sender: {0} , data: {1}\r\n{2}",
                        sender,
                        data.ToHex(),
                        ex.ToString()
                );
                return;
            }
            byte[] orig;
            if (File.Exists("debug")) {
                orig = new byte[data.Length - UDPSendSignByteArr.Length];
                Array.Copy(
                    data,
                    UDPSendSignByteArr.Length,
                    orig,
                    0,
                    data.Length - UDPSendSignByteArr.Length
                );
            } else {
                orig = null;
                var key = new byte[16];
                var timestamp = Program.GetUnixTimestamp().ModFloor(300) - 300;
                Array.Copy(Config.SPBA, key, Config.SPBA.Length);
                using (var aes = new AesCryptoServiceProvider()) {
                    aes.Mode = CipherMode.CBC;
                    for (var i = 0; i < 3; ++i) {
                        key.WriteUInt(Config.SPBA.Length, timestamp);
                        try {
                            using (var ss = new MemoryStream(
                                data,
                                UDPSendSignByteArr.Length,
                                data.Length - UDPSendSignByteArr.Length,
                                false
                            ))
                            using (var dpt = aes.CreateDecryptor(key, IV))
                            using (var cs = new CryptoStream(
                                ss,
                                dpt,
                                CryptoStreamMode.Read
                            ))
                            using (var ds = new MemoryStream()) {
                                cs.CopyTo(ds);
                                orig = ds.ToArray();
                                break;
                            }
                        } catch (Exception ex) {
                            var exStr = ex.ToString();
                            if (!exStr.Contains("pad")) {
                                Logger.Error(
                                    "sender: {0} , data: {1} , key: {2}\r\n{3}",
                                        sender,
                                        data.ToHex(),
                                        key.ToHex(),
                                        exStr
                                );
                            }
                        }
                        timestamp += 300;
                    }
                }
                if (orig == null) {
                    return;
                }
            }
            string reqStr;
            try {
                reqStr = Config.Encoding.GetString(orig);
            } catch (Exception ex) {
                Logger.Error(
                    "sender: {0} , data: {1}\r\n{2}",
                        sender,
                        data.ToHex(),
                        ex.ToString()
                );
                return;
            }
            try {
                var reqObj = JsonConvert.DeserializeObject<ReqObj>(reqStr);
                if (string.IsNullOrWhiteSpace(reqObj.MimeType)) {
                    Logger.Error("sender: {0} , reqStr: {1}", sender, reqStr);
                    return;
                }
                string mimeCategory;
                string mimeSubCategory;
                {
                    var idx = reqObj.MimeType.IndexOf('/');
                    if (idx == -1) {
                        Logger.Error("sender: {0} , reqStr: {1}", sender, reqStr);
                        return;
                    }
                    mimeCategory = reqObj.MimeType.Substring(0, idx);
                    mimeSubCategory = reqObj.MimeType.Substring(idx + 1);
                }
                if (string.IsNullOrWhiteSpace(reqObj.Content)) {
                    if (MimeTypeComplexTransferHandlerDict.TryGetValue(
                        mimeCategory,
                        out Func<string, HSParams, Task> handler
                    )) {
                        var hsparams =
                            JsonConvert.DeserializeObject<HSParams>(
                                reqObj.Handshake
                            );
                        await handler(mimeSubCategory, hsparams)
                                    .ConfigureAwait(false);
                        return;
                    }
                } else {
                    if (MimeTypeSimpleTransferHandlerDict.TryGetValue(
                        mimeCategory,
                        out Func<string, string, Task> handler
                    )) {
                        await handler(mimeSubCategory, reqObj.Content)
                                    .ConfigureAwait(false);
                        return;
                    }
                }
                Logger.Error(
                    "Unknown type: sender: {0} , reqStr: {1}", sender, reqStr
                );
            } catch (Exception ex) {
                Logger.Error(
                    "sender: {0} , reqStr: {1}\r\n{2}",
                        sender, reqStr, ex.ToString()
                );
                return;
            }
        }

#pragma warning disable CS1998
        private static async Task TextHandler(string subCategory, string content) {
#pragma warning restore CS1998
            switch (subCategory) {
            case "plain":
                var gcHandle = GCHandle.Alloc(content);
                var result = Program.PostMessageW(
                    Program.MainFormHandle,
                    MainForm.WM_SENDTOCLIPBOARD,
                    IntPtr.Zero,
                    GCHandle.ToIntPtr(gcHandle)
                );
                if (result == 0) {
                    var error = Program.GetLastError();
                    Logger.Error(
                        "Win32 message sent failed: " +
                            "subCategory: {0} , content: {1}, error: 0x{2:X08}",
                            subCategory, content, error
                    );
                    gcHandle.Free();
                }
                break;
            default:
                Logger.Error(
                    "Unknown text subCategory: subCategory: {0} , content: {1}",
                        subCategory, content
                );
                break;
            }
        }
    }

    public static class UdpClientExtension {

        public static
        Task<(IPEndPoint, byte[])> ReceiveAsync(this UdpClient client) {
            var tcsOut = new TaskCompletionSource<(IPEndPoint, byte[])>();
            client.BeginReceive((iar) => {
                var tcs = (TaskCompletionSource<(IPEndPoint, byte[])>)iar.AsyncState;
                try {
                    IPEndPoint ep = default;
                    var ba = client.EndReceive(iar, ref ep);
                    tcs.SetResult((ep, ba));
                } catch (Exception ex) {
                    tcs.SetException(ex);
                }
            }, tcsOut);
            return tcsOut.Task;
        }

        public static
        Task<int> SendToAsync(this UdpClient client, IPEndPoint ep, byte[] data) {
            var tcsOut = new TaskCompletionSource<int>();
            client.BeginSend(data, data.Length, ep, new AsyncCallback((iar) => {
                var tcs = (TaskCompletionSource<int>)iar.AsyncState;
                try {
                    var result = client.EndSend(iar);
                    tcs.SetResult(result);
                } catch (Exception ex) {
                    tcs.SetException(ex);
                }
            }), tcsOut);
            return tcsOut.Task;
        }
    }
}
