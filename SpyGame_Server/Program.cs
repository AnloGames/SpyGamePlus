using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SpyGamePlus_Server.Classes;
using System.IO;
using System.Data.SQLite;
using Dapper;

namespace SpyGamePlus_Server
{
    static class Program
    {
        public static List<Game> Games = new List<Game>();
        public static SQLiteConnection connection;
        static void Main(string[] args)
        {
            string connection_string = "Data Source=GameUsers.db";
            connection = new SQLiteConnection(connection_string);
            connection.Open();
            connection.Execute("UPDATE Users SET isActive = 'false'");

            int port = 8080;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse("26.190.29.233");
            IPEndPoint Ep = new IPEndPoint(ipAddress, port);

            socket.Bind(Ep);
            socket.Listen(100000);
            Console.WriteLine("Server started");
            while (true)
            {
                //Лучше использовать асинхронность и из неё запускать первые методы
                //т.к. Здесь испоьзуется получение данных из сети, хоть и с коротким интервалом
                //Тогда нам не придется использовать Thread.Sleep(2000)
                Thread thread = new Thread(WorkWithClient);
                thread.Start(socket.Accept());
                Thread.Sleep(2000);
            }
        }
        static void SendMessageToAll(string mes, List<User> users, User ExtraUser)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i] != ExtraUser)
                {
                    NetLib.SendDataToNet(users[i].ClientSocket, mes);
                }
            }
        }

        static void WorkWithClient(object ClSocket)
        {
            Socket ClientSocket = ClSocket as Socket;
            User user = new User(ClientSocket);
            ExecuteOperation(user);
        }
        static void ExecuteOperation(User user)
        {
            string OperationName = "NULL";
            string UserName = "NULL";
            string UserPass = "NULL";

            try
            {
                string[] Operation = NetLib.ReadDataFromNet(user.ClientSocket).Split('/');
                OperationName = Operation[0];
                UserName = Operation[1];
                UserPass = Operation[2];
            }
            catch (Exception)
            {
                user.ClientSocket.Shutdown(SocketShutdown.Both);
                user.ClientSocket.Close();
                Thread.Sleep(Timeout.Infinite);
                Thread.CurrentThread.Interrupt();
            }
            if (OperationName == "Reg")
            {
                int NamesCount = connection.Query<string>("SELECT name FROM Users WHERE name = @name", new {name = UserName}).ToList().Count;
                if (NamesCount == 0)
                {
                    string Query = ("INSERT INTO Users(name, password, isActive, rate) VALUES (@name, @password, 'true', 0)");
                    DynamicParameters Values = new DynamicParameters();
                    Values.Add("@name", UserName);
                    Values.Add("@password", UserPass);
                    connection.Execute(Query, Values);
                    user.Nickname = UserName;
                    Console.WriteLine("User " + user.Nickname + " connected");
                    NetLib.SendDataToNet(user.ClientSocket, "OK");
                    Start(user);
                }
                else
                {
                    NetLib.SendDataToNet(user.ClientSocket, "NameError");
                    ExecuteOperation(user);
                }
            }
            else if (OperationName == "Sig")
            {
                List<UserData> userData = connection.Query<UserData>("SELECT * FROM Users WHERE name = @name", new { name = UserName }).ToList();
                if (userData.Count != 0 && UserPass == userData[0].Password)
                {
                    if (userData[0].IsActive == "true")
                    {
                        NetLib.SendDataToNet(user.ClientSocket, "ActivityError");
                        ExecuteOperation(user);
                    }
                    else
                    {
                        user.Nickname = UserName;
                        Console.WriteLine("User " + user.Nickname + " connected");
                        NetLib.SendDataToNet(user.ClientSocket, "OK");
                        user.Rate = userData[0].Rate;
                        connection.Execute("UPDATE Users SET isActive = 'true' WHERE name = @name", new { name = UserName });
                        Start(user);
                    }
                }
                else
                {
                    NetLib.SendDataToNet(user.ClientSocket, "PassNameError");
                    ExecuteOperation(user);
                }
            }
        }
        public static void Start(User user)
        {
            NetLib.SendDataToNet(user.ClientSocket, user.Rate.ToString());
            Thread GameStartThread = new Thread(SearchGame);
            Socket SavedSocket = user.ClientSocket;
            string mes = "";
            while (true)
            {
                try
                {
                    mes = NetLib.ReadDataFromNet(user.ClientSocket);
                    Console.WriteLine(mes);
                }
                catch (Exception e)
                {
                    user.ClientSocket = null;
                    connection.Execute("UPDATE Users SET isActive = 'false' WHERE name = @name", new { name = user.Nickname });
                    if (user.UserState == "InGame" || user.UserState == "Turn") 
                    {
                        SendMessageToAll(user.UserGame.UsersInGame.IndexOf(user) + "/Disconnect/-1/NULL|", user.UserGame.UsersInGame, user);
                        RateUpdate(user, false);
                        if (user.Role == "Spy")
                        {
                            user.UserGame.SpiesCount--;
                            user.UserGame.PlayersNow--;
                            if (user.UserGame.SpiesCount<=0)
                            {
                                string message = -1 + "/ResWin/" + -1 + "/В живых не осталось ни одного шпиона! Горожане победили!|";
                                SendMessageToAll(message, user.UserGame.UsersInGame, user);
                            }
                            else if (user.UserGame.PlayersNow <= 3)
                            {
                                string message = -1 + "/SpyWin/" + -1 + "/Мирных жителей осталось слишком мало и они не смогли сопротивляться! Шпионы победили!|";
                                SendMessageToAll(message, user.UserGame.UsersInGame, user);
                            }
                        }
                        else
                        {
                            user.UserGame.PlayersNow--;
                            if (user.UserGame.PlayersNow <= 3 && user.UserGame.SpiesCount >= 1)
                            {
                                string message = -1 + "/SpyWin/" + -1 + "/Мирных жителей осталось слишком мало и они не смогли сопротивляться! Шпионы победили!|";
                                SendMessageToAll(message, user.UserGame.UsersInGame, user);
                            }
                        }
                        if (user.UserState == "Turn")
                        {
                            user.UserGame.Waiting.Set();
                        }
                    }
                    else if (user.UserState == "Waiting" && user.UserGame != null)
                    {
                        user.UserGame.UsersInGame.Remove(user);
                    }
                    SavedSocket.Shutdown(SocketShutdown.Both);
                    SavedSocket.Close();
                    Console.WriteLine("User " + user.Nickname + " disconnected");
                    Thread.Sleep(Timeout.Infinite);
                    Thread.CurrentThread.Interrupt();
                }
                if (mes == "Win")
                {
                    Console.WriteLine("Игрок " + user.Nickname + " выигрывает");
                    user.UserGame.UsersInGame[user.UserGame.UsersInGame.IndexOf(user)] = new User(null);
                    RateUpdate(user, true);
                    user = new User(user.ClientSocket, user.Nickname, user.Rate);
                    NetLib.SendDataToNet(user.ClientSocket, user.Rate.ToString());
                    Thread.Sleep(1000);
                }
                else if (mes == "Lose")
                {
                    Console.WriteLine("Игрок " + user.Nickname + " проигрывает");
                    user.UserGame.UsersInGame[user.UserGame.UsersInGame.IndexOf(user)] = new User(null);
                    RateUpdate(user, false);
                    user = new User(user.ClientSocket, user.Nickname, user.Rate);
                    NetLib.SendDataToNet(user.ClientSocket, user.Rate.ToString());
                    Thread.Sleep(1000);
                }
                else if (mes == "Cancel")
                {
                    user.UserGame.UsersInGame.Remove(user);
                    user.UserGame = null;
                    user.UserState = null;
                }
                else if (mes.Contains("PlayersCount"))
                {
                    user.PlayersCount = int.Parse(mes.Split('/')[1]);
                    user.RateMultiplier = user.PlayersCount * 0.25;
                    GameStartThread = new Thread(SearchGame);
                    GameStartThread.Start(user);

                }
                else if (mes.Contains("/SelectedKeyWord/"))
                {
                    user.SpyKeyWord = mes.Split('/')[3];
                    if (user.PlayersCount >= 8) 
                    {
                        user.BaseWinRate += 5;
                    }
                }
                else
                {
                    if (mes.Contains("/Answer/"))
                    {
                        user.UserGame.Waiting.Set();
                        user.UserState = "InGame";
                    }
                    else if (mes.Contains("/Question/"))
                    {
                        int receiverIndex = int.Parse(mes.Split('/')[2]);
                        user.UserGame.UsersInGame[receiverIndex].UserState = "Turn";
                        user.UserState = "InGame";
                    }
                    else if (mes.Contains("/SelectedSpy/"))
                    {
                        user.UserGame.UsersInGame[int.Parse(mes.Split('/')[2])].Votes++;
                        if (user.UserGame.UsersInGame[int.Parse(mes.Split('/')[2])].Role == "Spy" && user.Role != "Spy")
                        {
                            user.BaseWinRate += 2;
                            user.BaseLoseRate -= 1;
                        }
                    }
                    SendMessageToAll(mes, user.UserGame.UsersInGame, null);
                }
            }
        }
        public static void SearchGame(object ObjUser)
        {
            List<Game> GamesNow = Games;
            User user = ObjUser as User;
            user.UserState = "Waiting";
            for (int i = 0; i < GamesNow.Count; i++)
            {
                int RateLimit = 200;
                if (user.Rate > 1000) 
                {
                    RateLimit = user.Rate / 5;
                }
                if (GamesNow[i].PlayersCount == user.PlayersCount && !GamesNow[i].IsGameStarted && Math.Abs(GamesNow[i].GameRate - user.Rate) < RateLimit)
                {
                    Console.WriteLine("User " + user.Nickname + " join game");
                    GamesNow[i].UsersInGame.Add(user);
                    GamesNow[i].GameRateUpdate();
                    user.UserGame = GamesNow[i];
                    if (GamesNow[i].PlayersCount == GamesNow[i].UsersInGame.Count)
                    {
                        Console.WriteLine("Game started");
                        GamesNow[i].IsGameStarted = true;
                        SendMessageToAll("P", GamesNow[i].UsersInGame, null);
                        Games.Remove(GamesNow[i]);
                        Thread GameThread = new Thread(GameStart);
                        GameThread.Start(user.UserGame);
                    }
                    Thread.Sleep(Timeout.Infinite);
                    Thread.CurrentThread.Interrupt();
                }
            }
            Game NewGame = new Game(user.PlayersCount);
            Games.Add(NewGame);
            NewGame.UsersInGame.Add(user);
            user.UserGame = NewGame;
            NewGame.GameRateUpdate();
            Console.WriteLine("User " + user.Nickname + " create game");
            Thread.Sleep(Timeout.Infinite);
            Thread.CurrentThread.Interrupt();
        }
        public static void GameStart(Object GameRef)
        {
            string PlayerListMes = "";
            Game game = GameRef as Game;
            for (int i = 0; i < game.PlayersCount; i++)
            {
                game.UsersInGame[i].UserState = "InGame";
                PlayerListMes += game.UsersInGame[i].Nickname + "/";
            }
            Console.WriteLine(PlayerListMes);
            SendMessageToAll(PlayerListMes, game.UsersInGame, null);
            Thread.Sleep(1000);
            GainRole(ref game);
            GainKeyWord(ref game);
            int KeyWordOffset = 0;
            int KeyWordUnlocking = (int)(game.KeyWord.Length * 0.25);
            if (KeyWordUnlocking == 0)
            {
                KeyWordUnlocking = 1;
            }

            while (true)
            {
                for (int i = 0; i < game.PlayersCount; i++)
                {
                    Thread.Sleep(1000);
                    if (game.UsersInGame[i].ClientSocket != null)
                    {
                        game.UsersInGame[i].UserState = "Turn";
                        NetLib.SendDataToNet(game.UsersInGame[i].ClientSocket, "Turn|");
                        game.Waiting.Reset();

                    }
                    game.Waiting.WaitOne();
                }
                SendMessageToAll("Debate|", game.UsersInGame, null);
                Thread.Sleep(15000);
                int TargetUserNum = -1;
                int MaxVotes = 0;
                bool IsSingleMaxVotes = false;
                for (int i = 0; i < game.PlayersCount; i++)
                {
                    if (game.UsersInGame[i].Votes == MaxVotes)
                    {
                        IsSingleMaxVotes = false;
                    }
                    else if (game.UsersInGame[i].Votes > MaxVotes)
                    {
                        MaxVotes = game.UsersInGame[i].Votes;
                        TargetUserNum = i;
                        IsSingleMaxVotes = true;
                    }
                    game.UsersInGame[i].Votes = 0;
                }
                if (IsSingleMaxVotes)
                {
                    if (game.UsersInGame[TargetUserNum].Role == "Spy")
                    {
                        if (game.UsersInGame[TargetUserNum].ClientSocket != null)
                        {
                            string mes = -1 + "/DeadSpy/" + TargetUserNum + "/ был раскрыт и убит мирными жителями!|";
                            game.PlayersNow--;
                            game.SpiesCount--;
                            SendMessageToAll(mes, game.UsersInGame, null);
                            Thread.Sleep(2000);
                        }
                    }
                    else
                    {
                        if (game.UsersInGame[TargetUserNum].ClientSocket != null)
                        {
                            string mes = -1 + "/DeadRes/" + TargetUserNum + "/" + GetKeyWordPart(game.KeyWord, KeyWordOffset, KeyWordUnlocking) + "|";
                            KeyWordOffset += KeyWordUnlocking;
                            game.PlayersNow--;
                            SendMessageToAll(mes, game.UsersInGame, null);
                            Thread.Sleep(2000);
                        }
                    }
                }
                else
                {
                    string mes = -1 + "/NoDead/" + -1 + "/NULL|";
                    SendMessageToAll(mes, game.UsersInGame, null);
                    Thread.Sleep(2000);
                }
                if (game.SpiesCount <= 0)
                {
                    string mes = -1 + "/ResWin/" + -1 + "/В живых не осталось ни одного шпиона! Горожане победили!|";
                    SendMessageToAll(mes, game.UsersInGame, null);
                    Thread.Sleep(2000);
                    break;
                }
                bool IsSpyWin = false;
                for (int i = 0; i < game.SpiesInGame.Count; i++)
                {
                    if (game.SpiesInGame[i].SpyKeyWord != "")
                    {
                        if (game.SpiesInGame[i].SpyKeyWord == game.KeyWord)
                        {
                            
                            string mes = -1 + "/SpyWin/" + -1 + "/Шпионы отгадали кодовое слово и победили! Горожане проиграли!|";
                            SendMessageToAll(mes, game.UsersInGame, null);
                            IsSpyWin = true;
                            break;
                        }
                        else
                        {
                            if (game.SpiesInGame[i].ClientSocket != null)
                            {
                                string mes = -1 + "/DeadSpy/" + game.UsersInGame.IndexOf(game.SpiesInGame[i]) + "/ неправильно отгадал слово и раскрыл себя!|";
                                game.PlayersNow--;
                                game.SpiesCount--;
                                SendMessageToAll(mes, game.UsersInGame, null);
                                Thread.Sleep(2000);
                            }
                        }
                    }
                }
                if (IsSpyWin)
                {
                    Thread.Sleep(2000);
                    break;
                }
                else if (game.SpiesCount <= 0)
                {
                    string mes = -1 + "/ResWin/" + -1 + "/В живых не осталось ни одного шпиона! Горожане победили!|";
                    SendMessageToAll(mes, game.UsersInGame, null);
                    Thread.Sleep(2000);
                    break;
                }
                else if (game.PlayersNow <= 3)
                {
                    string mes = -1 + "/SpyWin/" + -1 + "/Мирных жителей осталось слишком мало и они не смогли сопротивляться! Шпионы победили!|";
                    SendMessageToAll(mes, game.UsersInGame, null);
                    Thread.Sleep(2000);
                    break;
                }
                SendMessageToAll("EndDebate|", game.UsersInGame, null);
            }
            game.UsersInGame.Clear();
            game.IsGameStarted = false;
            Games.Add(game);
            Thread.Sleep(Timeout.Infinite);
            Thread.CurrentThread.Interrupt();

        }
        public static void GainRole(ref Game game)
        {
            Random random = new Random();
            for (int i = 0; i < game.SpiesCount; i++)
            {
                Console.WriteLine("Send roles");
                int NewSpy = random.Next(0, game.PlayersCount);
                if (game.UsersInGame[NewSpy].Role == null)
                {
                    game.UsersInGame[NewSpy].Role = "Spy";
                    game.SpiesInGame.Add(game.UsersInGame[NewSpy]);
                    game.UsersInGame[NewSpy].RateMultiplier += 2;
                    NetLib.SendDataToNet(game.UsersInGame[NewSpy].ClientSocket, game.UsersInGame[NewSpy].Role);
                    Console.WriteLine(game.UsersInGame[NewSpy].Nickname + " - Spy");
                }
                else
                {
                    i--;
                }
            }
            for (int i = 0; i < game.OutlandersInGame; i++)
            {
                int NewOutlander= random.Next(0, game.PlayersCount);
                if (game.UsersInGame[NewOutlander].Role == null)
                {
                    game.UsersInGame[NewOutlander].Role = "Outlander";
                    game.UsersInGame[NewOutlander].RateMultiplier *= 2;
                    game.UsersInGame[NewOutlander].BaseLoseRate -= 5;
                    NetLib.SendDataToNet(game.UsersInGame[NewOutlander].ClientSocket, game.UsersInGame[NewOutlander].Role);
                }
                else
                {
                    i--;
                }
            }
            for (int i = 0; i < game.PlayersCount; i++)
            {
                if (game.UsersInGame[i].Role == null)
                {
                    game.UsersInGame[i].Role = "Resident";
                    NetLib.SendDataToNet(game.UsersInGame[i].ClientSocket, game.UsersInGame[i].Role);
                }
            }
        }
        public static void GainKeyWord(ref Game game)
        {
            Console.WriteLine("Giving keywords");
            StreamReader reader = new StreamReader("Locations.txt");
            string[] Locations = reader.ReadToEnd().Split('!');
            string[] CurrentString = new string[1];
            for (int i = 0; i < Locations.Length; i++)
            {
                CurrentString = Locations[i].Split('/');
                if (game.PlayersCount < int.Parse(CurrentString[0])) 
                {
                    CurrentString = Locations[i-1].Split('/');
                    break;
                }
            }
            reader.Close();
            Random WordChoise = new Random();
            string ResidentKeyWord = CurrentString[WordChoise.Next(1, CurrentString.Length)];
            game.KeyWord = ResidentKeyWord;
            string OutlanderKeyWord = CurrentString[WordChoise.Next(1, CurrentString.Length)];
            while (ResidentKeyWord == OutlanderKeyWord) 
            {
                OutlanderKeyWord = CurrentString[WordChoise.Next(1, CurrentString.Length)];
            }
            game.FalseKeyWord = OutlanderKeyWord;
            for (int i = 0; i < game.PlayersCount; i++)
            {
                if (game.UsersInGame[i].Role == "Resident")
                {
                    game.UsersInGame[i].KeyWord = ResidentKeyWord;
                    Console.WriteLine("KeyWord - " + game.KeyWord);
                    NetLib.SendDataToNet(game.UsersInGame[i].ClientSocket, ResidentKeyWord);
                }
                if (game.UsersInGame[i].Role == "Outlander")
                {
                    game.UsersInGame[i].KeyWord = OutlanderKeyWord;
                    NetLib.SendDataToNet(game.UsersInGame[i].ClientSocket, OutlanderKeyWord);
                }
            }
        }
        public static void RateUpdate(User user, bool IsWin)
        {
            int NewRate;
            if (IsWin)
            {
                NewRate = user.Rate + (int) (user.BaseWinRate * user.RateMultiplier);
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@name", user.Nickname);
                dynamicParameters.Add("@rate", NewRate);
                connection.Execute("UPDATE Users SET rate = @rate WHERE name = @name", dynamicParameters);
            }
            else
            {
                NewRate = user.Rate - (int)(user.BaseLoseRate * user.RateMultiplier);
                if (NewRate<0)
                {
                    NewRate = 0;
                }
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@name", user.Nickname);
                dynamicParameters.Add("@rate", NewRate);
                connection.Execute("UPDATE Users SET rate = @rate WHERE name = @name", dynamicParameters);
            }
            user.Rate = NewRate;
        }
        public static string GetKeyWordPart(string KeyWord, int Offset, int Unlocking)
        {
            string KeyWordPart = "";
            if (Offset + Unlocking > KeyWord.Length)
            {
                Unlocking = KeyWord.Length - Offset;
            }
            for (int i = Offset; i < Offset + Unlocking; i++)
            {
                KeyWordPart += KeyWord[i];
            }
            return KeyWordPart;
        }
    }
}
