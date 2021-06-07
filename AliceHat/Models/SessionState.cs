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
        public int TotalWords { get; set; }
        public bool LeftShown { get; set; }

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
            TotalWords = 0;
            LeftShown = false;
        }

        public bool NeedShowScore()
        {
            return !CurrentPlayer.ScoreShown && WordsLeft.Count <= TotalWords / 2;
        }
    }

    public class Player
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public bool ScoreShown { get; set; }
    }

    public enum SessionStep
    {
        None,
        AwaitNames,
        Game,
        AwaitRestart
    }
}