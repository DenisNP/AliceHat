using System;
using System.Collections.Generic;
using System.Linq;
using AliceHat.Models;
using AliceHat.Models.Abstract;
using AliceHat.Models.Alice;

namespace AliceHat.Services
{
    public class AliceService
    {
        private readonly GameplayService _gameplayService;
        private readonly string[] _prepareButtons = {"Только я", "Заново", "Помощь", "Выход"};
        private readonly string[] _ingameButtons = {"Повтори", "Подсказка", "Какой счёт", "Начать с начала", "Помощь", "Выход" };
        private readonly string[] _yesNoButtons = {"Да", "Нет", "Помощь" };

        private readonly ISoundEngine _soundEngine = new AliceSoundEngine();
        
        public AliceService(GameplayService gameplayService)
        {
            _gameplayService = gameplayService;
        }
        
        public AliceResponse HandleRequest(AliceRequest request)
        {
            if (request.Request.Command.ToLower() == "сбросить состояние")
            {
                request.State.User = new UserState();
                request.State.Session = new SessionState();
                return Enter(request);
            }
            
            // enter
            if (request.State.Session.Step == SessionStep.None || request.IsEnter())
                return Enter(request);

            // help
            if (request.HasIntent("help"))
                return Help(request);

            // help
            if (request.HasIntent("hint"))
                return Hint(request);

            // score
            if (request.HasIntent("score"))
                return Score(request);

            // exit
            if (request.HasIntent("exit"))
                return Exit(request);
            
            // restart
            if (request.HasIntent("restart"))
                return Restart(request);

            // setup game
            if (request.State.Session.Step == SessionStep.AwaitNames)
            {
                if (request.HasIntent("restart"))
                    return Enter(request, true);
                
                return SetUpGame(request);
            }
            
            // answer
            if (request.State.Session.Step == SessionStep.Game)
            {
                if (request.HasIntent("repeat"))
                    return Repeat(request);
                
                return Answer(request);
            }

            if (request.State.Session.Step == SessionStep.AwaitRestart)
                return MaybeRestart(request);

            throw new ArgumentOutOfRangeException();
        }

        private AliceResponse Repeat(AliceRequest request)
        {
            var phrase = new Phrase(
                GameplayService.ReadWord(request.State.Session, _soundEngine, ReadMode.Repeat),
                _ingameButtons
            );
            return phrase.Generate(request);
        }

        private AliceResponse Restart(AliceRequest request)
        {
            var phrase = new Phrase("Ты точно хочешь закончить эту игру и начать новую?", _yesNoButtons);
            _gameplayService.PauseForRestart(request.State.Session);
            return phrase.Generate(request);
        }

        private AliceResponse MaybeRestart(AliceRequest request)
        {
            if (request.HasIntent("yes"))
                return Enter(request, restart: true);
            
            // continue game or exit
            SessionState state = request.State.Session;
            if (state.CurrentWord != null)
            {
                _gameplayService.Resume(state);
                return new Phrase(
                        GameplayService.ReadWord(state, _soundEngine, ReadMode.Continue),
                        _ingameButtons
                    )
                    .Generate(request);
            }
            
            // exit game
            return Exit(request);
        }

        private AliceResponse Answer(AliceRequest request)
        {
            SessionState state = request.State.Session;
            string word = state.CurrentWord.Word;
            string wordSaid = string.Join("", request.Request.Nlu.Tokens);
            AnswerResult result = _gameplayService.Answer(request.State.User, request.State.Session, wordSaid);
            string sound = result == AnswerResult.Right
                ? "[audio|dialogs-upload/008dafcd-99bc-4fd1-9561-4686c375eec6/7fbd83e1-7c22-468d-a8fe-8f0439000fd6.opus]"
                : "[audio|dialogs-upload/008dafcd-99bc-4fd1-9561-4686c375eec6/ac858f28-3c34-403c-81c7-5d64449e4ea7.opus]";
            
            string prefix = sound;

            if (result == AnswerResult.SeccondAttempt)
            {
                return Hint(request, prefix);
            }
            
            if (result == AnswerResult.Wrong)
                prefix += request.HasScreen()
                    ? $"Правильный ответ: {word.ToUpper()}.\n\n[p|300]"
                    : $"Твой ответ: {wordSaid.ToUpper()}, а правильный: {word.ToUpper()}.\n\n[p|300]";

            var phrase = new Phrase(prefix);
            if (state.CurrentWord == null)
            {
                // game finished
                phrase += new Phrase(
                    "[audio|alice-sounds-game-win-3.opus]Игра завершена!\n" +
                    $"{GameplayService.ReadScore(request.State.User, state)}\n\nХочешь начать новую игру?",
                    _yesNoButtons
                );
                return phrase.Generate(request);
            }
            
            // continue game
            if (state.NeedShowScore())
            {
                _gameplayService.SetScoreShown(state);
                
                // continue game read score
                phrase += new Phrase(
                    GameplayService.ReadScoreOnDemand(state, state.LeftShown),
                    _ingameButtons
                );

                // read left words
                if (!state.LeftShown)
                {
                    _gameplayService.SetLeftShown(state);
                    phrase += new Phrase(GameplayService.ReadWordsLeft(state));
                }
                
                // read word
                phrase += new Phrase(GameplayService.ReadWord(request.State.Session, _soundEngine, ReadMode.Normal, true));
            }
            else
            {
                // just continue game
                phrase += new Phrase(
                    GameplayService.ReadWord(request.State.Session, _soundEngine),
                    _ingameButtons
                );
            }

            return phrase.Generate(request);
        }

        private AliceResponse Exit(AliceRequest request)
        {
            var phrase = new Phrase("Выхожу из игры. Возвращайся!");
            AliceResponse response = phrase.Generate(request);
            response.Response.EndSession = true;

            return response;
        }

        private AliceResponse Hint(AliceRequest request, string prefix = "")
        {
            SessionState state = request.State.Session;
            _gameplayService.HintTaken(state);

            if (request.State.Session.Step == SessionStep.Game)
            {
                var phrase = new Phrase(
                    $"{prefix}Подскажу:\n" +
                    $"{state.CurrentWord.Definition.ToUpperFirst()}.\n" +
                    GameplayService.ReadHint(request.State.Session, _soundEngine),
                    _ingameButtons
                );

                return phrase.Generate(request);
            }

            return Help(request);
        }

        private AliceResponse Score(AliceRequest request)
        {
            Phrase phrase;
            SessionState state = request.State.Session;

            if (request.State.Session.Step == SessionStep.Game)
            {
                phrase = new Phrase(
                    GameplayService.ReadScoreOnDemand(state, false) +
                    GameplayService.ReadWordsLeft(state) +
                    GameplayService.ReadWord(request.State.Session, _soundEngine, ReadMode.Repeat, true),
                    _ingameButtons
                );
            }
            else
            {
                return Repeat(request);
            }

            return phrase.Generate(request);
        }

        private AliceResponse Help(AliceRequest request)
        {
            Phrase phrase;
            switch (request.State.Session.Step)
            {
                case SessionStep.AwaitNames:
                    phrase = new Phrase(
                        "Ты в игре «Шляпа», в которой я буду говорить тебе или вам с друзьями короткие определения, " +
                        "а вы должны отгадывать слова. Прямо сейчас назови мне имена игроков по порядку, " +
                        "либо можешь сказать, что играть будешь только ты.",
                        _prepareButtons
                    );
                    break;
                case SessionStep.Game:
                    SessionState state = request.State.Session;
                    bool multi = state.Players.Length > 1;
                    phrase = new Phrase(
                        $"{(multi ? "Вы" : "Ты")} в игре «Шляпа», в которой я говорю " +
                        $"{(multi ? "вам" : "тебе")} короткие определения, " +
                        $"а {(multi ? "вы отгадываете" : "ты отгадываешь")} слова. " +
                        $"Прямо сейчас я дала {(multi ? $"игроку по имени {state.CurrentPlayer.Name}" : "тебе")} " +
                        "очередное задание, и жду на него ответ. Можно попросить меня повторить задание, если нужно," +
                        " или пропустить его, сказав «Не знаю».",
                        _ingameButtons
                    );
                    break;
                case SessionStep.AwaitRestart:
                    phrase = new Phrase(
                        "Ты в игре «Шляпа», в которой я буду говорить тебе или вам с друзьями" +
                        " короткие определения, по которым нужно отгадывать слова. " +
                        "Сейчас ты можешь начать новую игру сказав «Да» и выйти сказав «Нет»",
                        _yesNoButtons
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return phrase.Generate(request);
        }

        private AliceResponse Enter(AliceRequest request, bool restart = false)
        {
            bool newUser = _gameplayService.EnterIsNewUser(request.State.User, request.State.Session);

            Phrase phrase = restart switch
            {
                true => new Phrase(
                    "[audio|dialogs-upload/008dafcd-99bc-4fd1-9561-4686c375eec6/cb19ca47-2ef6-4788-b09f-0d47776e4de3.opus]" +
                    "Начинаем новую игру. Перечисли имена игроков:",
                    _prepareButtons
                ),
                false when newUser => new Phrase(
                    "[audio|dialogs-upload/008dafcd-99bc-4fd1-9561-4686c375eec6/cb19ca47-2ef6-4788-b09f-0d47776e4de3.opus]" +
                    "Привет. В этой игре я буду загадывать тебе или вам с друзьями определения, " +
                    "а вы должны называть слова. Кто больше угадал — тот и выиграл.\n\n" +
                    "Для начала перечисли имена игроков:",
                    _prepareButtons
                ),
                _ => new Phrase(
                    "[audio|dialogs-upload/008dafcd-99bc-4fd1-9561-4686c375eec6/cb19ca47-2ef6-4788-b09f-0d47776e4de3.opus]" +
                    "Привет! Чтобы начать игру, перечисли имена игроков:",
                    _prepareButtons
                )
            };

            return phrase.Generate(request);
        }

        private AliceResponse SetUpGame(AliceRequest request)
        {
            Phrase phrase;
            List<string> names = request.Request.Nlu.Tokens.Except(new []{"и"}).ToList();
            if (request.HasIntent("only_me"))
                names = new List<string> {"я"};
                
            if (names.Count == 0)
            {
                phrase = new Phrase(
                    "Назови подряд имена всех игроков, либо скажи, что играть будешь только ты.",
                    _prepareButtons
                );
                return phrase.Generate(request);
            }
            else if (names.Count > 10)
            {
                phrase = new Phrase(
                    "Пока что играть могу не более десяти человек на одном устройстве. Перечисли не более десяти имён.",
                    _prepareButtons
                );
                return phrase.Generate(request);
            }

            SessionState state = request.State.Session;
            _gameplayService.Start(request.State.User, state, names.ToArray());
            
            // read first word
            string playersNum = names.Count.ToPhrase("игрок", "игрока", "игроков");
            string startPhrase = $"Отлично, {playersNum}, начинаем. " +
                                 $"\n\n{GameplayService.ReadWord(state, _soundEngine, ReadMode.First)}";

            return new Phrase(startPhrase, _ingameButtons).Generate(request);
        }
    }
}