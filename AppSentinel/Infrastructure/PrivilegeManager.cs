using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AppSentinel.Infrastructure
{
    public static class PrivilegeManager
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [StructLayout(LayoutKind.Sequential)]
        struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        struct TOKEN_PRIVILEGES { public int PrivilegeCount; public LUID Luid; public int Attributes; }

        public static void EnablePrivilege(string privilege)
        {
            if (OpenProcessToken(Process.GetCurrentProcess().Handle, 0x20 | 0x8, out IntPtr hToken))
            {
                if (LookupPrivilegeValue(null, privilege, out LUID luid))
                {
                    TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = 0x2 };
                    AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                }
                NativeMethods.CloseHandle(hToken);
            }
        }
    }
}