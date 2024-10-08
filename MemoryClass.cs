﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TcpServerAuthentication
{
    public class MemoryClass
    {
        // A simple use of a Singleton. Not necessary, just for tutorial purposes.
        private static MemoryClass Instance;

        public static MemoryClass Memory
        {
            get
            {
                if (Instance == null)
                    Instance = new MemoryClass();
                return Instance;
            }
        }

        public  List<TcpClient> TcpClientList = new List<TcpClient>();
        public  Dictionary<int, Client> Clients = new Dictionary<int, Client>();

        public  TcpListener ServerSocket = new TcpListener(8888);
        public  TcpClient ClientSocket = default(TcpClient);

        public  X509Certificate ServerCertificate = null;
        public  bool IsAcceptingConnections = true;

        public void RemoveClient(int SessionId)
        {
            lock (Clients)
            {
                Clients.Remove(SessionId);
            }
        }
    }
    public class Client
    {
        // A simple Client class with all related methods included. Refactor for real-world use, but it's similar to this.
        public TcpClient TcpClient { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsLoggedIn { get; set; }
        public string UserIP { get; set; }
        public int SessionId { get; set; }
        SslStream SSL { get; set; }

        private bool IsConnected
        {
            get
            {
                try
                {
                    if (TcpClient != null && TcpClient.Client != null && TcpClient.Client.Connected)
                    {
                        // Detect if client disconnected
                        if (TcpClient.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (TcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
        public void DisconnectClient()
        {
            if (TcpClient == null)
            {
                return;
            }
            SSL.Close();
            TcpClient.Close();
            MemoryClass.Memory.RemoveClient(SessionId);
        }
        private static bool VerifyClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public bool AuthenticateConnection()
        {
            if (IsConnected)
            {
                try
                {
                    // Setting up the security for SLL certification.
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
                    UserIP = ((IPEndPoint)TcpClient.Client.RemoteEndPoint).Address.ToString();
                    SSL = new SslStream(TcpClient.GetStream(), false, VerifyClientCertificate, null);
                    SSL.AuthenticateAsServer(MemoryClass.Memory.ServerCertificate, clientCertificateRequired: true, checkCertificateRevocation: true);
                    if (SSL.RemoteCertificate == null)
                    {
                        Console.WriteLine("Client is not genuine");
                        DisconnectClient();
                        return false;
                    }
                    else
                    {
                        if(SSL.RemoteCertificate != null && SSL.RemoteCertificate.GetIssuerName() != "CN=Example")
                        {
                            Console.WriteLine("Certificate not accepted");
                            DisconnectClient();
                            return false;
                        }
                    }
                }
                catch
                {
                    DisconnectClient();
                    return false;
                }
                SSL.ReadTimeout = 5000;
                SSL.WriteTimeout = 5000;
            }
            return false;
        }

        public bool SendPacket(byte[] Packet)
        {
            try
            {
                if (IsConnected)
                {
                    SSL.Write(Packet);
                    SSL.Flush();
                    return true;
                }
                else
                {
                    Console.WriteLine("Not connected");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Error occurred at SendPacket for ClientID {0}. Message: {1}", SessionId, e.Message));
                return false;
            }
            return false;
        }
    }
}
