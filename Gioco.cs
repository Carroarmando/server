using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data.Common;
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

        User[] users;
        public struct User
        {
            public int stato, dir;
            public int cube, chunk, pixel;
            TcpClient client;
            public User(TcpClient client)
            {
                this.client = client;
                cube = 0;
                chunk = 0;
                pixel = 0;
                stato = 0;
                dir = 0;
            }
            public static implicit operator TcpClient(User u) => u.client;
            public static implicit operator User(TcpClient client) => new User(client);
            public static bool operator ==(User u, TcpClient client) => u.client.Equals(client);
            public static bool operator !=(User u, TcpClient client) => !u.client.Equals(client);
        }

        int[,] mappa;

        public Gioco(TcpClient[] tcpClients, int port)
        {
            if (tcpClients.Length != Program.giocatori)
                throw new ArgumentException();

            users = new User[tcpClients.Length];
            for (int i = 0; i < tcpClients.Length; i++)
                users[i] = tcpClients[i];

            tcpPort = port;

            mappa = CreaMappa(7);

            foreach(TcpClient client in users)
            {
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }
        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
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
                } while (message != "confmap");
            }//invio mappa
            {
                int n = 9;
                for (int i = 0; i < users.Length; i++)
                    if (users[i] == client)
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
                } while (message != "confnum");
            }//invio numeri
            {
                string s = "ngt" + users.Length;

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
                } while (message != "confngt");
            }//invio numero giocatori

            new Thread(() => Ricezione(stream)).Start();
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
                int n = stream.Read(Data, 0, messageLength);

                if (n > 0)
                {
                    string message = Encoding.UTF8.GetString(Data);

                    string code = message[..3];
                    message = message.Substring(3);

                    if (commands.ContainsKey(code))
                    {
                        Thread thread = commands[code]();
                        thread.Start(message);
                    }
                }
                else
                {
                    break;
                }
            }
            stream.Close();
        }
        static int[,] CreaMappa(int lenght)
        {
            int[,] mappa = new int[lenght, lenght];
            for (int y = 0; y < lenght; y++)
                for (int x = 0; x < lenght; x++)
                    if (x == 0)
                        if (y == 0)
                            mappa[x, y] = 0;
                        else if (y == lenght - 1)
                            mappa[x, y] = 2;
                        else
                            mappa[x, y] = 1;
                    else if (x == lenght - 1)
                        if (y == 0)
                            mappa[x, y] = 20;
                        else if (y == lenght - 1)
                            mappa[x, y] = 22;
                        else
                            mappa[x, y] = 21;
                    else if (y == 0)
                        mappa[x, y] = 10;
                    else if (y == lenght - 1)
                        mappa[x, y] = 12;
                    else
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
        void plr(object message) // [indice in tcpclients][pixel][chunk][cube][stato][k]
        {
            string msg = message.ToString();
            int i = msg[0] - '0';
            users[i].pixel = Convert.ToInt32(msg[1..3]);
            users[i].chunk = Convert.ToInt32(msg[3..5]);
            users[i].cube  = Convert.ToInt32(msg[5..7]);
            users[i].stato = msg[7] - '0';
            users[i].dir   = msg[8] - '0';
        }

        static void Write(string message, NetworkStream stream)
        {
            Console.WriteLine(message);
            byte[] str = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(str.Length);
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(str, 0, str.Length);
            stream.Flush();
        }//invia il messaggio al player
    }
}
