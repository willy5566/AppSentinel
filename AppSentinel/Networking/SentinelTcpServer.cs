using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppSentinel.Infrastructure;

namespace AppSentinel.Networking
{
    public class SentinelTcpServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        public event Action OnClientDisconnected;

        public void Start(int port)
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(System.Net.IPAddress.Loopback, port);
            _listener.Start();
            Task.Run(() => ListenLoop(_cts.Token));
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClient(client, token);
                }
                catch { break; }
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                try
                {
                    while (client.Connected && !token.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (bytesRead == 0) break;

                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Trace.WriteLine(receivedData);
                        // "Launch|Mode|Path"
                        // "Launch|AsCurrentUser|C:\Temp\UICore.exe"
                        var parts = receivedData.Split('|');
                        if (parts.Length == 3 && parts[0] == "Launch")
                        {
                            Enum.TryParse(parts[1], out LaunchMode mode);
                            string path = parts[2];

                            ProcessLauncher.Launch(path, mode);
                        }
                    }
                }
                catch { }
                finally { OnClientDisconnected?.Invoke(); }
            }
        }

        public void Stop() { _cts?.Cancel(); _listener?.Stop(); }
    }
}