using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace SpyGamePlus_Server.Classes
{
    class User
    {
        public Socket ClientSocket;
        private string nickname;
        private string password;
        public int PlayersCount;
        public int Rate;
        public string Role = null;
        public string KeyWord;
        public string SpyKeyWord = "";
        public int Votes = 0;
        public Game UserGame;
        public string UserState;
        public int BaseWinRate = 10;
        public int BaseLoseRate = 10;
        public double RateMultiplier;

        public string Nickname { get; set; }
        public string Password { get; set; }

        public User(Socket ClientSocket)
        {
            this.ClientSocket = ClientSocket;
        }
        public User(Socket ClientSocket, string Name, int Rate)
        {
            this.ClientSocket = ClientSocket;
            Nickname = Name;
            this.Rate = Rate;
        }
        public User()
        {

        }
    }
}
