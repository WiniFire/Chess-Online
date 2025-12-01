using ChessCommon.GeneralUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessCommon
{
    namespace SocketUtils
    {
        public class Message
        {
            public RemoteFunctionTypes Type { get; set; }
            public object Payload { get; set; }
        }
        public enum RemoteFunctionTypes
        {
            LoginRequest,
            LoginResponse,
            RegisterRequest,
            LobbyCreateRequest,
            LobbyCreateResponse,
            LobbyJoinResponse,
            LobbyJoinRequest,
            GetLobbiesRequest,
            GetLobbiesResponse,
            UpdateFieldRequest,
            MakeActionRequest,
            MakeActionResponse,
            OtherPlayerHaveLeft
        }
        public class RemoteFunction
        {
            public TcpClient Client { get; set; }
            public RemoteFunction(TcpClient client)
            {
                Client = client;
            }
            public Message ReceiveMessage()
            {
                NetworkStream stream = Client.GetStream();

                byte[] lenBuf = new byte[4];
                int read = stream.Read(lenBuf, 0, 4);
                if (read == 0)
                    throw new IOException("Remote closed connection");

                int len = BitConverter.ToInt32(lenBuf, 0);

                const int MAX_MESSAGE_SIZE = 5 * 1024 * 1024;

                if (len <= 0 || len > MAX_MESSAGE_SIZE)
                    throw new IOException($"Invalid message length: {len}");

                byte[] buf = new byte[len];
                int pos = 0;

                while (pos < len)
                {
                    int r = stream.Read(buf, pos, len - pos);
                    if (r == 0)
                        throw new IOException("Remote closed connection");
                    pos += r;
                }

                string json = Encoding.UTF8.GetString(buf);

                Message message;
                try
                {
                    message = JsonSerializer.Deserialize<Message>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    throw new IOException($"Invalid JSON: {ex.Message}");
                }

                return message;
            }
            public static ArgsType ConvertPayloadIntoArgs<ArgsType>(object payload)
            {
                ArgsType args;

                if (payload is JsonElement el)
                {
                    args = el.Deserialize<ArgsType>(new JsonSerializerOptions   
                    {
                        PropertyNameCaseInsensitive = true,
                        IncludeFields = true
                    });
                }
                else
                {
                    string jsonPayload = JsonSerializer.Serialize(payload);
                    args = JsonSerializer.Deserialize<ArgsType>(jsonPayload);
                }
                return args;
            }
            public void SendMessage(object args, RemoteFunctionTypes functionType)
            {
                string json = JsonSerializer.Serialize(new Message { Type = functionType, Payload = args });
                byte[] data = Encoding.UTF8.GetBytes(json);
                byte[] dataLen = BitConverter.GetBytes(data.Length);
                Client.GetStream().Write(dataLen, 0, 4);
                Client.GetStream().Write(data, 0, data.Length);
            }
        }
        public class LoginRequestArgs
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public LoginRequestArgs(string username, string password)
            {
                Username = username;
                Password = password;
            }
        }
        public class LoginResponseArgs
        {
            public Account Account { get; set; }
            public ResponseTypes Error { get; set; }
            public LoginResponseArgs(Account account, ResponseTypes error)
            {
                Account = account;
                Error = error;
            }
        }
        public class RegisterRequestArgs
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public RegisterRequestArgs(string username, string password)
            {
                Username = username;
                Password = password;
            }
        }
        public class LobbyCreateRequestArgs
        {
            public string Password { get; set; }
            public LobbyCreateRequestArgs(string password)
            {
                Password = password;
            }

        }
        public class LobbyCreateResponseArgs
        {
            public ResponseTypes Error { get; set; }
            public LobbyCreateResponseArgs(ResponseTypes error)
            {
                Error = error;
            }
        }
        public class LobbyJoinRequestArgs
        {
            public int LobbyId { get; set; }
            public string Password { get; set; }
            public LobbyJoinRequestArgs(int lobbyId, string password)
            {
                LobbyId = lobbyId;
                Password = password;
            }
        }
        public class LobbyJoinResponseArgs
        {
            public ResponseTypes Error { get; set; }
            public UpdateFieldRequestArgs UpdateField { get; set; }
            public LobbyJoinResponseArgs(ResponseTypes error, UpdateFieldRequestArgs updateField)
            {
                Error = error;
                UpdateField = updateField;
            }
        }
        public class GetLobbiesResponseArgs
        {
            public List<PublicLobby> Lobbies { get; set; }
            public GetLobbiesResponseArgs(List<PublicLobby> lobbies)
            {
                Lobbies = lobbies;
            }
        }
        public class GetLobbiesRequestArgs
        {
            public GetLobbiesRequestArgs()
            {
            }
        }
        public class UpdateFieldRequestArgs
        {
            public Field GameField { get; set; }
            public Account Account1 { get; set; }
            public Account Account2 { get; set; }
            public bool IsGameEnded { get; set; }
            public Account Winner { get; set; } = new Account(-1, "", -1, -1);
            public NextActionType NextAction { get; set; }
            public UpdateFieldRequestArgs(Field gameField, Account account1, Account account2, NextActionType nextAction, bool isGameEnded, Account winner)
            {
                GameField = gameField;
                Account1 = account1;
                Account2 = account2;
                IsGameEnded = isGameEnded;
                NextAction = nextAction;
                Winner = winner;
            }
        }
        public class MakeActionRequestArgs
        {
            public PositionInField From { get; set; }
            public PositionInField To { get; set; }
            public MakeActionRequestArgs(PositionInField from, PositionInField to)
            { 
                From = from; 
                To = to; 
            }
        }
        public class MakeActionResponseArgs
        {
            public UpdateFieldRequestArgs NewField { get; set; }
            public ActionResult Result { get; set; }
            public ResponseTypes ResponseType { get; set; }
            public bool IsGameEnded { get; set; }
            public MakeActionResponseArgs(UpdateFieldRequestArgs newField, ActionResult result, ResponseTypes responseType)
            {
                NewField = newField;
                Result = result;
                ResponseType = responseType;
            }
        }
        public class OtherPlayerHaveLeftRequestArgs
        {

        }
    }
}