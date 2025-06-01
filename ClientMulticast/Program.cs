using System.Net;
using System.Net.Sockets;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

UdpClient sender = new UdpClient();
sender.EnableBroadcast = true;
IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 2009);
Console.WriteLine("Клієнт запущено");
string login, password;

while (true)
{
    Console.WriteLine("Оберіть дію: 1 - Руєстрація, 2 - Вхід:");
    string choise = Console.ReadLine();

    Console.WriteLine("Введіть логін:");
    login = Console.ReadLine();
    Console.WriteLine("Введіть пароль:");
    password = Console.ReadLine();

    string authMessage = choise == "1" ? $"REGISTER:{login},{password}" : $"LOGIN:{login},{password}";
    byte[] sendMessage = Encoding.UTF8.GetBytes(authMessage);
    sender.Send(sendMessage, sendMessage.Length, endPoint);

    byte[] response = sender.Receive(ref endPoint);
    string responseMessage = Encoding.UTF8.GetString(response);

    if (responseMessage.StartsWith("Логін") || responseMessage.StartsWith("Невірний"))
    {
        Console.WriteLine(responseMessage);
        Console.WriteLine("Спробуйте ще раз");
        continue;

    }
    else
    {
        Console.WriteLine(responseMessage);
        Console.WriteLine($"Тепер ви можете надсилати повідомлення в чаті");
        Console.WriteLine("Для того щоб надіслати повідомлення приватно введіть PRIVATE:[ім'я],[повідомлення]");
        Console.WriteLine("Введіть 'exit' для виходу");
        break;
    }
}


Task.Run(() => {
    while (true)
    {
        try
        {
            byte[] data = sender.Receive(ref endPoint);
            string receiveMessage = Encoding.UTF8.GetString(data);
            Console.WriteLine(receiveMessage);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Помилка отримання повідомлення: {ex.Message}");
        }
    }
});

while (true)
{
    string message = Console.ReadLine();
    if (message.ToLower().StartsWith("exit"))
    {
        byte[] sendExit = Encoding.UTF8.GetBytes($"{message}");
        sender.Send(sendExit, sendExit.Length, endPoint);
        break;
    }
    byte[] send = Encoding.UTF8.GetBytes($"{message}");
    sender.Send(send, send.Length, endPoint);
}