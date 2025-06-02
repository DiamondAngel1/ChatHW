using System.Net;
using System.Net.Sockets;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

UdpClient sender = new UdpClient();
sender.EnableBroadcast = true;
IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 2009);

Console.WriteLine("Адмін запущено");
string login, password;

while (true){
    Console.WriteLine("Вхід:");
    Console.WriteLine("Введіть логін:");
    login = Console.ReadLine();
    Console.WriteLine("Введіть пароль:");
    password = Console.ReadLine();

    byte[] sendMessage = Encoding.UTF8.GetBytes($"LOGIN:{login},{password},ADMIN");
    sender.Send(sendMessage, sendMessage.Length, endPoint);

    byte[] response = sender.Receive(ref endPoint);
    string responseMessage = Encoding.UTF8.GetString(response);

    if (responseMessage.StartsWith("Логін") || responseMessage.StartsWith("Невірний")){
        Console.WriteLine(responseMessage);
        Console.WriteLine("Спробуйте ще раз");
        continue;
    }
    else{
        Console.WriteLine(responseMessage);
        break;
    }
}

Console.WriteLine("Тепер ви можете надсилати повідомлення в чаті");
Console.WriteLine("ps: Для видалення користувача - DELETE:[логін]");
Console.WriteLine("ps: Для бану користувача - BAN:[логін],[час у хвилинах]");
Console.WriteLine("ps: Введіть 'exit' для виходу");

Task.Run(() =>{
    IPEndPoint anyEndPoint = new IPEndPoint(IPAddress.Any, 0);
    while (true){
        try{
            byte[] data = sender.Receive(ref anyEndPoint);
            string receiveMessage = Encoding.UTF8.GetString(data);
            Console.WriteLine(receiveMessage);
        }
        catch (SocketException ex){
            Console.WriteLine($"Помилка отримання повідомлення: {ex.Message}");
        }
    }
});

while (true){
    string message = Console.ReadLine();
    if (message.ToLower() == "exit"){
        byte[] sendExit = Encoding.UTF8.GetBytes("EXIT");
        sender.Send(sendExit, sendExit.Length, endPoint);
        break;
    }
    byte[] send = Encoding.UTF8.GetBytes(message);
    sender.Send(send, send.Length, endPoint);
}