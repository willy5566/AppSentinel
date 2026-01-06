using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace AppSentinel
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // 設定服務執行的帳戶 (通常為 LocalSystem 才能 Bypass UAC)
            processInstaller.Account = ServiceAccount.LocalSystem;

            // 設定服務名稱與啟動類型
            serviceInstaller.ServiceName = "AppSentinelService";
            serviceInstaller.DisplayName = "AppSentinel Guardian Service";
            serviceInstaller.Description = "Monitor App status and enable cross-session startup and permission switching.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}