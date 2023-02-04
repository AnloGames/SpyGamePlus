using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SpyGamePlus_Server.Classes
{
    class Game
    {
        public bool IsGameStarted = false;
        public List<User> UsersInGame;
        public List<User> SpiesInGame;
        public int PlayersCount;
        public int SpiesCount;
        public int OutlandersInGame;
        public string KeyWord;
        public string FalseKeyWord;
        public ManualResetEvent Waiting = new ManualResetEvent(true);
        public int PlayersNow;
        public int GameRate;

        public Game(int PlayersCount)
        {
            this.PlayersCount = PlayersCount;
            PlayersNow = PlayersCount;
            UsersInGame = new List<User>();
            SpiesInGame = new List<User>();
            if (PlayersCount >= 2 && PlayersCount <= 5)
            {
                SpiesCount = 1;
                OutlandersInGame = 0;
            }
            else if (PlayersCount == 6 || PlayersCount == 7)
            {
                SpiesCount = 1;
                OutlandersInGame = 1;
            }
            else if (PlayersCount == 8 || PlayersCount == 9)
            {
                SpiesCount = 2;
                OutlandersInGame = 1;
            }
            else if (PlayersCount >= 10 || PlayersCount <= 13)
            {
                SpiesCount = 2;
                OutlandersInGame = 2;
            }
            else if (PlayersCount>=14)
            {
                SpiesCount = 3;
                OutlandersInGame = 3;
            }
        }
        public void GameRateUpdate()
        {
            int RateSum = 0;
            for (int i = 0; i < UsersInGame.Count; i++)
            {
                RateSum += UsersInGame[i].Rate;
            }
            GameRate = RateSum/UsersInGame.Count;
        }
    }
}
