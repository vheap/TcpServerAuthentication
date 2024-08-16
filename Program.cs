using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System;

namespace TcpServerAuthentication
{
    internal class Program
    {
        public static MemoryClass Memory = MemoryClass.Memory;

        static void Main(string[] args)
        {
            InitiateServer();
        }
        private static void InitiateServer()
        {
            //Certificate for the server. Used for authentication for clients.
            string CertPath = AppDomain.CurrentDomain.BaseDirectory + "\\cert.pfx";
            if (!File.Exists(CertPath))
            {
                Console.WriteLine("Server failed to launch. Certification private key '.pfx' does not exist in main directory.");
                Console.ReadLine();
                return;
            }
            Memory.ServerCertificate = X509Certificate.CreateFromCertFile(CertPath);

            Thread ctThread = new Thread(InitiateReceiver);
            ctThread.Start();
            Console.Read();
        }
        private static void InitiateReceiver()
        {
            Memory.ServerSocket.Start();
            Console.WriteLine("[Server] Launch has been successful, awaiting connections..");

            while (Memory.IsAcceptingConnections == true)
            {
                Memory.ClientSocket = Memory.ServerSocket.AcceptTcpClient();

                int ClientID = Memory.Clients.Count + 1; //SessionId

                Client client = new Client();
                client.SessionId = ClientID;
                client.TcpClient = Memory.ClientSocket;

                Console.WriteLine("[Connect] Connection Time: " + DateTime.Now.ToString("h:mm:ss tt"));
                Console.WriteLine("[Connect] Client ID: " + ClientID);
                Console.WriteLine("[Connect] IP Address: " + ((IPEndPoint)Memory.ClientSocket.Client.RemoteEndPoint).Address.ToString());

                lock (Memory.Clients)
                {
                    Memory.Clients.TryAdd(ClientID, client);
                }
                Thread ctThread = new Thread(() => ClientAuthentication.ProcessClient(client));
                ctThread.Start();
            }

            Memory.ClientSocket.Close();
            Memory.ServerSocket.Stop();
            Console.WriteLine("[Server] System has been shutdown...");
        }
    }
}
