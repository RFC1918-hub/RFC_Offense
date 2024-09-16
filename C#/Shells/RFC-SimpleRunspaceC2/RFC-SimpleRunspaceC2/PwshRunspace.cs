using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace RFC_SimpleRunspaceC2
{
    internal class PwshRunspace
    {
        Runspace runspace;
        PowerShell powershell;

        public void Start()
        {
            //create the runspace
            runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();

            //create a PowerShell object with the runspace
            powershell = PowerShell.Create();
            powershell.Runspace = runspace;
        }

        public void Stop()
        {
            powershell.Dispose();
            runspace.Dispose();
        }

        public string RunPwshCommand(string command)
        {
            StringWriter stringWriter = new StringWriter();
            Collection<PSObject> results = null;

            powershell.AddScript(command);
            powershell.Streams.ClearStreams();
            results = powershell.Invoke();

            foreach (PSObject obj in results)
            {
                stringWriter.WriteLine(obj.ToString());
            }

            foreach (ErrorRecord error in powershell.Streams.Error)
            {
                stringWriter.WriteLine(error.ToString());
            }

            if (stringWriter.ToString() != "")
            {
                return stringWriter.ToString();
            }
            else
            {
                return "[!] No output results";
            }
        }
    }
}
