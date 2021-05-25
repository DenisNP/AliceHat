using System.Collections.Generic;
using Newtonsoft.Json;

namespace AliceHat.Models
{
    public class SessionState
    {
        public SessionStep Step { get; set; }
        public WordData CurrentWord { get; set; }
        public Player[] Players { get; set; }
        public int CurrentPlayerIdx { get; set; } = 0;
        public List<WordData> WordsLeft { get; set; }

        [JsonIgnore]
        public Player CurrentPlayer => Players != null && Players.Length > CurrentPlayerIdx
            ? Players[CurrentPlayerIdx]
            : null;

        public void Clear()
        {
            CurrentWord = null;
            Players = null;
            CurrentPlayerIdx = 0;
            WordsLeft = null;
        }
    }

    public class Player
    {
        public string Name { get; set; }
        public int Score { get; set; }
    }

    public enum SessionStep
    {
        None,
        AwaitNames,
        Game,
        AwaitRestart
    }
}