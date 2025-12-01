using ChessCommon.SocketUtils;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Collections.Generic;
using ChessCommon.GeneralUtils;

namespace Client
{
    class Program
    {
        static TcpClient tcp;
        static NetworkStream stream;
        static Account myAccount;
        static void Main(string[] args)
        {
            while (true)
            {
                if (!Connect()) { Console.WriteLine("Не удалось подключится к серверу!"); Thread.Sleep(1000); continue; }
                break;
            }

            RemoteFunction remoteFunction = new RemoteFunction(tcp);

            Console.Clear();
            Console.WriteLine("1) Войти в аккаунт\n2) Зарегистрироваться");
            int userChoice = int.Parse(Console.ReadLine());
            switch (userChoice)
            {
                case 1:
                    while (true)
                    {
                        Console.Clear();
                        Console.Write("Логин: ");
                        string login = Console.ReadLine();
                        Console.Write("\nПароль: ");
                        string password = Console.ReadLine();

                        remoteFunction.SendMessage(new LoginRequestArgs(login, password), RemoteFunctionTypes.LoginRequest);

                        Message message;

                        RemoteFunctionTypes messageType;

                        while (true)
                        {
                            message = remoteFunction.ReceiveMessage();
                            messageType = message.Type;
                            if (messageType == RemoteFunctionTypes.LoginResponse) break;
                        }

                        LoginResponseArgs response = RemoteFunction.ConvertPayloadIntoArgs<LoginResponseArgs>(message.Payload); 

                        if (response.Error != ResponseTypes.Success)
                        {
                            Console.WriteLine($"{ErrorTypesConverter.GetStringByErrorType(response.Error)}");
                            Console.ReadLine();
                        }
                        else
                        {
                            myAccount = response.Account;
                            break;
                        }
                    }

                    break;
                case 2:
                    while (true)
                    {
                        Console.Clear();
                        Console.Write("Логин: ");
                        string login = Console.ReadLine();
                        Console.Write("\nПароль: ");
                        string password = Console.ReadLine();

                        remoteFunction.SendMessage(new RegisterRequestArgs(login, password), RemoteFunctionTypes.RegisterRequest);

                        Message message;

                        RemoteFunctionTypes messageType;

                        while (true)
                        {
                            message = remoteFunction.ReceiveMessage();
                            messageType = message.Type;
                            if (messageType == RemoteFunctionTypes.LoginResponse) break;
                        }

                        LoginResponseArgs response = RemoteFunction.ConvertPayloadIntoArgs<LoginResponseArgs>(message.Payload);

                        if (response.Error != ResponseTypes.Success)
                        {
                            Console.WriteLine($"{ErrorTypesConverter.GetStringByErrorType(response.Error)}");
                            Console.ReadLine();
                        }
                        else
                        {
                            myAccount = response.Account;
                            break;
                        }
                    }
                    break;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"{myAccount.Username} - {myAccount.Wins}, {myAccount.Loses} ({myAccount.Id})");
                Console.WriteLine("1) Найти игру\n2) Создать игру");

                userChoice = int.Parse(Console.ReadLine());
                Console.Clear();

                Field gameField = null;
                Account whitePlayer = null;
                Account blackPlayer = null;
                NextActionType nextAction = NextActionType.WhiteTurn;

                switch (userChoice)
                {
                    case 1:
                        while (true)
                        {
                            remoteFunction.SendMessage(new GetLobbiesRequestArgs(), RemoteFunctionTypes.GetLobbiesRequest);

                            Message message;

                            RemoteFunctionTypes messageType;

                            while (true)
                            {
                                message = remoteFunction.ReceiveMessage();
                                messageType = message.Type;
                                if (messageType == RemoteFunctionTypes.GetLobbiesResponse) break;
                                Console.WriteLine("WrongType");
                            }

                            List<PublicLobby> lobbiesList = RemoteFunction.ConvertPayloadIntoArgs<GetLobbiesResponseArgs>(message.Payload).Lobbies;
                            foreach (PublicLobby lobby in lobbiesList)
                            {
                                Console.WriteLine($"[{lobby.Id}] {lobby.Player1.Username}, {lobby.Player1.Wins} - {lobby.Player1.Loses}");
                            }

                            Console.Write("Введите номер лобби: ");
                            int lobbyId = int.Parse(Console.ReadLine());

                            PublicLobby selectedLobby = null;

                            foreach (PublicLobby lobby in lobbiesList)
                            {
                                if (lobby.Id == lobbyId)
                                {
                                    selectedLobby = lobby;
                                    break;
                                }
                            }

                            if (selectedLobby is null)
                            {
                                Console.WriteLine("Введите существующий номер лобби!");
                                Console.ReadLine();
                                continue;
                            }

                            string password = string.Empty;

                            if (selectedLobby.IsPrivate)
                            {
                                Console.WriteLine("Введите пароль лобби: ");
                                password = Console.ReadLine();
                            }

                            remoteFunction.SendMessage(new LobbyJoinRequestArgs(selectedLobby.Id, password), RemoteFunctionTypes.LobbyJoinRequest);

                            while (true)
                            {
                                message = remoteFunction.ReceiveMessage();
                                messageType = message.Type;
                                if (messageType == RemoteFunctionTypes.LobbyJoinResponse) break;
                            }

                            LobbyJoinResponseArgs resp = RemoteFunction.ConvertPayloadIntoArgs<LobbyJoinResponseArgs>(message.Payload);

                            if (resp.Error != ResponseTypes.Success)
                            {
                                Console.WriteLine(ErrorTypesConverter.GetStringByErrorType(resp.Error));
                                Console.ReadLine();
                                continue;
                            }

                            gameField = resp.UpdateField.GameField;
                            whitePlayer = resp.UpdateField.Account1;
                            blackPlayer = resp.UpdateField.Account2;

                            break;
                        }

                        break;
                    case 2:
                        while (true)
                        {
                            Console.WriteLine("Введите пароль для лобби: ");
                            string lobbyPassword = Console.ReadLine();
                            remoteFunction.SendMessage(new LobbyCreateRequestArgs(lobbyPassword), RemoteFunctionTypes.LobbyCreateRequest);

                            Message message;
                            RemoteFunctionTypes messageType;
                            while (true)
                            {
                                message = remoteFunction.ReceiveMessage();
                                messageType = message.Type;
                                if (messageType == RemoteFunctionTypes.LobbyCreateResponse) break;
                            }

                            LobbyCreateResponseArgs resp = RemoteFunction.ConvertPayloadIntoArgs<LobbyCreateResponseArgs>(message.Payload);

                            if (resp.Error != ResponseTypes.Success)
                            {
                                Console.WriteLine(ErrorTypesConverter.GetStringByErrorType(resp.Error));
                                Console.ReadLine();
                                continue;
                            }

                            while (true)
                            {
                                message = remoteFunction.ReceiveMessage();
                                messageType = message.Type;
                                if (messageType == RemoteFunctionTypes.UpdateFieldRequest) break;
                            }

                            UpdateFieldRequestArgs respField = RemoteFunction.ConvertPayloadIntoArgs<UpdateFieldRequestArgs>(message.Payload);

                            gameField = respField.GameField;
                            whitePlayer = respField.Account1;
                            blackPlayer = respField.Account2;

                            break;
                        }
                        break;
                }

                while (true)
                {
                    Console.Clear();

                    Console.WriteLine($"{blackPlayer.Username}, {blackPlayer.Wins}, {blackPlayer.Loses}");
                    RenderField(gameField);
                    Console.WriteLine($"{whitePlayer.Username}, {whitePlayer.Wins}, {whitePlayer.Loses}");
                    Console.WriteLine();
                    Console.WriteLine("K=King, Q=Queen, R=Rook, B=Bishop, H=Knight, P=Pawn");
                    Console.WriteLine();
                    Console.WriteLine($"Для того что-бы походить напишите куда и откуда нужно это сделать. (Например: \"2A 4A\")");

                    if (nextAction == NextActionType.WhiteTurn && whitePlayer.Id == myAccount.Id ||
                        nextAction == NextActionType.BlackTurn && blackPlayer.Id == myAccount.Id)
                    {
                        string[] input = Console.ReadLine().Split();

                        PositionInField from;
                        PositionInField to;

                        try
                        {
                            from = new PositionInField(int.Parse(input[0][0].ToString()) - 1, char.Parse(input[0][1].ToString()));
                            to = new PositionInField(int.Parse(input[1][0].ToString()) - 1, char.Parse(input[1][1].ToString()));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.ReadLine();
                            continue;
                        }

                        remoteFunction.SendMessage(new MakeActionRequestArgs(from, to), RemoteFunctionTypes.MakeActionRequest);

                        Message message;
                        RemoteFunctionTypes messageType;

                        bool isOtherPlayerLeft = false;

                        while (true)
                        {
                            message = remoteFunction.ReceiveMessage();
                            messageType = message.Type;
                            if (messageType == RemoteFunctionTypes.OtherPlayerHaveLeft)
                            {
                                Console.Clear();

                                Console.WriteLine($"{blackPlayer.Username}, {blackPlayer.Wins}, {blackPlayer.Loses}");
                                RenderField(gameField);
                                Console.WriteLine($"{whitePlayer.Username}, {whitePlayer.Wins}, {whitePlayer.Loses}");
                                Console.WriteLine($"Для того что-бы походить напишите куда и откуда нужно это сделать. (Например: \"2A 4A\")");
                                Console.WriteLine("Противник покинул игру!");
                                Console.WriteLine("Ви победили!");
                                Console.ReadLine();
                                isOtherPlayerLeft = true;
                                break;
                            }
                            if (messageType == RemoteFunctionTypes.MakeActionResponse) break;
                        }

                        if (isOtherPlayerLeft)
                        {
                            Console.ReadLine();
                            break;
                        }

                        MakeActionResponseArgs resp = RemoteFunction.ConvertPayloadIntoArgs<MakeActionResponseArgs>(message.Payload);

                        if (resp.ResponseType != ResponseTypes.Success)
                        {
                            Console.WriteLine(ErrorTypesConverter.GetStringByErrorType(resp.ResponseType));
                            Console.ReadLine();
                            continue;
                        }

                        if (resp.Result != ActionResult.Success)
                        {
                            Console.WriteLine(resp.Result);
                            Console.ReadLine();
                            continue;
                        }

                        gameField.MoveFigure(from, to);
                        nextAction = resp.NewField.NextAction;

                        if (resp.IsGameEnded)
                        {
                            Console.Clear();
                                
                            Console.WriteLine($"{blackPlayer.Username}, {blackPlayer.Wins}, {blackPlayer.Loses}");
                            RenderField(gameField);
                            Console.WriteLine($"{whitePlayer.Username}, {whitePlayer.Wins}, {whitePlayer.Loses}");
                            Console.WriteLine($"Для того что-бы походить напишите куда и откуда нужно это сделать. (Например: \"2A 4A\")");
                            Console.WriteLine("Ви победили!");
                            Console.ReadLine();
                            break;
                        }
                    }
                    else
                    {
                        Message message;
                        RemoteFunctionTypes messageType;
                        bool isOtherPlayerLeft = false;

                        while (true)
                        {
                            message = remoteFunction.ReceiveMessage();
                            messageType = message.Type;
                            if (messageType == RemoteFunctionTypes.OtherPlayerHaveLeft)
                            {
                                Console.Clear();

                                Console.WriteLine($"{blackPlayer.Username}, {blackPlayer.Wins}, {blackPlayer.Loses}");
                                RenderField(gameField);
                                Console.WriteLine($"{whitePlayer.Username}, {whitePlayer.Wins}, {whitePlayer.Loses}");
                                Console.WriteLine($"Для того что-бы походить напишите куда и откуда нужно это сделать. (Например: \"2A 4A\")");
                                Console.WriteLine("Противник покинул игру!");
                                Console.WriteLine("Ви победили!");
                                Console.ReadLine();
                                isOtherPlayerLeft = true;
                                break;
                            }
                            if (messageType == RemoteFunctionTypes.UpdateFieldRequest) break;
                        }

                        if (isOtherPlayerLeft)
                        {
                            Console.ReadLine();
                            break;
                        }

                        UpdateFieldRequestArgs resp = RemoteFunction.ConvertPayloadIntoArgs<UpdateFieldRequestArgs>(message.Payload);

                        gameField = resp.GameField;
                        nextAction = resp.NextAction;

                        if (resp.IsGameEnded)
                        {
                            Console.Clear();

                            Console.WriteLine($"{blackPlayer.Username}, {blackPlayer.Wins}, {blackPlayer.Loses}");
                            RenderField(gameField);
                            Console.WriteLine($"{whitePlayer.Username}, {whitePlayer.Wins}, {whitePlayer.Loses}");
                            Console.WriteLine($"Для того что-бы походить напишите куда и откуда нужно это сделать. (Например: \"2A 4A\")");
                            Console.WriteLine("Ви проиграли!");
                            Console.ReadLine();
                            break;
                        }
                    }
                }
            }
        }
        static bool Connect()
        {
            try
            {
                tcp = new TcpClient();
                IAsyncResult ar = tcp.BeginConnect("127.0.0.1", 5000, null, null);
                bool ok = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                if (!ok) return false;
                tcp.EndConnect(ar);
                stream = tcp.GetStream();
                return true;
            }
            catch { return false; }
        }
        static void RenderField(Field field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));

            var prevBg = Console.BackgroundColor;
            var prevFg = Console.ForegroundColor;

            Console.Write("  ");
            for (char file = 'a'; file <= 'h'; file++)
            {
                Console.Write($"   {file} ");
            }
            Console.WriteLine();

            for (int row = 0; row < 8; row++)
            {
                int rank = row + 1;
                Console.Write($" {rank} ");

                for (int col = 0; col < 8; col++)
                {
                    var pos = new PositionInField(row, col);
                    Square sq = field.getSquare(pos);

                    bool isLightSquare = ((row + col) % 2 == 0);

                    ConsoleColor bg = isLightSquare ? ConsoleColor.DarkYellow : ConsoleColor.DarkGreen;
                    Console.BackgroundColor = bg;

                    string pieceSymbol = FigureTypeConventor.getStringByFigureType(sq.Figure);

                    if (sq.Figure == FigureType.Empty)
                    {
                        Console.ForegroundColor = isLightSquare ? ConsoleColor.Black : ConsoleColor.Gray;
                        Console.Write($"  {pieceSymbol}  ");
                    }
                    else
                    {
                        Console.ForegroundColor = (sq.Color == TeamColor.White) ? ConsoleColor.White : ConsoleColor.Black;

                        Console.Write($"  {pieceSymbol}  ");    
                    }

                }

                Console.BackgroundColor = prevBg;
                Console.ForegroundColor = prevFg;
                Console.Write($" {rank}");

                Console.WriteLine();
            }

            Console.Write("  ");
            for (char file = 'a'; file <= 'h'; file++)
            {
                Console.Write($"   {file} ");
            }
            Console.WriteLine();

            Console.ForegroundColor = prevFg;
            Console.BackgroundColor = prevBg;
        }
    }
}
