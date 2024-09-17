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

        int[,] mappa;

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
                string s = "map";
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 7; x++)
                        if (mappa[x, y] < 10)
                            s += "0" + (mappa[x, y]).ToString();
                        else
                            s += mappa[x, y].ToString();

                Write(s, stream);

                string message;
                do
                {
                    byte[] lengthPrefix = new byte[4];
                    stream.Read(lengthPrefix, 0, 4);
                    int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

                    byte[] Data = new byte[messageLength];
                    stream.Read(Data, 0, messageLength);

                    message = Encoding.UTF8.GetString(Data);
                } while (message == "confmap");
            }//invio mappa
            {
                int n = 9;
                for (int i = 0; i < tcpClients.Length; i++)
                    if (tcpClients[i] == client)
                        n = i;
                if (n == 9)
                    throw (new Exception("n"));
                string s = "num" + n;

                Write(s, stream);

                string message;
                do
                {
                    byte[] lengthPrefix = new byte[4];
                    stream.Read(lengthPrefix, 0, 4);
                    int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

                    byte[] Data = new byte[messageLength];
                    stream.Read(Data, 0, messageLength);

                    message = Encoding.UTF8.GetString(Data);
                } while (message == "confnum");
            }//invio numeri
            {
                string s = "ngt" + tcpClients.Length;

                Write(s, stream);

                string message;
                do
                {
                    byte[] lengthPrefix = new byte[4];
                    stream.Read(lengthPrefix, 0, 4);
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
                byte[] lengthPrefix = new byte[4];
                stream.Read(lengthPrefix, 0, 4);
                int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

                byte[] Data = new byte[messageLength];
                stream.Read(Data, 0, messageLength);

                string message = Encoding.UTF8.GetString(Data);

                string code = message[..3];
                message = message.Substring(3);

                if (commands.ContainsKey(code))
                {
                    Thread thread = commands[code]();
                    thread.Start(message);
                }
            }
        }
        static int[,] CreaMappa(int lenght)
        {
            int[,] mappa = new int[lenght, lenght];
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

        static void Write(string message, NetworkStream stream)
        {
            byte[] str = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(str.Length);
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(str, 0, str.Length);
            stream.Flush();
        }//invia il messaggio al player
    }
}
