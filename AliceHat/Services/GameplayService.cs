using System;
using System.Linq;
using AliceHat.Models;

namespace AliceHat.Services
{
    public class GameplayService
    {
        private readonly ContentService _contentService;
        
        private static readonly string[] InfixesSingle =
        {
            "следующее слово",
            "ещё одно задание",
            "следующее задание",
            "ещё одно слово"
        };
        
        private static readonly string[] InfixesFirst =
        {
            "слово для тебя",
            "слово тебе",
            "задание для тебя",
        };
        private static readonly string[] InfixesNext =
        {
            "следующее слово для тебя",
            "следующее слово тебе",
            "следующее задание для тебя",
            "следующее слово твоё",
            "вот слово для тебя",
            "вот задание для тебя",
            "вот твоё слово",
            "отгадай, что это"
        };

        private static readonly string[] LetterPrefixes =
        {
            "начинается на",
            "на букву",
            "первая буква"
        };

        public GameplayService(ContentService contentService)
        {
            _contentService = contentService;
        }
        
        public bool EnterIsNewUser(UserState user, SessionState session)
        {
            bool newUser = user.LastEnter < DateTime.Now - TimeSpan.FromDays(15);
            
            user.LastEnter = DateTime.Now;
            session.Clear();
            session.Step = SessionStep.AwaitNames;

            return newUser;
        }

        public void Start(UserState user, SessionState session, string[] playerNames)
        {
            session.Players = playerNames.Select(name => new Player
            {
                Name = name.ToLower().ToUpperFirst(),
                Score = 0
            }).ToArray();

            int wordsCount = playerNames.Length switch
            {
                1 => 5,
                2 => 4,
                >= 3 and <= 4 => 3,
                _ => 2
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
            bool right = Utils.LevenshteinMatchRatio(session.CurrentWord.Word, answer) >= 0.65;
            if (right)
                session.CurrentPlayer.Score++;

            if (session.WordsLeft.Count > 0)
            {
                NextWord(session);
            }
            else
            {
                // single player, write score
                if (session.Players.Length == 1) 
                    user.TotalScore += session.Players.First().Score;
                
                session.CurrentWord = null;
                session.Step = SessionStep.AwaitRestart;
            }

            return right;
        }

        public void PauseForRestart(SessionState session)
        {
            session.Step = SessionStep.AwaitRestart;
        }

        public void Resume(SessionState session)
        {
            session.Step = SessionStep.Game;
        }

        private void NextWord(SessionState session)
        {
            session.CurrentWord = session.WordsLeft[0];
            session.WordsLeft.RemoveAt(0);
            
            session.CurrentPlayerIdx++;
            if (session.CurrentPlayerIdx >= session.Players.Length)
                session.CurrentPlayerIdx = 0;
        }
        
        public static string GetLetterTts(string letter)
        {
            switch (letter)
            {
                case "Б": case "В": case "Г": case "Д": case "Ж": case "З": case "П": case "Т": case "Ц": case "Ч":
                    return letter.ToLower() + "э";
                case "К": case "Х": case "Ш": case "Щ": case "А":
                    return letter.ToLower() + "а";
                case "Л": case "М": case "Н": case "Р": case "С": case "Ф":
                    return "э" + letter.ToLower();
                case "И":
                    return "ии";
                case "О":
                    return "оо";
                case "Й":
                    return "и краткое";
                case "Ь":
                    return "мягкий знак";
                case "Ъ":
                    return "твёрдый знак";
                default:
                    return letter.ToLower();
            }
        }
        
        public static string ReadWord(SessionState state, ReadMode readMode = ReadMode.Normal)
        {
            string infix;
            if (state.Players.Length == 1)
            {
                infix = readMode switch
                {
                    ReadMode.Normal => InfixesSingle.PickRandom(),
                    ReadMode.First => "первое задание",
                    ReadMode.Repeat => "повторяю задание",
                    ReadMode.Continue => InfixesSingle.PickRandom(),
                    _ => throw new ArgumentOutOfRangeException(nameof(readMode), readMode, null)
                };
            }
            else
            {
                if (readMode != ReadMode.Normal)
                {
                    string prefix = readMode switch
                    {
                        ReadMode.First => "первое",
                        ReadMode.Repeat => "повторяю",
                        ReadMode.Continue => "",
                        _ => throw new ArgumentOutOfRangeException(nameof(readMode), readMode, null)
                    };

                    infix = $"{prefix} {InfixesFirst.PickRandom()}";
                }
                else
                {
                    infix = InfixesNext.PickRandom();
                }
            }

            string firstLetter = state.CurrentWord.Word.First().ToString().ToUpper();
            var letterText = $"{LetterPrefixes.PickRandom()} [screen|{firstLetter}][voice|{GetLetterTts(firstLetter)}]";
            string nameText = state.Players.Length == 1 ? infix.ToUpperFirst() : $"{state.CurrentPlayer.Name}, {infix}";
            return $"{nameText}: [p|500]\n{state.CurrentWord.Definition.ToUpperFirst()}, {letterText}.";
        }

        public static string ReadScore(UserState user, SessionState state)
        {
            if (state.Players.Length == 1)
                return $"У тебя {state.Players.First().Score.ToPhrase("очко", "очка", "очков")} за игру игру, " +
                       $"и {user.TotalScore.ToPhrase("очко", "очка", "очков")} всего!";
            
            return string.Join(
                "\n",
                state.Players
                    .OrderByDescending(p => p.Score)
                    .Select(p => $"{p.Name} — {p.Score.ToPhrase("очко", "очка", "очков")}")
            );
        }
    }
    
    public enum ReadMode
    {
        Normal,
        First,
        Repeat,
        Continue
    }
}