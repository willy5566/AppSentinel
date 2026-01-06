using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace AppSentinel.Infrastructure
{
    internal static class NativeMethods
    {
        public const uint MAXIMUM_ALLOWED = 0x2000000;
        public const int TOKEN_DUPLICATE = 0x0002;
        public const int NORMAL_PRIORITY_CLASS = 0x20;
        public const int CREATE_NO_CONSOLE = 0x08000000;
        public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public const int GENERIC_ALL_ACCESS = 0x10000000;
        public const int TOKEN_ADJUST_PRIVILEGES = 0x20;
        public const int TOKEN_QUERY = 0x8;
        public const int SE_PRIVILEGE_ENABLED = 0x2;

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenSessionId = 12
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb; 
            public string lpReserved; 
            public string lpDesktop; 
            public string lpTitle;
            public uint dwX; 
            public uint dwY; 
            public uint dwXSize; 
            public uint dwYSize;
            public uint dwXCountChars; 
            public uint dwYCountChars; 
            public uint dwFillAttribute;
            public uint dwFlags; 
            public short wShowWindow; 
            public short cbReserved2;
            public IntPtr lpReserved2; 
            public IntPtr hStdInput; 
            public IntPtr hStdOutput; 
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess; 
            public IntPtr hThread; 
            public uint dwProcessId; 
            public uint dwThreadId;
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

        [DllImport("kernel32.dll")]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandle,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DuplicateTokenEx(
            IntPtr hExistingToken, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpThreadAttributes,
            int ImpersonationLevel, int dwTokenType, ref IntPtr phNewToken);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(
            out IntPtr lpEnvironment,
            IntPtr hToken,
            bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool SetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            ref uint TokenInformation,
            uint TokenInformationLength);
    }
}