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
string userMode;
string joinRoomName;

while (true){
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

while (true){
    Console.WriteLine("Оберіть чат: 1 - Загальний, 2 - Кімната:");
    userMode = "chat";
    string chatChoice = Console.ReadLine();
    byte[] chooseChat = Encoding.UTF8.GetBytes($"CHOOSE_CHAT:{chatChoice}");
    sender.Send(chooseChat, chooseChat.Length, endPoint);

    byte[] response = sender.Receive(ref endPoint);
    string responseMessage = Encoding.UTF8.GetString(response);

    if (responseMessage == "Вітаємо. Ви в загальному чаті"){
        Console.WriteLine($"Тепер ви можете надсилати повідомлення в чаті");
        Console.WriteLine("ps: Для того щоб надіслати повідомлення приватно введіть PRIVATE:[ім'я],[повідомлення]");
        Console.WriteLine("ps: Введіть 'exit' для виходу");
        break;
    }
    if (responseMessage == "Створити кімнату? (так/ні)"){
        userMode = "room";
        Console.WriteLine(responseMessage);

        string createRoomChoise = Console.ReadLine();
        if (createRoomChoise.ToLower() == "так"){
            Console.WriteLine("Введіть назву кімнати:");
            joinRoomName = Console.ReadLine();
            Console.WriteLine("Введіть максимальну кількість учасників:");
            string maxParticipants = Console.ReadLine();

            byte[] createRoomMessage = Encoding.UTF8.GetBytes($"CREATE_ROOM:{joinRoomName},{maxParticipants}");
            sender.Send(createRoomMessage, createRoomMessage.Length, endPoint);

            byte[] responseCreateRoom = sender.Receive(ref endPoint);
            string responseCreateRoomMessage = Encoding.UTF8.GetString(responseCreateRoom);

            Console.WriteLine(responseCreateRoomMessage);
            Console.WriteLine($"Тепер ви можете надсилати повідомлення в чаті");
            Console.WriteLine("ps: Введіть 'exit' для виходу");
            break;
        }
        else if (createRoomChoise.ToLower() == "ні"){
            Console.WriteLine("Надсилаємо запит на список кімнат:");
            byte[] listRoomsMessage = Encoding.UTF8.GetBytes($"LIST");
            sender.Send(listRoomsMessage, listRoomsMessage.Length, endPoint);

            byte[] responseRooms = sender.Receive(ref endPoint);
            string responseRoomsMessage = Encoding.UTF8.GetString(responseRooms);

            Console.WriteLine(responseRoomsMessage);
            if (!responseRoomsMessage.StartsWith("Немає доступних кімнат.")){
                Console.WriteLine("Введіть назву кімнати для приєднання:");
                joinRoomName = Console.ReadLine();
                byte[] joinRoomMessage = Encoding.UTF8.GetBytes($"JOIN_ROOM:{joinRoomName}");
                sender.Send(joinRoomMessage, joinRoomMessage.Length, endPoint);

                byte[] receiveMessage = sender.Receive(ref endPoint);
                string receiveMessageJoin = Encoding.UTF8.GetString(receiveMessage);
                if (receiveMessageJoin.StartsWith("Ви приєдналися до кімнати ")){
                    Console.WriteLine(receiveMessageJoin);
                    Console.WriteLine($"Тепер ви можете надсилати повідомлення в чаті");
                    Console.WriteLine("ps: Введіть 'exit' для виходу");
                }
            }
            else{
                continue;
            }
            break;
        }
    }
}
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
    if (message.ToLower().StartsWith("exit")){
        byte[] sendExit = Encoding.UTF8.GetBytes($"{message}");
        sender.Send(sendExit, sendExit.Length, endPoint);
        break;
    }
    string userMessage = userMode == "chat" ? message : $"ROOM_MSG:{message}";
    byte[] send = Encoding.UTF8.GetBytes($"{userMessage}");
    sender.Send(send, send.Length, endPoint);
}