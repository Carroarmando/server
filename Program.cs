using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace LatoServer
{
    internal class Program
    {
        static TcpListener server = new TcpListener(IPAddress.Any, 8888);

        static object ClientsLock = new object();
        static List<TcpClient> tcpClients = new List<TcpClient>();

        public const int giocatori = 2;
        static void Main()
        {
            server.Start();

            Thread acceptclient = new Thread(AcceptClient);
            acceptclient.Start();
        }

        static void AcceptClient()
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                lock (ClientsLock)
                {
                    tcpClients.Add(client);

                    if(tcpClients.Count == giocatori)
                    {
                        _ = new Gioco(tcpClients.ToArray(), 8888);
                        tcpClients.Clear();
                    }
                }
            }
        }
    }
}
