
namespace ClassLibrary1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const int port = 1111;
            var client = new ChatClient(port);
            var server = new ChatServer(port);
            Console.WriteLine("Чат по локальной сети");
            Console.WriteLine($"Ваш IP: {client.LocalIP}");

            await client.ScanNetworkAsync();

            Console.WriteLine("\nНайденные хосты:");
            foreach (var ip in client.DiscoveredHosts)
            {
                Console.WriteLine($" - {ip}");
            }

            server.Start();

            while (true)
            {
                Console.WriteLine("\nВведите IP-адрес получателя:");
                string targetIP = Console.ReadLine();
                if (string.Equals(targetIP, "exit", StringComparison.OrdinalIgnoreCase))
                    break;
                Console.WriteLine("Введите сообщение:");
                string message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message)) continue;

                await client.SendMessageAsync(targetIP, message);
            }

            server.Stop();
            Environment.Exit(0);
        }
    }
}