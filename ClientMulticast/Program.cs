using System.Net;
using System.Net.Sockets;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

UdpClient sender = new UdpClient();
sender.EnableBroadcast = true;
IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 2009);
Console.WriteLine("Клієнт запущено");
string login;


while (true){
    Console.WriteLine("Введіть логін:");
    login = Console.ReadLine();
    byte[] sendMessage = Encoding.UTF8.GetBytes($"LOGIN:{login}");
    sender.Send(sendMessage, sendMessage.Length, endPoint);

    byte[] response = sender.Receive(ref endPoint);
    string responseMessage = Encoding.UTF8.GetString(response);
    if (responseMessage.StartsWith("Логін")){
        Console.WriteLine(responseMessage);
    }
    else{
        Console.WriteLine(responseMessage);
        break;
    }
}

Console.WriteLine($"Тепер ви можете надсилати повідомлення в чаті");
Task.Run(() => {
    while (true){
        try{
            byte[] data = sender.Receive(ref endPoint);
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
    byte[] send = Encoding.UTF8.GetBytes($"{message}");
    sender.Send(send, send.Length, endPoint);
}