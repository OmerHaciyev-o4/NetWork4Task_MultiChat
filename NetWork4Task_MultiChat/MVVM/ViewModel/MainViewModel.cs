using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetWork4Task_MultiChat.MVVM.Commands;

namespace NetWork4Task_MultiChat.MVVM.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        #region Private Varibale

        private TcpClient _client;
        private StreamReader _sr;
        private StreamWriter _sw;
        private string _receive;
        private string _textToSend;

        private readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, 0);
        private readonly BackgroundWorker backgroundWorker1 = new BackgroundWorker();
        private readonly BackgroundWorker backgroundWorker2 = new BackgroundWorker();

        #endregion

        #region Commands

        public ICommand StartCommand { get; set; }
        public ICommand ConnectCommand { get; set; }
        public ICommand SendCommand { get; set; }

        #endregion

        #region Full Property

        private string serverIPText;
        public string ServerIPText
        {
            get { return serverIPText; }
            set { serverIPText = value; OnPropertyChanged();}
        }

        private string serverPortText;
        public string ServerPortText
        {
            get { return serverPortText; }
            set { serverPortText = value; OnPropertyChanged(); }
        }

        private string clientIPText;
        public string ClientIPText
        {
            get { return clientIPText; }
            set { clientIPText = value; OnPropertyChanged(); }
        }

        private string clientPortText;
        public string ClientPortText
        {
            get { return clientPortText; }
            set { clientPortText = value; OnPropertyChanged(); }
        }
        
        private string sendMessageText;
        public string SendMessageText
        {
            get { return sendMessageText; }
            set { sendMessageText = value; OnPropertyChanged(); }
        }

        #endregion

        #region Auto Property

        public ObservableCollection<ListBoxItem> Items { get; set; }

        #endregion

        #region References

        public MainWindow MainWindow { get; set; }

        #endregion


        public MainViewModel()
        {
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker2.DoWork += BackgroundWorker2_DoWork;
            _client = new TcpClient();
            Items = new ObservableCollection<ListBoxItem>();

            IPAddress[] localAddresses = Dns.GetHostAddresses(Dns.GetHostName());

            foreach ( var localAddress in localAddresses)
            {
                if (localAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    ServerIPText = localAddress.ToString();
                }
            }

            ServerPortText = getAvailablePort().ToString();

            #region Commands

            StartCommand = new RelayCommand((o) =>
            {
                StartServer();
            });

            ConnectCommand = new RelayCommand((o) =>
            {
                ConnectServer();
            });

            SendCommand = new RelayCommand((o) =>
            {
                SendMessageServer();
            });

            #endregion
            
        }

        private void SendMessageServer()
        {
            if (SendMessageText != "")
            {
                _textToSend = SendMessageText;
                backgroundWorker2.RunWorkerAsync();
            }
            SendMessageText = "";
        }

        private void ConnectServer()
        {
            Task.Run(() =>
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ClientIPText), int.Parse(ClientPortText));

                try
                {
                    _client.Connect(endPoint);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Label label = new Label();
                        label.FontSize = 20;
                        label.Content = "Connect To Server";

                        Items.Add(new ListBoxItem(){ Content = label });
                    });

                    _sw = new StreamWriter(_client.GetStream());
                    _sr = new StreamReader(_client.GetStream());
                    _sw.AutoFlush = true;

                    backgroundWorker1.RunWorkerAsync();
                    backgroundWorker2.WorkerSupportsCancellation = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
        }

        private void StartServer()
        {
            Task.Run(() =>
            {
                TcpListener listener = new TcpListener(IPAddress.Parse(ServerIPText), int.Parse(ServerPortText));
                try
                {
                    listener.Start();

                    MessageBox.Show("Server started", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    while (true)
                    {
                        _client = listener.AcceptTcpClient();
                        _sr = new StreamReader(_client.GetStream());
                        _sw = new StreamWriter(_client.GetStream());
                        _sw.AutoFlush = true;

                        backgroundWorker1.RunWorkerAsync();
                        backgroundWorker2.WorkerSupportsCancellation = true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        private int getAvailablePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(DefaultLoopbackEndpoint);
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (_client.Connected)
            {
                try
                {
                    _receive = _sr.ReadLine();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Label label = new Label();
                        label.Content = _client.Client.RemoteEndPoint + " : " + _receive;
                        label.FontSize = 20;

                        Items.Add(new ListBoxItem() { Content = label });

                        _receive = "";
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            if (_client.Connected)
            {
                _sw.WriteLine(_textToSend);
                App.Current.Dispatcher.Invoke(() =>
                {
                    Label label = new Label();
                    label.Content = "Me : " + _textToSend;
                    label.FontSize = 20;

                    Items.Add(new ListBoxItem() { Content = label });
                });
            }
            else
            {
                MessageBox.Show("Sending Failed");
            }
            backgroundWorker2.CancelAsync();
        }
    }
}