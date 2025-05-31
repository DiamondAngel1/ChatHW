using System.Net;
using System.Net.Sockets;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

UdpClient server = new UdpClient(2009);
server.EnableBroadcast = true;
IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 2009);

Dictionary<IPEndPoint,string> clientSubscribe = new Dictionary<IPEndPoint, string>();
HashSet<string> usedLogins = new HashSet<string>();

Console.WriteLine("Сервер запущено. Очікуємо клієнта...");

while (true){
    var data = server.Receive(ref endPoint);
    string message = Encoding.UTF8.GetString(data);
    if (clientSubscribe.ContainsKey(endPoint)){
        Console.WriteLine($"Повідомлення від {endPoint}: {message}");
        byte[] dataToSend = Encoding.UTF8.GetBytes($"{clientSubscribe[endPoint]}: {message}");
        foreach (var client in clientSubscribe){
            if (!client.Key.Equals(endPoint)){
                server.Send(dataToSend, dataToSend.Length, client.Key);
            }

        }
    }
    else if (message.StartsWith("LOGIN:")){
        string login = message.Replace("LOGIN:", "").Trim();
        if (usedLogins.Contains(login)){
            byte[] errorMessage = Encoding.UTF8.GetBytes($"Логін '{login}' вже використовується. Спробуйте інший.");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            continue;
        }
        clientSubscribe[endPoint] = login;
        usedLogins.Add(login);

        Console.WriteLine($"Клієнт {endPoint} приєднався: {login}");
        byte[] welcomeMessage = Encoding.UTF8.GetBytes($"✅ {login}, вітаємо у чаті!");
        server.Send(welcomeMessage, welcomeMessage.Length, endPoint);
    }
}