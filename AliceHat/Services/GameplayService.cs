using System.Linq;
using AliceHat.Models;

namespace AliceHat.Services
{
    public class GameplayService
    {
        private readonly ContentService _contentService;

        public GameplayService(ContentService contentService)
        {
            _contentService = contentService;
        }
        
        public void Enter(UserState user, SessionState session)
        {
            session.Step = SessionStep.AwaitNames;
        }

        public void Start(UserState user, SessionState session, string[] playerNames)
        {
            session.Players = playerNames.Select(name => new Player
            {
                Name = name,
                Score = 0
            }).ToArray();

            int wordsCount = playerNames.Length switch
            {
                1 => 10,
                >= 2 and <= 3 => 5,
                4 => 4,
                _ => 3
            };

            session.CurrentPlayerIdx = session.Players.Length - 1;
            session.WordsLeft = _contentService.GetByComplexity(wordsCount, user.WordIdsGot);
            session.Step = SessionStep.Game;
            user.WordIdsGot.AddRange(session.WordsLeft.Select(w => w.Id));
            while (user.WordIdsGot.Count > 100)
                user.WordIdsGot.RemoveAt(0);
            
            NextWord(session);
        }

        public bool Answer(UserState user, SessionState session, string answer)
        {
            bool right = session.CurrentWord.Word == answer;
            if (right)
                session.CurrentPlayer.Score++;

            if (session.WordsLeft.Count > 0)
                NextWord(session);
            else
                session.Step = SessionStep.AwaitRestart;

            return right;
        }

        private void NextWord(SessionState session)
        {
            session.CurrentWord = session.WordsLeft[0];
            session.WordsLeft.RemoveAt(0);
            
            session.CurrentPlayerIdx++;
            if (session.CurrentPlayerIdx >= session.Players.Length)
                session.CurrentPlayerIdx = 0;
        }
    }
}