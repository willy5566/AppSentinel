using System;
using System.Linq;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;

namespace AppSentinel
{
    internal static class Program
    {
        private const string ServiceName = "AppSentinelService";

        static void Main(string[] args)
        {
            if (Environment.UserInteractive && args.Length > 0)
            {
                // 解析命令列參數
                string parameter = args[0].ToLower();
                try
                {
                    switch (parameter)
                    {
                        case "-i":
                        case "--install":
                            InstallService();
                            Console.WriteLine("The service has been successfully installed and set to start automatically.");
                            break;
                        case "-u":
                        case "--uninstall":
                            UninstallService();
                            Console.WriteLine("The service has been removed.");
                            break;
                        case "-s":
                        case "--start":
                            StartService();
                            Console.WriteLine("Service starting up...");
                            break;
                        case "-t":
                        case "--stop":
                            StopService();
                            Console.WriteLine("Service is currently unavailable...");
                            break;
                        case "-r":
                        case "--restart":
                            StopService();
                            StartService();
                            Console.WriteLine("The service has been restarted.");
                            break;
                        default:
                            ShowHelp();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution error: {ex.Message}");
                }
                return;
            }

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SentinelService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        #region Service management functions

        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new[] { "/LogFile=", Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new[] { "/u", "/LogFile=", Assembly.GetExecutingAssembly().Location });
        }

        private static void StartService()
        {
            using (var sc = new ServiceController(ServiceName))
            {
                if (sc.Status != ServiceControllerStatus.Running) sc.Start();
            }
        }

        private static void StopService()
        {
            using (var sc = new ServiceController(ServiceName))
            {
                if (sc.Status != ServiceControllerStatus.Stopped) sc.Stop();
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("AppSentinel service management tool");
            Console.WriteLine("----------------------------------");
            Console.WriteLine("Usage:");
            Console.WriteLine("  -i, --install   Install and set to start automatically.");
            Console.WriteLine("  -u, --uninstall Remove service");
            Console.WriteLine("  -s, --start     Start service");
            Console.WriteLine("  -t, --stop      Stop service");
            Console.WriteLine("  -r, --restart   Restart service");
        }

        #endregion
    }
}