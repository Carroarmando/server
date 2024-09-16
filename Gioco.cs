using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LatoServer
{
    internal class Gioco
    {
        int tcpPort;

        TcpClient[] tcpClients;

        Casa[,] mappa;

        public Gioco(TcpClient[] tcpClients, int port)
        {
            this.tcpClients = tcpClients;
            if (tcpClients.Length != Program.giocatori)
                throw new ArgumentException();

            mappa = CreaMappa(7);

            this.tcpClients = tcpClients;
            tcpPort = port;

            foreach(TcpClient client in tcpClients)
            {
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }
        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            List<byte> data = new List<byte>();

            {
                List<byte> map = new(Encoding.UTF8.GetBytes("map"));
                string s = "";
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 7; x++)
                        if ((int)mappa[x, y] < 10)
                            s += "0" + ((int)mappa[x, y]).ToString();
                        else
                            s += ((int)mappa[x, y]).ToString();
                map.AddRange(Encoding.UTF8.GetBytes(s));

                byte[] Map = map.ToArray();
                byte[] lengthPrefix = BitConverter.GetBytes(Map.Length);
                stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                stream.Write(Map, 0, Map.Length);
                stream.Flush();

                string message;
                do
                {
                    lengthPrefix = new byte[7];
                    stream.Read(lengthPrefix, 0, 7);
                    int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

                    byte[] Data = new byte[messageLength];
                    stream.Read(Data, 0, messageLength);

                    message = Encoding.UTF8.GetString(Data);
                } while (message == "confmap");
            }//invio mappa
            {
                int n = 5;
                for (int i = 0; i < tcpClients.Length; i++)
                    if (tcpClients[i] == client)
                        n = i;
                if (n == 5)
                    throw (new Exception("n"));
                string s = "num" + n;

                byte[] Map = Encoding.UTF8.GetBytes(s);
                byte[] lengthPrefix = BitConverter.GetBytes(Map.Length);
                stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                stream.Write(Map, 0, Map.Length);
                stream.Flush();

                string message;
                do
                {
                    lengthPrefix = new byte[7];
                    stream.Read(lengthPrefix, 0, 7);
                    int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

                    byte[] Data = new byte[messageLength];
                    stream.Read(Data, 0, messageLength);

                    message = Encoding.UTF8.GetString(Data);
                } while (message == "confnum");
            }//invio numeri
            {
                string s = "ngt" + tcpClients.Length;

                byte[] Map = Encoding.UTF8.GetBytes(s);
                byte[] lengthPrefix = BitConverter.GetBytes(Map.Length);
                stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                stream.Write(Map, 0, Map.Length);
                stream.Flush();

                string message;
                do
                {
                    lengthPrefix = new byte[7];
                    stream.Read(lengthPrefix, 0, 7);
                    int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

                    byte[] Data = new byte[messageLength];
                    stream.Read(Data, 0, messageLength);

                    message = Encoding.UTF8.GetString(Data);
                } while (message == "confngt");
            }//invio numero giocatori

            new Thread(() => Ricezione(stream)).Start();

            try
            {
                while (true)
                {
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        data.AddRange(buffer.Take(bytesRead));
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Errore nella gestione del client: " + e.Message);
            }
            finally
            {
                // Chiudi la connessione
                stream.Close();
                client.Close();
                for (int i = 0; i < tcpClients.Length; i++)
                    if (tcpClients[i] == client)
                        tcpClients[i] = null;
            }
        }
        void Ricezione(NetworkStream stream)
        {
            Dictionary<string, Func<Thread>> commands = new Dictionary<string, Func<Thread>>()
            {
                { "plr", () => new Thread(new ParameterizedThreadStart(plr)) },
            };

            while (true)
            {
                List<byte> data = new List<byte>();
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    data.AddRange(buffer.Take(bytesRead));
                    if (bytesRead < 1024)
                        break;
                }
                string message = Encoding.UTF8.GetString(data.ToArray());

                string code = message[..3];
                message = message.Substring(3);

                if (commands.ContainsKey(code))
                {
                    Thread thread = commands[code]();
                    thread.Start(message);
                }
            }
        }
        static Casa[,] CreaMappa(int lenght)
        {
            Casa[,] mappa = new Casa[lenght, lenght];
            for (int y = 0; y < mappa.GetLength(1); y++)
                for (int x = 0; x < mappa.GetLength(0); x++)
                {
                    Random r = new Random();
                    int n = r.Next(100);

                    if (n < 65)
                        mappa[x, y] = 11;
                    else if (n < 90)
                    {
                        int n1 = r.Next(1, 5);
                        switch (n1)
                        {
                            case 1:
                                mappa[x, y] = 10;
                                break;
                            case 2:
                                mappa[x, y] = 21;
                                break;
                            case 3:
                                mappa[x, y] = 12;
                                break;
                            case 4:
                                mappa[x, y] = 01;
                                break;
                        }
                    }
                    else
                    {
                        int n1 = r.Next(1, 5);
                        switch (n1)
                        {
                            case 1:
                                mappa[x, y] = 00;
                                break;
                            case 2:
                                mappa[x, y] = 20;
                                break;
                            case 3:
                                mappa[x, y] = 22;
                                break;
                            case 4:
                                mappa[x, y] = 02;
                                break;
                        }
                    }
                }
            return mappa;
        }
        void plr(object message) //"[indice in tcpclients][pixel][chunk][cube][stato]
        {
            string msg = message.ToString();
            foreach(TcpClient client in tcpClients)
            {
                NetworkStream stream = client.GetStream();
                string s = "plr" + msg;
                byte[] data = Encoding.UTF8.GetBytes(s);
                
                int offset = 0;
                while (offset < data.Length)
                {
                    stream.Write(data, offset, Math.Min(data.Length - offset, 1024));
                    offset += 1024;
                }
            }
        }
    }
}
