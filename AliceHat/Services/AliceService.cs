using System;
using System.Collections.Generic;
using System.Linq;
using AliceHat.Models;
using AliceHat.Models.Alice;

namespace AliceHat.Services
{
    public class AliceService
    {
        private readonly GameplayService _gameplayService;
        
        public AliceService(GameplayService gameplayService)
        {
            _gameplayService = gameplayService;
        }
        
        public AliceResponse HandleRequest(AliceRequest request)
        {
            // enter
            if (request.State.Session.Step == SessionStep.None)
                return Enter(request);

            // help
            if (request.HasIntent("help"))
                return Help(request);

            // exit
            if (request.HasIntent("exit"))
                return Exit(request);
            
            // restart
            if (request.HasIntent("restart"))
                return Restart(request);

            // setup game
            if (request.State.Session.Step == SessionStep.AwaitNames)
                return SetUpGame(request);
            
            // answer
            if (request.State.Session.Step == SessionStep.Game)
                return Answer(request);

            if (request.State.Session.Step == SessionStep.AwaitRestart)
                return MaybeRestart(request);

            throw new ArgumentOutOfRangeException();
        }

        private AliceResponse Restart(AliceRequest request)
        {
            throw new NotImplementedException();
        }

        private AliceResponse MaybeRestart(AliceRequest request)
        {
            throw new NotImplementedException();
        }

        private AliceResponse Answer(AliceRequest request)
        {
            throw new NotImplementedException();
        }

        private AliceResponse Exit(AliceRequest request)
        {
            throw new NotImplementedException();
        }

        private AliceResponse Help(AliceRequest request)
        {
            throw new NotImplementedException();
        }

        private AliceResponse Enter(AliceRequest request)
        {
            bool newUser = _gameplayService.EnterIsNewUser(request.State.User, request.State.Session);

            Phrase phrase = newUser
                ? new Phrase(
                    "Привет. В этой игре я буду загадывать тебе или вам с друзьями определения, " +
                    "а вы должны называть слова. Кто больше угадал — тот и выиграл.\n\n" +
                    "Для начала перечисли имена игроков.",
                    new[] {"Только я", "Помощь", "Выход"}
                )
                : new Phrase(
                    "Привет, сыграем в шляпу. Перечисли имена игроков.",
                    new[] {"Только я", "Помощь", "Выход"}
                );

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
                    new[] {"Только я", "Помощь", "Выход"}
                );
                return phrase.Generate(request);
            }
            else if (names.Count > 10)
            {
                phrase = new Phrase(
                    "Пока что играть могу не более десяти человек на одном устройстве. Перечисли не более десяти имён.",
                    new[] {"Помощь", "Выход"}
                );
                return phrase.Generate(request);
            }
                
            _gameplayService.Start(request.State.User, request.State.Session, names.ToArray());
            
            // read first word
            
        }
    }
}