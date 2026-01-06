using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AppSentinel.Infrastructure
{
    public enum LaunchMode { User, Admin, System }

    public static class ProcessLauncher
    {
        public static bool Launch(string appPath, LaunchMode mode)
        {
            uint sessionId = NativeMethods.WTSGetActiveConsoleSessionId();
            if (sessionId == 0xFFFFFFFF)
            {
                Trace.WriteLine("WTSGetActiveConsoleSessionId failed");
                return false;
            }

            Trace.WriteLine($"WTSGetActiveConsoleSessionId {sessionId}");

            bool ret = false;

            switch (mode)
            {
                case LaunchMode.User:
                    ret = StartAsUser(appPath, sessionId);
                    break;
                case LaunchMode.Admin:
                    ret = StartAsAdmin(appPath, sessionId);
                    break;
                case LaunchMode.System:
                    ret = StartAsSystem(appPath, sessionId);
                    break;
                default:
                    break;
            }

            return ret;
        }

        private static bool StartAsUser(string appPath, uint sessionId)
        {
            IntPtr hToken = IntPtr.Zero;
            IntPtr hDupedToken = IntPtr.Zero;
            IntPtr lpEnvironment = IntPtr.Zero;
            bool ret = false;

            try
            {
                ret = NativeMethods.WTSQueryUserToken(sessionId, out hToken);
                if (!ret)
                {
                    Trace.WriteLine("WTSQueryUserToken failed");
                    return false;
                }

                NativeMethods.STARTUPINFO si = new NativeMethods.STARTUPINFO();
                si.cb = Marshal.SizeOf(typeof(NativeMethods.STARTUPINFO));

                NativeMethods.SECURITY_ATTRIBUTES sa = new NativeMethods.SECURITY_ATTRIBUTES();
                sa.Length = Marshal.SizeOf(sa);

                ret = NativeMethods.DuplicateTokenEx(
                    hToken,
                    NativeMethods.GENERIC_ALL_ACCESS,
                    ref sa,
                    (int)NativeMethods.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                    (int)NativeMethods.TOKEN_TYPE.TokenPrimary,
                    ref hDupedToken);
                if (!ret)
                {
                    Trace.WriteLine("DuplicateTokenEx failed");
                    return false;
                }

                ret = NativeMethods.CreateEnvironmentBlock(out lpEnvironment, hDupedToken, false);
                if (!ret)
                {
                    Trace.WriteLine("CreateEnvironmentBlock failed");
                    return false;
                }

                NativeMethods.PROCESS_INFORMATION pi;
                int dwCreationFlags = NativeMethods.NORMAL_PRIORITY_CLASS | NativeMethods.CREATE_UNICODE_ENVIRONMENT;

                ret = NativeMethods.CreateProcessAsUser(
                    hDupedToken, 
                    appPath, 
                    String.Empty, 
                    ref sa, ref sa,
                    false, dwCreationFlags, lpEnvironment, 
                    null, ref si, out pi);
                if (!ret)
                {
                    int error = Marshal.GetLastWin32Error();
                    string message = String.Format("CreateProcessAsUser Error: {0}", error);
                    Trace.WriteLine(message);
                }

                if (pi.hProcess != IntPtr.Zero)
                    NativeMethods.CloseHandle(pi.hProcess);
                if (pi.hThread != IntPtr.Zero)
                    NativeMethods.CloseHandle(pi.hThread);
                if (hDupedToken != IntPtr.Zero)
                    NativeMethods.CloseHandle(hDupedToken);
            }
            finally
            {
                if (hToken != IntPtr.Zero) NativeMethods.CloseHandle(hToken);
                if (lpEnvironment != IntPtr.Zero) NativeMethods.DestroyEnvironmentBlock(lpEnvironment);
            }

            return ret;
        }

        private static bool StartAsAdmin(string appPath, uint sessionId)
        {
            Process[] processes = Process.GetProcessesByName("winlogon");
            Process targetProc = Array.Find(processes, p => (uint)p.SessionId == sessionId);
            if (targetProc == null)
            {
                Trace.WriteLine("winlogon not exist");
                return false;
            }

            IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.MAXIMUM_ALLOWED, false, (uint)targetProc.Id);
            IntPtr hToken = IntPtr.Zero;
            IntPtr hDupToken = IntPtr.Zero;
            IntPtr lpEnvironment = IntPtr.Zero;
            bool ret = false;
            try
            {
                if (!NativeMethods.OpenProcessToken(hProcess, NativeMethods.TOKEN_DUPLICATE, out hToken))
                {
                    NativeMethods.CloseHandle(hProcess);
                    Trace.WriteLine("OpenProcessToken failed");
                    return false;
                }

                NativeMethods.SECURITY_ATTRIBUTES sa = new NativeMethods.SECURITY_ATTRIBUTES();
                sa.Length = Marshal.SizeOf(sa);

                if (!NativeMethods.DuplicateTokenEx(hToken, NativeMethods.MAXIMUM_ALLOWED, ref sa,
                    (int)NativeMethods.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                    (int)NativeMethods.TOKEN_TYPE.TokenPrimary, ref hDupToken))
                {
                    NativeMethods.CloseHandle(hProcess);
                    NativeMethods.CloseHandle(hToken);
                    Trace.WriteLine("DuplicateTokenEx failed");
                    return false;
                }

                bool envRet = NativeMethods.CreateEnvironmentBlock(out lpEnvironment, hDupToken, false);

                NativeMethods.STARTUPINFO si = new NativeMethods.STARTUPINFO();
                si.cb = Marshal.SizeOf(typeof(NativeMethods.STARTUPINFO));
                si.lpDesktop = @"winsta0\default";

                NativeMethods.PROCESS_INFORMATION pi;
                int dwCreationFlags = 
                    NativeMethods.NORMAL_PRIORITY_CLASS | 
                    NativeMethods.CREATE_NO_CONSOLE |
                    NativeMethods.CREATE_UNICODE_ENVIRONMENT;
                ret = NativeMethods.CreateProcessAsUser(
                    hDupToken,
                    appPath,
                    null,
                    ref sa, ref sa,
                    false,
                    dwCreationFlags,
                    lpEnvironment,
                    null,
                    ref si,
                    out pi);

                if (!ret)
                {
                    int error = Marshal.GetLastWin32Error();
                    string message = String.Format("CreateProcessAsUser Error: {0}", error);
                    Trace.WriteLine(message);
                }
                else
                {
                    Trace.WriteLine($"CreateProcessAsUser successed {pi.dwProcessId}");
                }
            }
            finally
            {
                if (hProcess != IntPtr.Zero) NativeMethods.CloseHandle(hProcess);
                if (hToken != IntPtr.Zero) NativeMethods.CloseHandle(hToken);
                if (hDupToken != IntPtr.Zero) NativeMethods.CloseHandle(hDupToken);
                if (lpEnvironment != IntPtr.Zero) NativeMethods.DestroyEnvironmentBlock(lpEnvironment);
            }

            return ret;
        }

        private static bool StartAsSystem(string appPath, uint sessionId)
        {
            IntPtr hSelfToken = IntPtr.Zero;
            IntPtr hDupToken = IntPtr.Zero;
            bool result = false;

            try
            {
                if (!NativeMethods.OpenProcessToken(Process.GetCurrentProcess().Handle,
                    NativeMethods.TOKEN_DUPLICATE | NativeMethods.GENERIC_ALL_ACCESS, out hSelfToken))
                    return false;

                NativeMethods.SECURITY_ATTRIBUTES sa = new NativeMethods.SECURITY_ATTRIBUTES();
                sa.Length = Marshal.SizeOf(sa);

                if (!NativeMethods.DuplicateTokenEx(hSelfToken, NativeMethods.MAXIMUM_ALLOWED, ref sa,
                    (int)NativeMethods.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    (int)NativeMethods.TOKEN_TYPE.TokenPrimary, ref hDupToken))
                    return false;

                if (!NativeMethods.SetTokenInformation(hDupToken,
                    NativeMethods.TOKEN_INFORMATION_CLASS.TokenSessionId, ref sessionId, sizeof(uint)))
                    return false;

                NativeMethods.STARTUPINFO si = new NativeMethods.STARTUPINFO();
                si.cb = Marshal.SizeOf(typeof(NativeMethods.STARTUPINFO));
                si.lpDesktop = @"winsta0\default";

                NativeMethods.PROCESS_INFORMATION pi;
                int dwCreationFlags = NativeMethods.NORMAL_PRIORITY_CLASS | NativeMethods.CREATE_UNICODE_ENVIRONMENT;

                result = NativeMethods.CreateProcessAsUser(
                    hDupToken, null, appPath, ref sa, ref sa,
                    false, dwCreationFlags, IntPtr.Zero, null, ref si, out pi);

                if (result)
                {
                    NativeMethods.CloseHandle(pi.hProcess);
                    NativeMethods.CloseHandle(pi.hThread);
                }
            }
            finally
            {
                if (hSelfToken != IntPtr.Zero) NativeMethods.CloseHandle(hSelfToken);
                if (hDupToken != IntPtr.Zero) NativeMethods.CloseHandle(hDupToken);
            }
            return result;
        }
    }
}
