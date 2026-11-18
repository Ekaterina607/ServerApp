using System.Net;                
using System.Net.NetworkInformation; 
using System.Net.Sockets;        
using System.Text;               


namespace ClassLibrary1
{

    public class ChatClient
    {
        // Порт, на котором работает серверная часть
        private readonly int _port;

        // локальный IP-адрес текущего компьютера
        public string LocalIP { get; private set; }

        // Список IP-адресов, обнаруженных при сканировании сети
        public List<string> DiscoveredHosts { get; private set; }

        public ChatClient(int port = 1111)
        {
            _port = port;
            LocalIP = GetLocalIPAddress();      // Определяем свой IP
            DiscoveredHosts = new List<string>(); // Инициализация списка хостов
        }


        // Метод получения локального IPv4
        // подключ. к публичному DNS-серверу
        private string GetLocalIPAddress()
        {
            // временный сокет типа Dgram (UDP)
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                // Подключаемся к внешнему адресу
                socket.Connect("1.1.1.1", 65530);

                // Получаем локальную точку IP + порт
                IPEndPoint endPoint = (IPEndPoint)socket.LocalEndPoint;

                // Возвращаем IP-адрес
                return endPoint.Address.ToString();
            }
        }

        // метод для проверки доступности одного IP-адреса с помощью ICMP-запроса (ping).
        private void PingSingleHost(string ip)
        {
            try
            {
                //объект Ping
                using (Ping ping = new Ping())
                {
                    // ping с тайм 100 мс
                    PingReply reply = ping.Send(ip, 100);

                    // Если хост ответил — добавляем его в список обнаруженных
                    if (reply.Status == IPStatus.Success)
                    {
                        // Блокировка, список используется из нескольких потоков
                        lock (DiscoveredHosts)
                        {
                            DiscoveredHosts.Add(ip);
                        }
                    }
                }
            }
            catch
            {
                // исключения игнор
            }
        }

        //
        // Асинхронный метод сканирования локальной подсети
        //Task — завершается после завершения всех ping-запросов
        public async Task ScanNetworkAsync()
        {
            Console.WriteLine("Сканирование сети");
            DiscoveredHosts.Clear();

            // Разбиваем IP на части
            string[] parts = LocalIP.Split('.');

            // Проверка корректности IP
            if (parts.Length != 4)
            {
                Console.WriteLine("Некорректный IP-адрес");
                return;
            }

            // маска подсети
            string sub = parts[0] + "." + parts[1] + "." + parts[2] + ".";

            // Список задач для параллельного выполнения ping-запросов
            List<Task> tasks = new List<Task>();

            // все возможные IP в подсети (от .1 до .254)
            for (int i = 1; i <= 254; i++)
            {
                string ip = sub + i;
                tasks.Add(Task.Run(() => PingSingleHost(ip)));
            }

            // Ждём завершения всех ping-запросов
            await Task.WhenAll(tasks);

            // удаляем собственный IP из списка
            List<string> filtered = new List<string>();
            foreach (string ip in DiscoveredHosts)
            {
                if (ip != LocalIP)
                {
                    filtered.Add(ip);
                }
            }

            // Обновляем список хостов
            DiscoveredHosts = filtered;
        }


        //является ли строка корректным IPv4-адресом
        public bool IsValidIP(string ip)
        {
            // out-параметр dummy не используется, но обязателен для синтаксиса
            IPAddress dummy;
            return IPAddress.TryParse(ip, out dummy);
        }


        // Асинхронная отправка сообщения
        // Устанавливает соединение, отправляет данные и закрывает соединение
        public async Task<bool> SendMessageAsync(string targetIP, string message)
        {
            // Проверка корректности IP-адреса получателя
            if (!IsValidIP(targetIP))
            {
                Console.WriteLine("Некорректный IP-адрес");
                return false;
            }

            try
            {
                // TCP-клиент для подключения к удалённому хосту
                using (TcpClient client = new TcpClient())
                {
                    // TCP-соединение с указанным IP и портом
                    await client.ConnectAsync(targetIP, _port);
                    // сетевой поток для обмена данными
                    NetworkStream stream = client.GetStream();
                    //текст сообщения в байты в кодировке UTF-8
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    // Асинхронно отправляем данные через поток
                    await stream.WriteAsync(data, 0, data.Length);
                    Console.WriteLine("Сообщение отправлено на " + targetIP);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось отправить сообщение на " + targetIP + ": " + ex.Message);
                return false;
            }
        }
    }
}