using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using AppSentinel.Infrastructure;
using AppSentinel.Networking;

namespace AppSentinel
{
    public partial class SentinelService : ServiceBase
    {
        private SentinelTcpServer _tcpServer;
        private readonly string _targetAppPath = @"C:\Temp\UICore.exe";

        public SentinelService()
        {
            this.ServiceName = "AppSentinelService";
        }

        protected override void OnStart(string[] args)
        {
            //EnablePrivilege("SeAssignPrimaryTokenPrivilege");
            //EnablePrivilege("SeIncreaseQuotaPrivilege");

            // 初始啟動應用程式
            if (File.Exists(_targetAppPath))
            {
                ProcessLauncher.Launch(_targetAppPath, LaunchMode.System);
            }
            else
            {
                Trace.WriteLine($"{_targetAppPath} not exist");
            }

            // 啟動監控伺服器
            _tcpServer = new SentinelTcpServer();
            _tcpServer.OnClientDisconnected += HandleClientCrash;
            _tcpServer.Start(5566);
        }

        private void HandleClientCrash()
        {
            //ProcessLauncher.Launch(_targetAppPath, LaunchMode.AsCurrentUser);
        }

        protected override void OnStop()
        {
            _tcpServer?.Stop();
        }

        private void EnablePrivilege(string privilege)
        {
            if(!NativeMethods.OpenProcessToken(Process.GetCurrentProcess().Handle, NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, out IntPtr hToken))
            {
                int error = Marshal.GetLastWin32Error();
                string message = String.Format("OpenProcessToken Error: {0}", error);
                Trace.WriteLine(message);
            }
            if(!NativeMethods.LookupPrivilegeValue(null, privilege, out NativeMethods.LUID luid))
            {
                int error = Marshal.GetLastWin32Error();
                string message = String.Format("LookupPrivilegeValue Error: {0}", error);
                Trace.WriteLine(message);
            }
            NativeMethods.TOKEN_PRIVILEGES tp = new NativeMethods.TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = NativeMethods.SE_PRIVILEGE_ENABLED };
            if(!NativeMethods.AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                string message = String.Format("AdjustTokenPrivileges Error: {0}", error);
                Trace.WriteLine(message);
            }
            NativeMethods.CloseHandle(hToken);
        }
    }
}
