using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace VSRPP8_Client
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket socket;
        private Thread thread;
        private byte[] outcomeBuf;
        private SynchronizationContext synchronizationContext;
        private string lastMessage;
        public MainWindow()
        {
            InitializeComponent();

            synchronizationContext = SynchronizationContext.Current;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            outcomeBuf = new byte[1024];

            try
            {
                socket.Connect("127.0.0.1", 8000);

                thread = new Thread(HandleIncomeMessages);
                thread.Start(synchronizationContext);
            }
            catch (SocketException)
            {
                Outcome.Text = "Невозможно подключиться к серверу";
                SendButton.Click -= Button_Click;
            }
        }

        private void HandleIncomeMessages(object syncContext)
        {
            byte[] incomeBuf = new byte[1024];

            while (true)
            {
                try
                {
                    socket.Receive(incomeBuf);
                } catch (Exception)
                {
                    Environment.Exit(1);
                }

                lastMessage = Encoding.UTF8.GetString(incomeBuf);
                lastMessage = lastMessage.Replace("\0", "");

                ((SynchronizationContext)syncContext).Send(this.AddMessage, lastMessage);
            }
        }

        private void AddMessage(object message)
        {
            Income.AppendText("\n" + message as string);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            outcomeBuf = Encoding.UTF8.GetBytes(Outcome.Text);

            socket.Send(outcomeBuf);

            Outcome.Text = "";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            socket.Close();

            if (thread != null)
            {
                thread.Abort();
            }
        }
    }
}
