using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UICore.Services
{
    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    namespace UICore.Services
    {
        public class SentinelCommunicationService
        {
            private TcpClient _client;
            private CancellationTokenSource _cts;
            private readonly string _ip;
            private readonly int _port;

            // 定義事件，供 ViewModel 訂閱
            public event Action<bool> ConnectionStatusChanged;
            public event Action<string> MessageLogged;

            public SentinelCommunicationService(string ip = "127.0.0.1", int port = 5566)
            {
                _ip = ip;
                _port = port;
            }

            public void Start()
            {
                Stop(); // 確保不會重複啟動
                _cts = new CancellationTokenSource();
                Task.Run(() => MaintainConnectionAsync(_cts.Token));
            }

            public void Stop()
            {
                _cts?.Cancel();
                _client?.Close();
                _client = null;
            }

            private async Task MaintainConnectionAsync(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        MessageLogged?.Invoke("正在連線至服務...");

                        using (_client = new TcpClient())
                        {
                            await _client.ConnectAsync(_ip, _port);
                            ConnectionStatusChanged?.Invoke(true);
                            MessageLogged?.Invoke("已連線至 AppSentinel Service");

                            using (var stream = _client.GetStream())
                            {
                                while (_client.Connected && !ct.IsCancellationRequested)
                                {
                                    byte[] data = Encoding.UTF8.GetBytes("ping");
                                    await stream.WriteAsync(data, 0, data.Length, ct);
                                    await Task.Delay(5000, ct);
                                }
                            }
                        }
                    }
                    catch
                    {
                        ConnectionStatusChanged?.Invoke(false);
                        MessageLogged?.Invoke("連線斷開，嘗試重連中...");
                    }

                    if (!ct.IsCancellationRequested)
                        await Task.Delay(2000, ct);
                }
            }

            public async Task SendLaunchRequest(string path, string mode)
            {
                if (_client != null && _client.Connected)
                {
                    var stream = _client.GetStream();
                    // 格式: Launch|AsCurrentUser|C:\Path\To\Exe
                    byte[] data = Encoding.UTF8.GetBytes($"Launch|{mode}|{path}");
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
        }
    }
}
