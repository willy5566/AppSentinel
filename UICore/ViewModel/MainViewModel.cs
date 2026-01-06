using UICore.Core;
using UICore.Services; // 引用剛建立的命名空間
using System.Security.Principal;
using System.Diagnostics;
using System;
using UICore.Services.UICore.Services;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace UICore.ViewModel
{
    internal class MainViewModel : ObservableObject
    {
        private readonly SentinelCommunicationService _commService;
        public RelayCommand LaunchUserCommand { get; }
        public RelayCommand LaunchAdminCommand { get; }
        public RelayCommand LaunchSystemCommand { get; set; }
        public RelayCommand OpenDialogCommand { get; }

        public MainViewModel()
        {
            Msg = RunWhoAmI();

            _commService = new SentinelCommunicationService();

            _commService.ConnectionStatusChanged += (isConnected) => {
                IsConnected = isConnected;
            };

            _commService.MessageLogged += (log) => {
                StatusText = $"[{DateTime.Now:HH:mm:ss}] {log}";
            };

            _commService.Start();

            string myPath = Assembly.GetExecutingAssembly().Location;

            LaunchUserCommand = new RelayCommand(async o => {
                await _commService.SendLaunchRequest(myPath, "User");
                Application.Current.Shutdown();
            });

            LaunchAdminCommand = new RelayCommand(async o => {
                await _commService.SendLaunchRequest(myPath, "Admin");
                Application.Current.Shutdown();
            });

            LaunchSystemCommand = new RelayCommand(async o => {
                await _commService.SendLaunchRequest(myPath, "System");
                Application.Current.Shutdown();
            });

            OpenDialogCommand = new RelayCommand(o =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.ShowDialog();
            });
        }

        #region Properties
        private string _msg;
        public string Msg { get => _msg; set { _msg = value; OnPropertyChanged(); } }

        private string _statusText;
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

        private bool _isConnected;
        public bool IsConnected { get => _isConnected; set { _isConnected = value; OnPropertyChanged(); } }
        #endregion

        #region Methods
        public string RunWhoAmI()
        {
            try
            {
                var psi = new ProcessStartInfo("whoami", "/all")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    return proc.StandardOutput.ReadToEnd()?.Trim() ?? "No output";
                }
            }
            catch (Exception ex) { return $"Error: {ex.Message}"; }
        }
        #endregion

        public const int TOKEN_ADJUST_PRIVILEGES = 0x20;
        public const int TOKEN_QUERY = 0x8;
        public const int SE_PRIVILEGE_ENABLED = 0x2;

        private void EnablePrivilege(string privilege)
        {
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr hToken))
            {
                int error = Marshal.GetLastWin32Error();
                string message = String.Format("OpenProcessToken Error: {0}", error);
                Trace.WriteLine(message);
            }
            if (!LookupPrivilegeValue(null, privilege, out LUID luid))
            {
                int error = Marshal.GetLastWin32Error();
                string message = String.Format("LookupPrivilegeValue Error: {0}", error);
                Trace.WriteLine(message);
            }
            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
            if (!AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                string message = String.Format("AdjustTokenPrivileges Error: {0}", error);
                Trace.WriteLine(message);
            }
            CloseHandle(hToken);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID Luid;
            public int Attributes;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);
    }
}