using System.ComponentModel;
using System.ServiceProcess;

namespace SysMonLogger
{
    /// <summary>
    /// 
    /// </summary>
    [RunInstaller(true)]
    public sealed class SmlServiceInstallerProcess : ServiceProcessInstaller
    {
        public SmlServiceInstallerProcess()
        {
            this.Account = ServiceAccount.LocalSystem;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [RunInstaller(true)]
    public sealed class SmlServiceInstaller : ServiceInstaller
    {
        public SmlServiceInstaller()
        {
            this.Description = Global.DESCRIPTION;
            this.DisplayName = Global.DISPLAY_NAME;
            this.ServiceName = Global.SERVICE_NAME;
            this.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
        }
    }
}
