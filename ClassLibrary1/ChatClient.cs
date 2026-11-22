using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class ChatClient
    {
        private readonly int _port;
        public string LocalIP { get; private set; }

        public ChatClient(int port = 1111)
        {
            _port = port;
            LocalIP = GetLocalIPAddress();
        }

        // Безопасное получение локального IP без интернета
        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                // Сначала ищем IP из нашей внутренней сети 192.168.13.x
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString().StartsWith("192.168.13."))
                        return ip.ToString();
                }
                // Если не нашли, возвращаем первый попавшийся IPv4
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }

        public bool IsValidIP(string ip)
        {
            return IPAddress.TryParse(ip, out _);
        }

        public async Task<bool> SendMessageAsync(string targetIP, string message)
        {
            if (!IsValidIP(targetIP))
            {
                Console.WriteLine("Error: Invalid IP address format.");
                return false;
            }

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(targetIP, _port);
                    NetworkStream stream = client.GetStream();
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);
                    Console.WriteLine($"Message sent to {targetIP}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to send to {targetIP}. Reason: {ex.Message}");
                return false;
            }
        }
    }
}