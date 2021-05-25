﻿using System;
using System.Linq;
using AliceHat.Models;

namespace AliceHat.Services
{
    public class GameplayService
    {
        private readonly ContentService _contentService;
        
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

        public bool Answer(SessionState session, string answer)
        {
            bool right = session.CurrentWord.Word == answer;
            if (right)
                session.CurrentPlayer.Score++;

            if (session.WordsLeft.Count > 0)
            {
                NextWord(session);
            }
            else
            {
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

            string firstLetter = state.CurrentWord.Word.First().ToString().ToUpper();
            var letterText = $"{LetterPrefixes.PickRandom()} [screen|{firstLetter}][voice|{GetLetterTts(firstLetter)}]";
            return $"{state.CurrentPlayer.Name}, {infix}: [p|500]\n{state.CurrentWord.Definition.ToUpperFirst()}, {letterText}.";
        }

        public static string ReadScore(SessionState state)
        {
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