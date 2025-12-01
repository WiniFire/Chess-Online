using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using ChessCommon.GeneralUtils;
using ChessCommon.SocketUtils;
using System.Xml.Linq;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Server
{
    public static class DataBaseRequests
    {
        private static string connectionString = "Server=127.0.0.1;Database=ChessOnline;Uid=root;Pwd=;";
        public static LoginResponseArgs TryToLogin(LoginRequestArgs request)
        {
            using MySqlConnection connection = new MySqlConnection(connectionString);
            using MySqlCommand command = new MySqlCommand("SELECT Id, Password, Wins, Loses FROM Users WHERE Username=@u", connection);
            command.Parameters.AddWithValue("@u", request.Username);
            connection.Open();
            using MySqlDataReader reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return new LoginResponseArgs(null, ResponseTypes.CantConnectWithDB);
            }
            string Password = reader.GetString("Password");
            if (Password != request.Password)
            {
                return new LoginResponseArgs(null, ResponseTypes.WrongPasswordOrLogin);
            }
            Account account = new Account(reader.GetInt32("Id"), request.Username, reader.GetInt32("Wins"), reader.GetInt32("Loses"));
            LoginResponseArgs response = new LoginResponseArgs(account, ResponseTypes.Success);
            return response;
        }
        public static LoginResponseArgs TryToRegister(RegisterRequestArgs request)
        {
            if (request.Username.Length > 20 || request.Username.Length < 3) 
                return new LoginResponseArgs(null, ResponseTypes.WrongUsernameLength);
            if (request.Password.Length > 200 || request.Password.Length < 8) 
                return new LoginResponseArgs(null, ResponseTypes.WrongPasswordLength);

            using MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            using (MySqlCommand commandCheck = new MySqlCommand("SELECT Id FROM Users WHERE Username=@u", connection))
            {
                commandCheck.Parameters.AddWithValue("@u", request.Username);

                using (MySqlDataReader readerCheck = commandCheck.ExecuteReader())
                {
                    if (readerCheck.Read())
                        return new LoginResponseArgs(null, ResponseTypes.UsernameAlreadyExist);
                }
            }

            using (MySqlCommand commandRegister = new MySqlCommand("INSERT INTO Users (Username, Password) VALUES (@username, @password)", connection))
            {
                commandRegister.Parameters.AddWithValue("@username", request.Username);
                commandRegister.Parameters.AddWithValue("@password", request.Password);
                commandRegister.ExecuteNonQuery();
            }

            using MySqlCommand cmdId = new MySqlCommand("SELECT Id FROM Users WHERE Username=@u", connection);
            cmdId.Parameters.AddWithValue("@u", request.Username);

            using MySqlDataReader idData = cmdId.ExecuteReader();
            idData.Read();

            Account acc = new Account(idData.GetInt32("Id"), request.Username, 0, 0);
            return new LoginResponseArgs(acc, ResponseTypes.Success);
        }
        public static bool AddWin(Account account)
        {
            using MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            using (MySqlCommand commandCheck = new MySqlCommand("select Id from Users where Username=@u", connection))
            {
                commandCheck.Parameters.AddWithValue("@u", account.Username);
                using (MySqlDataReader readerCheck = commandCheck.ExecuteReader())
                {
                    if (!readerCheck.Read())
                    {
                        return false;
                    }
                }
            }

            using (MySqlCommand commandRegister = new MySqlCommand($"UPDATE Users SET Wins=Wins+1 WHERE Id=@id", connection))
            {
                commandRegister.Parameters.AddWithValue("@id", account.Id);
                commandRegister.ExecuteReader();
                return true;
            }
        }
        public static bool AddLose(Account account)
        {
            using MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            using (MySqlCommand commandCheck = new MySqlCommand("select Id from Users where Username=@u", connection))
            {
                commandCheck.Parameters.AddWithValue("@u", account.Username);
                using (MySqlDataReader readerCheck = commandCheck.ExecuteReader())
                {
                    if (!readerCheck.Read())
                    {
                        return false;
                    }
                }
            }

            using (MySqlCommand commandRegister = new MySqlCommand($"UPDATE Users SET Loses=Loses+1 WHERE Id=@id", connection))
            {
                commandRegister.Parameters.AddWithValue("@id", account.Id);
                commandRegister.ExecuteReader();
                return true;
            }
        }
    }
}
