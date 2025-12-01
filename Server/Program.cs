using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using ChessCommon.SocketUtils;
using ChessCommon.GeneralUtils;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Server
{
    internal class Program
    {
        internal static List<ConnectedUser> ConnectedUsers = new List<ConnectedUser>();
        internal static List<Lobby> Lobbies = new List<Lobby>();
        static void Main(string[] args)
        {
            //Account whitePlr = new Account(1, "wpir", 0, 0);
            //Account blackPlr = new Account(2, "uzhs", 0, 0);
            //Chess chess = new Chess(whitePlr, blackPlr);
            //while (chess.NextAction != NextActionType.GameEnded)
            //{
            //    Console.Clear();
            //    RenderField(chess.GameField);

            //    Account actioner = (chess.NextAction == NextActionType.WhiteTurn) ? whitePlr : blackPlr;

            //    string[] input = Console.ReadLine().Split();

            //    PositionInField from;
            //    PositionInField to;

            //    try
            //    {
            //        from = new PositionInField(int.Parse(input[0][0].ToString()) - 1, char.Parse(input[0][1].ToString()));
            //        to = new PositionInField(int.Parse(input[1][0].ToString()) - 1, char.Parse(input[1][1].ToString()));
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //        Console.ReadLine();
            //        continue;
            //    }

            //    ActionResult res = chess.MakeAction(actioner, from, to);

            //    if (res != ActionResult.Success)
            //    {
            //        Console.WriteLine(res);
            //        Console.ReadLine();
            //        continue;
            //    }
            //}

            //Console.WriteLine(chess.Winner.Username);

            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            Thread commands = new Thread(() =>
            {
                while (true)
                {
                    string comm = Console.ReadLine();
                    if (comm == "listLobbies")
                    {
                        foreach (Lobby lob in Lobbies)
                        {
                            Console.WriteLine($"{lob.Player1.UserAccount.Username}, {lob.Id}, {lob.Password}");
                        }
                        Console.WriteLine("======");
                    }
                }
            });
            commands.Start();
            listener.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread handler = new Thread(() => ClientHandler(client));
                handler.Start();
            }
        }
        static void ClientHandler(TcpClient client)
        {
            RemoteFunction remoteFunction = new RemoteFunction(client);

            while (true)
            {
                try
                {
                    Message message = remoteFunction.ReceiveMessage();

                    switch (message.Type)
                    {
                        case RemoteFunctionTypes.LoginRequest:
                            LoginRequestArgs loginReq = RemoteFunction.ConvertPayloadIntoArgs<LoginRequestArgs>(message.Payload);

                            OnLoginRequestHandler(loginReq, remoteFunction);

                            break;

                        case RemoteFunctionTypes.RegisterRequest:
                            RegisterRequestArgs registerReq = RemoteFunction.ConvertPayloadIntoArgs<RegisterRequestArgs>(message.Payload);

                            OnRegisterRequestHandler(registerReq, remoteFunction);

                            break;

                        case RemoteFunctionTypes.GetLobbiesRequest:
                            GetLobbiesRequestArgs getLobbiesReq = RemoteFunction.ConvertPayloadIntoArgs<GetLobbiesRequestArgs>(message.Payload);

                            OnGetLobbiesRequestHandler(getLobbiesReq, remoteFunction);

                            break;

                        case RemoteFunctionTypes.LobbyCreateRequest:
                            LobbyCreateRequestArgs lobbyCreateReq = RemoteFunction.ConvertPayloadIntoArgs<LobbyCreateRequestArgs>(message.Payload);

                            OnLobbyCreateRequestHandler(lobbyCreateReq, remoteFunction);

                            break;

                        case RemoteFunctionTypes.LobbyJoinRequest:
                            LobbyJoinRequestArgs lobbyJoinReq = RemoteFunction.ConvertPayloadIntoArgs<LobbyJoinRequestArgs>(message.Payload);

                            OnLobbyJoinRequestHandler(lobbyJoinReq, remoteFunction);

                            break;

                        case RemoteFunctionTypes.MakeActionRequest:
                            MakeActionRequestArgs makeActionReq = RemoteFunction.ConvertPayloadIntoArgs<MakeActionRequestArgs>(message.Payload);

                            OnMakeActionRequest(makeActionReq, remoteFunction);

                            break;
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"{client.Client.RemoteEndPoint.ToString()}: {ex.Message}");
                    if (ex.Message == "Не удается прочитать данные из транспортного соединения: Удаленный хост принудительно разорвал существующее подключение.")
                    {
                        ConnectedUser user = null;
                        int userIndex = -1;
                        for (int i = 0; i < ConnectedUsers.Count; ++i)
                        {
                            if (ConnectedUsers[i].Client == client)
                            {
                                userIndex = i;
                                user = ConnectedUsers[i];
                            }
                        }

                        for (int i = 0; i < Lobbies.Count; ++i)
                        {
                            Lobby lobby = Lobbies[i];
                            if (lobby.IsAccountInLobby(user.UserAccount))
                            {
                                if (!lobby.IsGamesTarted)
                                {
                                    lobby.Player1 = null;
                                    lobby.Player2 = null;
                                    Lobbies.RemoveAt(i);
                                    break;
                                }
                                ConnectedUser winner = (user.UserAccount.Id == lobby.Player1.UserAccount.Id) ? lobby.Player2 : lobby.Player1;
                                DataBaseRequests.AddWin(winner.UserAccount);
                                DataBaseRequests.AddLose(user.UserAccount);
                                RemoteFunction remoteFunctionAnotherPlr = new RemoteFunction(winner.Client);
                                remoteFunctionAnotherPlr.SendMessage(new OtherPlayerHaveLeftRequestArgs(), RemoteFunctionTypes.OtherPlayerHaveLeft);
                                lobby.Player1 = null;
                                lobby.Player2 = null;
                                Lobbies.RemoveAt(i);
                                break;
                            }
                        }

                        if (userIndex != -1) ConnectedUsers.RemoveAt(userIndex);

                        break;
                    }
                }
            }
        }
        static void OnLoginRequestHandler(LoginRequestArgs args, RemoteFunction remoteFunction)
        {
            TcpClient client = remoteFunction.Client;
            LoginResponseArgs loginResult = DataBaseRequests.TryToLogin(args);
            if (loginResult.Error == ResponseTypes.Success)
            {
                foreach (ConnectedUser user in ConnectedUsers)
                {
                    if (user.UserAccount.Id == loginResult.Account.Id)
                    {
                        loginResult.Error = ResponseTypes.UserIsAlreadyAuthorized;
                    }
                }
                if (loginResult.Error == ResponseTypes.Success)
                {
                    ConnectedUsers.Add(new ConnectedUser(loginResult.Account, client));
                }
            }
            remoteFunction.SendMessage(loginResult, RemoteFunctionTypes.LoginResponse);
        }
        static void OnRegisterRequestHandler(RegisterRequestArgs args, RemoteFunction remoteFunction)
        {
            TcpClient client = remoteFunction.Client;
            LoginResponseArgs registerResult = DataBaseRequests.TryToRegister(args);
            if (registerResult.Error == ResponseTypes.Success)
            {
                ConnectedUsers.Add(new ConnectedUser(registerResult.Account, client));
            }
            remoteFunction.SendMessage(registerResult, RemoteFunctionTypes.LoginResponse);
        }
        static void OnGetLobbiesRequestHandler(GetLobbiesRequestArgs args, RemoteFunction remoteFunction)
        {
            TcpClient client = remoteFunction.Client;
            foreach (ConnectedUser user in ConnectedUsers)
            {
                if (user.Client == client)
                {
                    List<PublicLobby> lobbiesList = new List<PublicLobby>();
                    foreach (Lobby lobby in Lobbies)
                    {
                        lobbiesList.Add(new PublicLobby(lobby.Id, lobby.Player1.UserAccount, lobby.Player2.UserAccount, (lobby.Password == string.Empty)? false: true));
                    }
                    GetLobbiesResponseArgs response = new GetLobbiesResponseArgs(lobbiesList);
                    remoteFunction.SendMessage(response, RemoteFunctionTypes.GetLobbiesResponse);
                }
            }
        }
        static void OnLobbyCreateRequestHandler(LobbyCreateRequestArgs args, RemoteFunction remoteFunction)
        {
            TcpClient client = remoteFunction.Client;
            ConnectedUser userReq = null;
            foreach (ConnectedUser user in ConnectedUsers)
            {
                if (user.Client == client)
                {
                    userReq = user;
                    break;
                }
            }
            if (userReq is null)
            {
                remoteFunction.SendMessage(new LobbyCreateResponseArgs(ResponseTypes.YouAreNotAuthorized), RemoteFunctionTypes.LobbyCreateResponse);
                return;
            }
            bool isInLobby = false;
            foreach (Lobby lobby in Lobbies)
            {
                if (lobby.IsClientInLobby(client))
                {
                    isInLobby = true;
                    break;
                }
            }
            if (isInLobby)
            {
                remoteFunction.SendMessage(new LobbyCreateResponseArgs(ResponseTypes.YouAreAlreadyInLobby), RemoteFunctionTypes.LobbyCreateResponse);
                return;
            }
            Lobby newLobby = new Lobby(args.Password);
            newLobby.Player1 = userReq;
            Lobbies.Add(newLobby);
            remoteFunction.SendMessage(new LobbyCreateResponseArgs(ResponseTypes.Success), RemoteFunctionTypes.LobbyCreateResponse);
        }
        static void OnLobbyJoinRequestHandler(LobbyJoinRequestArgs args, RemoteFunction remoteFunction)
        {
            TcpClient client = remoteFunction.Client;
            ConnectedUser userReq = null;
            foreach (ConnectedUser user in ConnectedUsers)
            {
                if (user.Client == client)
                {
                    userReq = user;
                    break;
                }
            }
            if (userReq is null)
            {
                remoteFunction.SendMessage(new LobbyJoinResponseArgs(ResponseTypes.YouAreNotAuthorized, null), RemoteFunctionTypes.LobbyJoinResponse);
                return;
            }
            bool isInLobby = false;
            foreach (Lobby lobby in Lobbies)
            {
                if (lobby.IsClientInLobby(client))
                {
                    isInLobby = true;
                    break;
                }
            }
            if (isInLobby)
            {
                remoteFunction.SendMessage(new LobbyJoinResponseArgs(ResponseTypes.YouAreAlreadyInLobby, null), RemoteFunctionTypes.LobbyJoinResponse);
                return;
            }
            Lobby lobbyToJoin = null;
            foreach (Lobby lobby in Lobbies)
            {
                if (lobby.Id == args.LobbyId)
                {
                    lobbyToJoin = lobby;
                }
            }
            if (lobbyToJoin is null)
            {
                remoteFunction.SendMessage(new LobbyJoinResponseArgs(ResponseTypes.UnknownLobby, null), RemoteFunctionTypes.LobbyJoinResponse);
                return;
            }
            if (lobbyToJoin.Password == string.Empty)
            {
                lobbyToJoin.Player2 = userReq;
                lobbyToJoin.CreateGame();
                return;
            }
            else
            {
                if (lobbyToJoin.Password == args.Password)
                {
                    lobbyToJoin.Player2 = userReq;
                    lobbyToJoin.CreateGame();
                    return;
                }
                else
                {
                    remoteFunction.SendMessage(new LobbyJoinResponseArgs(ResponseTypes.WrongLobbyPassword, null), RemoteFunctionTypes.LobbyJoinResponse);
                    return;
                }
            }
        }
        static void OnMakeActionRequest(MakeActionRequestArgs args, RemoteFunction remoteFunction)
        {
            TcpClient client = remoteFunction.Client;
            ConnectedUser userReq = null;
            foreach (ConnectedUser user in ConnectedUsers)
            {
                if (user.Client == client)
                {
                    userReq = user;
                    break;
                }
            }
            if (userReq is null)
            {
                remoteFunction.SendMessage(new MakeActionResponseArgs(null, ActionResult.None ,ResponseTypes.YouAreNotAuthorized), RemoteFunctionTypes.MakeActionResponse);
                return;
            }
            Lobby userLobby = null; 
            foreach (Lobby lobby in Lobbies)
            {
                if (lobby.IsClientInLobby(client))
                {
                    userLobby = lobby;
                    break;
                }
            }
            if (userLobby is null)
            {
                remoteFunction.SendMessage(new MakeActionResponseArgs(null, ActionResult.None, ResponseTypes.YouAreNotInLobby), RemoteFunctionTypes.MakeActionResponse);
                return;
            }
            if (!userLobby.IsGamesTarted)
            {
                remoteFunction.SendMessage(new MakeActionResponseArgs(null, ActionResult.None, ResponseTypes.GameIsNotStartedYet), RemoteFunctionTypes.MakeActionResponse);
                return;
            }



            ActionResult res =  userLobby.game.MakeAction(userReq.UserAccount, args.From, args.To); 
            if (res != ActionResult.Success)
            {
                remoteFunction.SendMessage(new MakeActionResponseArgs(null, res, ResponseTypes.Success), RemoteFunctionTypes.MakeActionResponse);
                return;
            }

            bool isGameEnded = (userLobby.game.Winner is null) ? false : true;

            if (isGameEnded)
            {
                DataBaseRequests.AddWin(userReq.UserAccount);
                DataBaseRequests.AddLose((userReq.UserAccount.Id == userLobby.Player1.UserAccount.Id) ? userLobby.Player2.UserAccount : userLobby.Player1.UserAccount);
                MakeActionResponseArgs resp = new MakeActionResponseArgs(new UpdateFieldRequestArgs(new Field(userLobby.game.GameField), userLobby.Player1.UserAccount, userLobby.Player2.UserAccount, userLobby.game.NextAction, true, userReq.UserAccount), res, ResponseTypes.Success);
                resp.IsGameEnded = isGameEnded;
                remoteFunction.SendMessage(resp, RemoteFunctionTypes.MakeActionResponse);
                RemoteFunction remoteFunctionAnotherPlr = new RemoteFunction((client == userLobby.Player1.Client) ? userLobby.Player2.Client : userLobby.Player1.Client);
                remoteFunctionAnotherPlr.SendMessage(new UpdateFieldRequestArgs(new Field(userLobby.game.GameField), userLobby.Player1.UserAccount, userLobby.Player2.UserAccount, userLobby.game.NextAction, true, userReq.UserAccount), RemoteFunctionTypes.UpdateFieldRequest);
                userLobby.Player1 = null;
                userLobby.Player2 = null;
                
                for (int i = 0; i < Lobbies.Count; ++i)
                {
                    if (Lobbies[i].Id == userLobby.Id)
                    {
                        Lobbies.RemoveAt(i);
                        break;
                    }
                }
            }
            else
            {
                MakeActionResponseArgs resp = new MakeActionResponseArgs(new UpdateFieldRequestArgs(new Field(userLobby.game.GameField), userLobby.Player1.UserAccount, userLobby.Player2.UserAccount, userLobby.game.NextAction, false, new Account(-1, "", -1, -1)), res, ResponseTypes.Success);
                resp.IsGameEnded = isGameEnded;
                remoteFunction.SendMessage(resp, RemoteFunctionTypes.MakeActionResponse);
                RemoteFunction remoteFunctionAnotherPlr = new RemoteFunction((client == userLobby.Player1.Client) ? userLobby.Player2.Client : userLobby.Player1.Client);
                remoteFunctionAnotherPlr.SendMessage(new UpdateFieldRequestArgs(new Field(userLobby.game.GameField), userLobby.Player1.UserAccount, userLobby.Player2.UserAccount, userLobby.game.NextAction, false, new Account(-1, "", -1, -1)), RemoteFunctionTypes.UpdateFieldRequest);
            }
        }
        internal class ConnectedUser
        {
            internal Account UserAccount { get; set; }
            internal TcpClient Client { get; set; }
            internal ConnectedUser(Account account, TcpClient client)
            {
                UserAccount = account;
                Client = client;
            }
        }
        internal class Lobby
        {
            private static int NextId = 1;
            internal int Id { get; private set; }
            internal string Password { get; set; }
            internal ConnectedUser Player1 { get; set; } = new ConnectedUser(new Account(-1, "", 0, 0), null);
            internal ConnectedUser Player2 { get; set; } = new ConnectedUser(new Account(-1, "", 0, 0), null);
            internal bool IsGamesTarted { get; set; } = false;
            internal Chess game;
            internal bool IsUserInLobby(ConnectedUser user) => (Player1 == user || Player2 == user);
            internal bool IsAccountInLobby(Account account) => (Player1.UserAccount == account || Player2.UserAccount == account);
            internal bool IsClientInLobby(TcpClient client) => (Player1.Client == client || Player2.Client == client);
            internal void CreateGame()
            {
                game = new Chess(Player1.UserAccount, Player2.UserAccount);
                IsGamesTarted = true;
                RemoteFunction remoteFunctionPlayer1 = new RemoteFunction(Player1.Client);
                RemoteFunction remoteFunctionPlayer2 = new RemoteFunction(Player2.Client);
                remoteFunctionPlayer1.SendMessage(new UpdateFieldRequestArgs(game.GameField, Player1.UserAccount, Player2.UserAccount, game.NextAction, false, new Account(-1, "", -1, -1)), RemoteFunctionTypes.UpdateFieldRequest);
                remoteFunctionPlayer2.SendMessage(new LobbyJoinResponseArgs(ResponseTypes.Success, new UpdateFieldRequestArgs(game.GameField, Player1.UserAccount, Player2.UserAccount, game.NextAction, false, new Account(-1, "", -1, -1))), RemoteFunctionTypes.LobbyJoinResponse);
            }
            internal Lobby(string password)
            {
                Password = (password == "" || password == string.Empty) ? string.Empty : password;
                Id = NextId;
                ++NextId;
                game = null;
            }
        }
    }
}