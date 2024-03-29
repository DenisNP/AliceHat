﻿using System;
using System.Linq;
using AliceHat.Models;
using AliceHat.Models.Abstract;

namespace AliceHat.Services
{
    public class GameplayService
    {
        private readonly ContentService _contentService;

        private const int _scoreSecondAttempt = 1;
        private const int _scoreWithHint = 2;
        private const int _baseScore = 3;

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

        private static readonly string[] LetterEndings =
        {
            "заканчивается на",
            "в конце",
            "последняя буква"
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

            session.TotalWords = Utils.CalculateWordCount(playerNames);

            session.CurrentPlayerIdx = session.Players.Length - 1;
            session.WordsLeft = _contentService.GetByComplexity(session.TotalWords, user.WordIdsGot);
            session.Step = SessionStep.Game;
            user.WordIdsGot.AddRange(session.WordsLeft.Select(w => w.Id));
            while (user.WordIdsGot.Count > 100)
                user.WordIdsGot.RemoveAt(0);
            
            NextWord(session);
        }

        public void HintTaken(SessionState state)
        {
            state.HintTaken = true;
        }

        public void SecondAttempt(SessionState state)
        {
            state.SecondAttempt = true;
        }

        public AnswerResult Answer(UserState user, SessionState session, string answer)
        {
            bool right = Utils.LevenshteinMatchRatio(session.CurrentWord.Word, answer) >= 0.85;
            bool mispronounced = session.CurrentWord.Mispronounce.Contains(answer);
            AnswerResult result;

            if (right || mispronounced)
            {
                if (session.SecondAttempt)
                    session.CurrentPlayer.Score += _scoreSecondAttempt;
                else if (session.HintTaken)
                    session.CurrentPlayer.Score += _scoreWithHint;
                else
                {
                    session.CurrentPlayer.Score += _baseScore;
                }

                result = AnswerResult.Right;
            }
            else if (session.SecondAttempt || session.HintTaken)
            {
                result = AnswerResult.Wrong;
            }
            else
            {
                session.SecondAttempt = true;
                result = AnswerResult.SeccondAttempt;
            }

            if (result != AnswerResult.SeccondAttempt)
            {
                //Next word or end game
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
            }

            return result;
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
            session.HintTaken = false;
            session.SecondAttempt = false;

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
                case "Л": case "М": case "Н": case "Р": case "С":
                    return "э" + letter.ToLower();
                case "Ф":
                    return "эфф";
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

        public static string ReadWord(SessionState state, ISoundEngine soundEngine, ReadMode readMode = ReadMode.Normal, bool disableName = false)
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
            var letterText = $"{LetterPrefixes.PickRandom()} {soundEngine.GetLetterPronounce(firstLetter, GetLetterTts(firstLetter))}";
            string nameText = state.Players.Length == 1 || disableName ? infix.ToUpperFirst() : $"{state.CurrentPlayer.Name}, {infix}";
            return $"{nameText}: {soundEngine.GetPause(500)}\n{soundEngine.GetNextWordSound()}" +
                   $"{state.CurrentWord.Definition.ToUpperFirst()}, {letterText}.";
        }

        public static string ReadHint(SessionState state, ISoundEngine soundEngine)
        {
            string firstLetter = state.CurrentWord.Word.First().ToString().ToUpper();
            string lastLetter = state.CurrentWord.Word.Last().ToString().ToUpper();

            var hiddenWord =
                $"{firstLetter}{string.Join("", Enumerable.Repeat("-", state.CurrentWord.Word.Length - 2))}{lastLetter}";

            var letterStartText = GetLetterTts(firstLetter);
            var letterEndText = GetLetterTts(lastLetter);
            var letterCount = state.CurrentWord.Word.Length.ToPhrase("буква", "буквы", "букв");

            return $"{soundEngine.GetPause(500)}\n" +
                   $"[screen|{hiddenWord} ({letterCount})]" +
                   $"[voice|Всего {letterCount}, первая {letterStartText}, последняя {letterEndText}].";
        }


        public void SetScoreShown(SessionState state)
        {
            state.CurrentPlayer.ScoreShown = true;
        }

        public void SetLeftShown(SessionState state)
        {
            state.LeftShown = true;
        }

        public static string ReadScoreOnDemand(SessionState state, bool endSentence = false)
        {
            return state.Players.Length == 1
                ? $"Пока что у тебя {state.Players.First().Score.ToPhrase("очко", "очка", "очков")}{(endSentence ? ".\n" : ", ")}"
                : $"{state.CurrentPlayer.Name}, у тебя {state.CurrentPlayer.Score.ToPhrase("очко", "очка", "очков")}{(endSentence ? ".\n" : ", ")}";
        }

        public static string ReadWordsLeft(SessionState state)
        {
            return $"осталось {state.WordsLeft.Count.ToPhrase("задание", "задания", "заданий")}.\n";
        }

        public static string ReadScore(UserState user, SessionState state)
        {
            if (state.Players.Length == 1)
                return $"У тебя {state.Players.First().Score.ToPhrase("очко", "очка", "очков")} за эту игру, " +
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

    public enum AnswerResult
    {
        Right,
        SeccondAttempt,
        Wrong
    }
}