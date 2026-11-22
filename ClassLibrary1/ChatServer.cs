using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class ChatServer
    {
        private readonly int _port;
        private TcpListener _listener;
        private bool _isRunning = false;

        public ChatServer(int port = 1111)
        {
            _port = port;
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, _port);
                _listener = new TcpListener(localEndPoint);
                _listener.Start();
                _isRunning = true;

                Console.WriteLine($"\nServer started on port {_port}\n");
                Task.Run(AcceptClientsLoop);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server start error: {ex.Message}");
            }
        }

        private async void AcceptClientsLoop()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Task.Run(() => HandleClientAsync(client));
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Accept error: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead <= 0) return;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim('\0');
                    IPEndPoint remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    string senderIP = remoteEndPoint.Address.ToString();

                    Console.WriteLine($"\n[Message from {senderIP}]: {message}\n");

                    // ВАЖНО: Путь к файлу внутри Docker-контейнера (с учетом регистра, как в Dockerfile)
                    string logPath = "/App/data/chat_history.txt";
                    string logEntry = $"{DateTime.Now}: {senderIP}: {message}{Environment.NewLine}";

                    try
                    {
                        File.AppendAllText(logPath, logEntry);
                        Console.WriteLine("Message saved to file.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Log write error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Message handling error: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _listener?.Stop();
            _listener?.Dispose();
        }
    }
}