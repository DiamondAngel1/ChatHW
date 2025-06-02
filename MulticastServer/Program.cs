using System.Collections.Generic;
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
Dictionary<string, (List<IPEndPoint>users, int maxUsers)> chatRooms = new Dictionary<string, (List<IPEndPoint>, int)>();
Dictionary<IPEndPoint, string> userRoom = new Dictionary<IPEndPoint, string>();
List<string> forbiddenWords = new List<string> { "привіт", "бу", "капець" };
Dictionary<string, DateTime> bannedUsers = new Dictionary<string, DateTime>();
List<IPEndPoint> adminList = new List<IPEndPoint>();

string adminLogin = "admin";
string adminPassword = "admin123";

Console.WriteLine("Сервер запущено. Очікуємо клієнта...");

while (true){
   
    var data = server.Receive(ref endPoint);
    string message = Encoding.UTF8.GetString(data);

    bool hadForbidden = false;
    string[] words = message.Split(' ');
    StringBuilder censoredMessageBuilder = new StringBuilder();

    foreach (string w in words){
        string cleanedWord = w.Trim('.', '!', '?', ';', '(', ')').ToLower();

        if (forbiddenWords.Contains(cleanedWord)){
            hadForbidden = true;
            censoredMessageBuilder.Append("### ");
        }
        else{
            censoredMessageBuilder.Append(w + " ");
        }
    }
    string censoredMessage = censoredMessageBuilder.ToString().TrimEnd();

    if (hadForbidden)
    {
        string alert = $"Користувач відправив заборонене слово";
        Console.WriteLine(alert);

        foreach (var adminEP in adminList)
        {
            byte[] alertBytes = Encoding.UTF8.GetBytes(alert);
            server.Send(alertBytes, alertBytes.Length, adminEP);
        }
    }
    if (message.ToLower().StartsWith("exit")){
        if (clientSubscribe.ContainsKey(endPoint)){
            string login = clientSubscribe[endPoint].login;
            clientSubscribe.Remove(endPoint);

            byte[] disconnectMessage = Encoding.UTF8.GetBytes($"{login} вийшов з чату.");
            if (userRoom.ContainsKey(endPoint)){
                string roomName = userRoom[endPoint];

                if(chatRooms.ContainsKey(roomName)){
                    chatRooms[roomName].users.Remove(endPoint);
                    Console.WriteLine($"Користувач {login} вийшов з кімнати {roomName}.");

                    foreach (var client in chatRooms[roomName].users){
                        server.Send(disconnectMessage, disconnectMessage.Length, client);
                    }

                    if (chatRooms[roomName].users.Count == 0){
                        chatRooms.Remove(roomName);
                        Console.WriteLine($"Кімната {userRoom[endPoint]} видалена, оскільки в ній більше немає учасників.");
                    }
                }
                userRoom.Remove(endPoint);
            }
            else{
                foreach (var client in clientSubscribe.Keys){
                    if (!client.Equals(endPoint)){
                        server.Send(disconnectMessage, disconnectMessage.Length, client);
                    }
                }
            }
        }
    }

    else if (message.StartsWith("BAN:")){
        if (!adminList.Contains(endPoint)){
            continue;
        }
        string[] parts = message.Replace("BAN:", "").Trim().Split(',');
        if (parts.Length < 2){
            continue;
        }
        string userToBan = parts[0].Trim();
        int banDuration = int.Parse(parts[1].Trim());

        if (!registeredUsers.ContainsKey(userToBan)){
            byte[] errorMessage = Encoding.UTF8.GetBytes($"Користувач {userToBan} не знайдений");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            continue;
        }
        bannedUsers[userToBan] = DateTime.Now.AddMinutes(banDuration);
        byte[] banMessage = Encoding.UTF8.GetBytes($"Користувач {userToBan} заблокований на {banDuration} хвилин.");
        server.Send(banMessage, banMessage.Length, endPoint);
    }

    else if (message.StartsWith("DELETE:")){
        if (!adminList.Contains(endPoint)){
            continue;
        }
        string targetLogin = message.Replace("DELETE:", "").Trim();
        if (!registeredUsers.ContainsKey(targetLogin)){
            byte[] errorMessage = Encoding.UTF8.GetBytes($"Користувач {targetLogin} не знайдено");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            continue;
        }
        IPEndPoint targetEP = null;
        foreach(var kvp in clientSubscribe){
            if (kvp.Value.login == targetLogin){
                targetEP = kvp.Key;
                break;
            }
        }
        registeredUsers.Remove(targetLogin);
        clientSubscribe.Remove(targetEP);
        byte[] successMessage = Encoding.UTF8.GetBytes($"Користувач {targetLogin} видалено");
        server.Send(successMessage, successMessage.Length, endPoint);
        Console.WriteLine($"Користувач {targetLogin} видалений адміністратором");
    }

    else if (message.StartsWith("CHOOSE_CHAT:")){
        string chatChoice = message.Replace("CHOOSE_CHAT:", "").Trim();

        if (chatChoice == "1"){
            userRoom[endPoint] = "chat";

            clientSubscribe[endPoint] = (clientSubscribe[endPoint].login, clientSubscribe[endPoint].password);
            Console.WriteLine($"Клієнт {endPoint} обрав загальний чат");
            byte[] response = Encoding.UTF8.GetBytes("Вітаємо. Ви в загальному чаті");
            server.Send(response, response.Length, endPoint);

            string joinMessage = $"{clientSubscribe[endPoint].login} приєднався до зпгпльного чату";
            byte[] joinMessageData = Encoding.UTF8.GetBytes(joinMessage);
            foreach (var client in clientSubscribe.Keys){
                if (!client.Equals(endPoint)){
                    server.Send(joinMessageData, joinMessageData.Length, client);
                }
            }
        }
        else if (chatChoice == "2"){
            Console.WriteLine($"Клієнт {endPoint} обрав кімнату");
            byte[] quest = Encoding.UTF8.GetBytes("Створити кімнату? (так/ні)");
            server.Send(quest, quest.Length, endPoint);

            byte[] response = server.Receive(ref endPoint);
            string responseMessage = Encoding.UTF8.GetString(response).Trim();

            if (responseMessage.StartsWith("LIST")){
                Console.WriteLine($"Користувач {clientSubscribe[endPoint].login} обрав перегляд доступних кімнат.");

                var availableRooms = chatRooms
                    .Where(room => room.Value.users.Count < room.Value.maxUsers)
                    .Select(room => $"{room.Key} (Учасники: {room.Value.users.Count}/{room.Value.maxUsers})")
                    .ToList();
                string roomListMessage = availableRooms.Any() ? $"Доступні кімнати: {string.Join("\n",availableRooms)}" : "Немає доступних кімнат.";
                byte[] roomListData = Encoding.UTF8.GetBytes(roomListMessage);
                server.Send(roomListData, roomListData.Length, endPoint);
            }
            else if (responseMessage.StartsWith("CREATE_ROOM")){
                Console.WriteLine($"Користувач {clientSubscribe[endPoint].login} обрав створення кімнати.");

                string[] parts = responseMessage.Replace("CREATE_ROOM:", "").Trim().Split(',');
                if (parts.Length < 2){
                    continue;
                }
                string roomName = parts[0].Trim();
                int maxUsers = int.Parse(parts[1].Trim());
                if (chatRooms.ContainsKey(roomName)){
                    byte[] errorMessage = Encoding.UTF8.GetBytes($"Кімната з назвою '{roomName}' вже існує.");
                    server.Send(errorMessage, errorMessage.Length, endPoint);
                    continue;
                }
                else{
                    chatRooms[roomName] = (new List<IPEndPoint> { endPoint }, maxUsers);
                    userRoom[endPoint] = roomName;
                    clientSubscribe[endPoint] = (clientSubscribe[endPoint].login, clientSubscribe[endPoint].password);
                    byte[] successMessage = Encoding.UTF8.GetBytes($"Ви створили кімнату '{roomName}'. Максимальна кількість учасників: {maxUsers}.");
                    server.Send(successMessage, endPoint);

                    Console.WriteLine($"Кімната '{roomName}' створена користувачем {clientSubscribe[endPoint].login}.");
                    continue;
                }
            }

        }
    }

    else if (message.StartsWith("JOIN_ROOM:")){
        string roomName = message.Replace("JOIN_ROOM:", "").Trim();

        if (!chatRooms.ContainsKey(roomName)){
            byte[] errorMessage = Encoding.UTF8.GetBytes($"Кімната '{roomName}' не знайдена.");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            continue;
        }
        else if (chatRooms[roomName].users.Count >= chatRooms[roomName].maxUsers){
            byte[] errorMessage = Encoding.UTF8.GetBytes($"Кімната '{roomName}' переповнена.");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            continue;
        }
        else{
            chatRooms[roomName].users.Add(endPoint);
            userRoom[endPoint] = roomName;

            byte[] successMessage = Encoding.UTF8.GetBytes($"Ви приєдналися до кімнати '{roomName}'.");
            server.Send(successMessage, successMessage.Length, endPoint);

            string joinMsg = $"{clientSubscribe[endPoint].login} приєднався до кімнати {roomName}.";
            byte[] joinMessageData = Encoding.UTF8.GetBytes(joinMsg);
            foreach (var client in chatRooms[roomName].users){
                if (!client.Equals(endPoint)){
                    server.Send(joinMessageData, joinMessageData.Length, client);
                }
            }
            Console.WriteLine($"Користувач {clientSubscribe[endPoint].login} приєднався до кімнати {roomName}");
        }
    }

    else if (message.StartsWith("ROOM_MSG:")){
        string senderLogin = clientSubscribe.ContainsKey(endPoint) ? clientSubscribe[endPoint].login : null;

        if (senderLogin == null || !registeredUsers.ContainsKey(senderLogin)){
            byte[] error = Encoding.UTF8.GetBytes("Вас видалено з сервера. Повідомлення не дозволено.");
            server.Send(error, error.Length, endPoint);
            continue;
        }

        if (bannedUsers.ContainsKey(senderLogin)){
            if (DateTime.Now < bannedUsers[senderLogin]){
                byte[] banMsg = Encoding.UTF8.GetBytes($"Ви заблоковані до {bannedUsers[senderLogin]}");
                server.Send(banMsg, banMsg.Length, endPoint);
                continue;
            }
            else{
                bannedUsers.Remove(senderLogin);
            }
        }
        if (!userRoom.ContainsKey(endPoint)) {
            byte[] errorMessage = Encoding.UTF8.GetBytes("Ви не в кімнаті. Приєднайтесь до кімнати перед надсиланням повідомлень.");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            return;
        }
        string roomName = userRoom[endPoint];
        string roomMessage = message.Replace("ROOM_MSG:", "").Trim();
        byte[] roomMessageData = Encoding.UTF8.GetBytes($"{senderLogin} (Кімната {roomName}): {roomMessage}");
        foreach (var client in chatRooms[roomName].users){
            if (!clientSubscribe.ContainsKey(client))
                continue;

            if (!registeredUsers.ContainsKey(senderLogin))
                continue;

            if (bannedUsers.ContainsKey(senderLogin)&& DateTime.Now < bannedUsers[senderLogin])
                continue;

            server.Send(roomMessageData, roomMessageData.Length, client);
        }
        foreach (var admin in clientSubscribe){
            var client = admin.Key;
            var user = admin.Value;
            if (user.login == "admin"){
                server.Send(roomMessageData,roomMessageData.Length,client);
            }
        }
        Console.WriteLine($"Повідомлення в кімнаті {roomName} від {senderLogin}: {roomMessage}");
    }

    else if (clientSubscribe.ContainsKey(endPoint)){
        string login = clientSubscribe[endPoint].login;
        if (bannedUsers.ContainsKey(login)){
            if(bannedUsers[login] > DateTime.Now){
                byte[] errorMessage = Encoding.UTF8.GetBytes($"Ви заблоковані до {bannedUsers[login]}");
                server.Send(errorMessage, errorMessage.Length, endPoint);
                continue;
            }
            else{
                bannedUsers.Remove(login);
                Console.WriteLine($"З користувача {login} знятий бан");
            }
        }
        if (!registeredUsers.ContainsKey(login)){
            byte[] msg = Encoding.UTF8.GetBytes("Ваш обліковий запис було видалено.");
            server.Send(msg, msg.Length, endPoint);

            clientSubscribe.Remove(endPoint);
            Console.WriteLine($"Видалено підключення користувача з видаленим логіном: {login}");
            continue;
        }
        if (registeredUsers.ContainsKey(clientSubscribe[endPoint].login)) {
            if (bannedUsers.ContainsKey(login)){
                if (DateTime.Now < bannedUsers[login]){
                    byte[] errorMessage = Encoding.UTF8.GetBytes($"Ви заблоковані до {bannedUsers[login]}");
                    server.Send(errorMessage, errorMessage.Length, endPoint);
                    return;
                }
                else{
                    bannedUsers.Remove(login);
                }
            }
            if (hadForbidden==true)
            {
                Console.WriteLine($"Повідомлення від {login}: {censoredMessage}");
                byte[] dataToSend = Encoding.UTF8.GetBytes($"{login}: {censoredMessage}");
                foreach (var client in clientSubscribe)
                {
                    IPEndPoint clientEP = client.Key;
                    if (!registeredUsers.ContainsKey(login))
                        continue;
                    if (bannedUsers.ContainsKey(login) && DateTime.Now < bannedUsers[login])
                        continue;
                    if (userRoom.ContainsKey(clientEP) && !string.IsNullOrWhiteSpace(userRoom[clientEP]))
                        continue;

                    if (!client.Key.Equals(endPoint))
                    {
                        server.Send(dataToSend, dataToSend.Length, client.Key);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Повідомлення від {login}: {message}");
                byte[] dataToSend = Encoding.UTF8.GetBytes($"{login}: {message}");
                foreach (var client in clientSubscribe) {
                    IPEndPoint clientEP = client.Key;
                    if (!registeredUsers.ContainsKey(login))
                        continue;
                    if (bannedUsers.ContainsKey(login) && DateTime.Now < bannedUsers[login])
                        continue;
                    if (userRoom.ContainsKey(clientEP) && !string.IsNullOrWhiteSpace(userRoom[clientEP]))
                        continue;
                    if (!client.Key.Equals(endPoint)) {
                        server.Send(dataToSend, dataToSend.Length, client.Key);
                    }
                }
            }
        }
    }

    else if (message.StartsWith("PRIVATE:")){
        string[] parts = message.Replace("PRIVATE:", "").Trim().Split(',');
        if (parts.Length < 2){
            continue;
        }
        string targetUser = parts[0].Trim();
        string privateMessage = parts[1].Trim();
        IPEndPoint? targetClient = clientSubscribe
            .FirstOrDefault(c => c.Value.login == targetUser).Key;

        if (targetClient != null){
            string senderLogin = clientSubscribe[endPoint].login;
            string privateMessageToSend = $"{senderLogin} (Приватне): {privateMessage}";
            byte[] privateDataToSend = Encoding.UTF8.GetBytes(privateMessageToSend);
            server.Send(privateDataToSend, privateDataToSend.Length, targetClient);

            Console.WriteLine($"Приватне повідомлення від {senderLogin} для {targetUser}: {privateMessage}");
        }
        else{
            byte[] errorMessage = Encoding.UTF8.GetBytes($"Користувач {targetUser} не знайдений.");
            server.Send(errorMessage, errorMessage.Length, endPoint);
        }
    }

    else if (message.StartsWith("REGISTER:")){
        string[] parts = message.Replace("REGISTER:", "").Trim().Split(',');
        if (parts.Length != 2){
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
        continue;
    }

    else if (message.StartsWith("LOGIN:")){
        string[] parts = message.Replace("LOGIN:", "").Split(",");
        if (parts.Length < 2){
            continue;
        }
        string login = parts[0].Trim();
        string password = parts[1].Trim();

        if (login == adminLogin && password == adminPassword){
            Console.WriteLine("Адміністратор на базі");

            byte[] adminMessage = Encoding.UTF8.GetBytes("Вітаємо, ви увійшли як адміністратор");
            server.Send(adminMessage, adminMessage.Length, endPoint);
            adminList.Add(endPoint);
            registeredUsers[adminLogin] = adminPassword;
        }
        if (bannedUsers.ContainsKey(login)){
            if (bannedUsers[login] > DateTime.Now){
                byte[] errorMessage = Encoding.UTF8.GetBytes($"Ви заблоковані до {bannedUsers[login]}");
                server.Send(errorMessage, errorMessage.Length, endPoint);
                return;
            }
            else{
                bannedUsers.Remove(login);
            }
        }
        if (!registeredUsers.ContainsKey(login) || registeredUsers[login] != password){
            byte[] errorMessage = Encoding.UTF8.GetBytes("Невірний логін або пароль. Спробуйте ще раз.");
            server.Send(errorMessage, errorMessage.Length, endPoint);
            continue;
        }
        clientSubscribe[endPoint] = (login, password);
        Console.WriteLine($"Клієнт {endPoint} приєднався: {login}");
        byte[] sendNewUser = Encoding.UTF8.GetBytes($"{login} приєднався до чату.");
        foreach (var client in clientSubscribe){
            if (!client.Key.Equals(endPoint)){
                server.Send(sendNewUser, sendNewUser.Length, client.Key);
            }
        }
    }
}