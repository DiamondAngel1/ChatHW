using System.Net;
using System.Net.Sockets;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

UdpClient server = new UdpClient(2009);
server.EnableBroadcast = true;
IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 2009);

Dictionary<IPEndPoint,(string login, string password)> clientSubscribe = new Dictionary<IPEndPoint, (string,string)>();
Dictionary<string,string> registeredUsers = new Dictionary<string, string>();

Console.WriteLine("Сервер запущено. Очікуємо клієнта...");

while (true){
   
    var data = server.Receive(ref endPoint);
    string message = Encoding.UTF8.GetString(data);

    if (message.ToLower().StartsWith("exit")){
        if (clientSubscribe.ContainsKey(endPoint)){
            string login = clientSubscribe[endPoint].login;
            clientSubscribe.Remove(endPoint);
            Console.WriteLine($"Користувач {login} відключився.");

            byte[] disconnectMessage = Encoding.UTF8.GetBytes($"{login} вийшов з чату.");
            foreach (var client in clientSubscribe.Keys){
                if (!client.Equals(endPoint)){
                    try{
                        server.Send(disconnectMessage, disconnectMessage.Length, client);
                    }
                    catch (Exception ex){
                        Console.WriteLine($"Помилка надсилання повідомлення відключення: {ex.Message}");
                    }
                } 
            }
        }
    }
    else if (clientSubscribe.ContainsKey(endPoint)){
        if(registeredUsers.ContainsKey(clientSubscribe[endPoint].login)){
            string login = clientSubscribe[endPoint].login;
            string messageToSend = $"{login}: {message}";
            Console.WriteLine($"Повідомлення від {login}: {message}");
            byte[] dataToSend = Encoding.UTF8.GetBytes($"{login}: {message}");
            foreach (var client in clientSubscribe){
                if (!client.Key.Equals(endPoint)){
                    server.Send(dataToSend, dataToSend.Length, client.Key);
                }
            }
        }
    }
    else if (message.StartsWith("REGISTER:")){
        string[] parts = message.Replace("REGISTER:", "").Trim().Split(',');
        if (parts.Length != 2) {
            continue;
        }
        string login = parts[0].Trim();
        string password = parts[1].Trim();

        if (registeredUsers.ContainsKey(login)){
            byte[] errorMessage = Encoding.UTF8.GetBytes($"Логін '{login}' вже використовується. Спробуйте інший.");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            continue;
        }
        registeredUsers[login] = password;
        byte[] successMessage = Encoding.UTF8.GetBytes($"'{login}' успішно зареєстровано.");
        server.Send(successMessage, successMessage.Length, endPoint);
        clientSubscribe[endPoint] = (login, password);

        Console.WriteLine($"Клієнт {endPoint} зареєстрований: {login}");
        byte[] welcomeMessage = Encoding.UTF8.GetBytes($"{login}, вітаємо у чаті!");
        byte[] sendNewUser = Encoding.UTF8.GetBytes($"{login} приєднався до чату.");
        foreach (var client in clientSubscribe){
            if (!client.Key.Equals(endPoint)){
                server.Send(sendNewUser, sendNewUser.Length, client.Key);
            }
        }
        server.Send(welcomeMessage, welcomeMessage.Length, endPoint);
        continue;
    }
    else if (message.StartsWith("LOGIN:")){
        string[] parts = message.Replace("LOGIN:", "").Split(",");
        if(parts.Length< 2){
            continue;
        }
        string login = parts[0].Trim();
        string password = parts[1].Trim();

        if (!registeredUsers.ContainsKey(login) || registeredUsers[login]!=password){
            byte[] errorMessage = Encoding.UTF8.GetBytes("Невірний логін або пароль. Спробуйте ще раз.");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            continue;
        }
        clientSubscribe[endPoint] = (login,password);
        Console.WriteLine($"Клієнт {endPoint} приєднався: {login}");
        byte[] welcomeMessage = Encoding.UTF8.GetBytes($"{login}, вітаємо у чаті!");
        byte[] sendNewUser = Encoding.UTF8.GetBytes($"{login} приєднався до чату.");
        foreach (var client in clientSubscribe){
            if (!client.Key.Equals(endPoint)){
                server.Send(sendNewUser, sendNewUser.Length, client.Key);
            }
        }
        server.Send(welcomeMessage, welcomeMessage.Length, endPoint);
    }
}