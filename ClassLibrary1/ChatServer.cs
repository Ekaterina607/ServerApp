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
        private TcpListener listener;
        private bool run = false; // флаг, запущен ли сервер

        public ChatServer(int port = 1111)
        {
            _port = port;
        }

        // Запуск сервера
        public void Start()
        {
            if (run)
                return;

            try
            {
                // Создание локальной точки (IP + порт)
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, _port);

                // Создание TcpListener (инкапсулирует сокет)
                listener = new TcpListener(localEndPoint);
                listener.Start();
                run = true;

                Console.WriteLine($"\nСервер запущен на порту {_port}. Ожидание сообщений...\n");

                // приёма др.польз.
                Task.Run(AcceptClientsLoop);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось запустить сервер: {ex.Message}");
            }
        }

        // ожидание входящих подключений
        private async void AcceptClientsLoop()
        {
            while (run)
            {
                try
                {
                    // Ожидание входящего запроса на соединение
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    // Обработка клиента в отдельном потоке
                    Task.Run(() => HandleClientAsync(client));
                }
                catch (ObjectDisposedException)
                {
                    // TcpListener остановлен — выходим
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Сервер] Ошибка при приёме клиента: {ex.Message}");
                }
            }
        }


        // Обработка одного польз. для чтения сообщения и вывод с IP
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    // Получение сетевого потока для обмена данными
                    NetworkStream stream = client.GetStream();

                    // Чтение данных из потока
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead <= 0)
                        return;

                    // Преобразование байтов в строку
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim('\0');

                    // Получение IP-адреса отправителя
                    IPEndPoint remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    string senderIP = remoteEndPoint.Address.ToString();

                    // Вывод сообщения с IP в консоль
                    Console.WriteLine($"\n[Сообщение от {senderIP}]: {message}\n");
                    Console.Write("Введите IP-адрес получателя: ");

                    // === НАЧАЛО БЛОКА ЗАПИСИ В ФАЙЛ ===
                    // Путь к файлу внутри контейнера. Сюда мы примонтируем том Docker.
                    string logPath = "/app/data/chat_history.txt";
                    string logEntry = $"{DateTime.Now}: {senderIP}: {message}{Environment.NewLine}";
                    try
                    {
                        File.AppendAllText(logPath, logEntry);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка записи в лог: {ex.Message}");
                    }
                    // === КОНЕЦ БЛОКА ЗАПИСИ В ФАЙЛ ===
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        // Остановка сервера
        public void Stop()
        {
            if (!run)
                return;

            run = false;
            listener?.Stop();
            listener?.Dispose();
        }
    }
}