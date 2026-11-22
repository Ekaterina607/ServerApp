using System;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const int port = 1111;
            var client = new ChatClient(port);
            var server = new ChatServer(port);

            Console.WriteLine("Chat");
            Console.WriteLine($"Your Local IP: {client.LocalIP}");

            // Запускаем локальный сервер (на случай, если кто-то отправит сообщение нам)
            server.Start();

            while (true)
            {
                Console.WriteLine("\nEnter server IP (or 'exit' to quit):");
                string targetIP = Console.ReadLine();

                if (string.Equals(targetIP, "exit", StringComparison.OrdinalIgnoreCase)) break;
                if (string.IsNullOrWhiteSpace(targetIP)) continue;

                Console.WriteLine("Enter your message:");
                string message = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(message)) continue;

                await client.SendMessageAsync(targetIP, message);
            }

            server.Stop();
            Environment.Exit(0);
        }
    }
}