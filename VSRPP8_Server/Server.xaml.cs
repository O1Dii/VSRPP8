using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using MongoDB.Driver;
using System.Configuration;
using System.Text;

namespace VSRPP8
{
    /// <summary>
    /// Логика взаимодействия для Server.xaml
    /// </summary>
    public partial class Server : Window
    {
        private Socket socket;
        private Socket clientSocket;
        private Thread mainThread;
        private Thread thread;
        private List<Socket> socketPool;
        private List<Thread> threadPool;
        private object locker;
        IMongoCollection<Logs> coll;
        private SynchronizationContext context;
        public Server()
        {
            InitializeComponent();

            context = SynchronizationContext.Current;

            try
            {
                string con = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
                MongoClient client = new MongoClient(con);
                IMongoDatabase database = client.GetDatabase("logs");
                coll = database.GetCollection<Logs>("messages");
            } catch(Exception)
            {
                Logger.Text += "Error with MongoDB connection";

                Thread.Sleep(1000000);
                Environment.Exit(1);
            }

            socketPool = new List<Socket>();
            threadPool = new List<Thread>();
            locker = new object();

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 8000));
            socket.Listen(5);

            mainThread = new Thread(MainHandler);
            mainThread.Start();
        }

        private void Handler(object clientSocket)
        {
            byte[] bytes = new byte[1024];
            Socket socket = clientSocket as Socket;

            while (true)
            {
                try
                {
                    try
                    {
                        socket.Receive(bytes);
                    } catch (Exception)
                    {
                        lock(locker)
                        {
                            context.Send(AddLog, socket.RemoteEndPoint.ToString() + " disconnected\n");
                            return;
                        }
                    }

                    Logs currentMessage = new Logs(Encoding.UTF8.GetString(bytes).Replace("\0", ""));

                    try
                    {
                        coll.InsertOne(currentMessage);
                    } catch (Exception)
                    {
                        lock(locker)
                        {
                            context.Send(AddLog, "Error inserting into MongoDB");
                        }
                    }

                    lock (locker)
                    {
                        foreach (Socket s in socketPool)
                        {
                            s.Send(bytes);
                        }
                    }
                } catch (SocketException)
                {
                    socketPool.Remove(socket);

                    break;
                }

                bytes = new byte[1024];
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Socket s in socketPool)
            {
                s.Close();
            }

            foreach (Thread t in threadPool)
            {
                t.Abort();
            }

            socket.Close();
            mainThread.Abort();
        }

        private void AddLog(object addr)
        {
            Logger.Text += (addr as string);
        }

        private void MainHandler()
        {
            while (true)
            {
                thread = new Thread(Handler);

                clientSocket = socket.Accept();

                thread.Start(clientSocket);

                lock (locker)
                {
                    context.Send(AddLog, clientSocket.RemoteEndPoint.ToString() + " connected\n");
                    socketPool.Add(clientSocket);
                    threadPool.Add(thread);
                }
            }
        }
    }
}
