using System;
using System.Linq;
using System.Text.RegularExpressions;
using AliceHat.Models;
using AliceHat.Services.Abstract;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AliceHat.Services
{
    public class TelegramService
    {
        private const int MyTelegramId = 928079;
        
        private readonly ILogger<TelegramService> _logger;
        private readonly IDbService _dbService;
        private readonly ContentService _contentService;
        private readonly string _botToken;
        private readonly TelegramBotClient _telegram;
        private readonly Random _random = new Random();
        
        public TelegramService(ILogger<TelegramService> logger, IDbService dbService, ContentService contentService)
        {
            _logger = logger;
            _dbService = dbService;
            _contentService = contentService;
            
            _botToken = Environment.GetEnvironmentVariable("ALICEHAT_TELEGRAM_TOKEN")
                        ?? throw new ArgumentException("Specify ALICEHAT_TELEGRAM_TOKEN variable");

            _telegram = new TelegramBotClient(_botToken);
            
            _logger.LogInformation($"Telegram service created: {_telegram.BotId}");
        }

        public void Start()
        {
            _telegram.StartReceiving();
            
            _telegram.OnUpdate += (_, args) =>
            {
                HandleUpdate(args.Update);
            };
            
            _logger.LogInformation("Telegram service started in poll mode");
        }

        public string GetToken()
        {
            return _botToken;
        }

        public void SendAll(string text)
        {
            var allUsers = _dbService.Collection<TgUser>().ToList();

            foreach (TgUser user in allUsers) 
                _telegram.SendTextMessageAsync(new ChatId(int.Parse(user.Id)), text, ParseMode.Html);
        }

        public void SendMe(string text)
        {
            _telegram.SendTextMessageAsync(new ChatId(MyTelegramId), text, ParseMode.Html);
        }

        public void HandleUpdate(Update update)
        {
            var userId = update?.Message?.From?.Id ?? update?.CallbackQuery?.From?.Id;
            if (!userId.HasValue) return;

            // read or create user
            var user = _dbService.ById<TgUser>(userId.Value.ToString());
            if (user == null)
            {
                user = new TgUser { Id = userId.Value.ToString() };
                _dbService.Update(user);
            }
            
            // on idle state
            if (user.LastWord == null && update.Message != null)
            {
                var input = update.Message.Text;
                if (input.StartsWith("/word"))
                {
                    var inputData = input.Split(" ");
                    var forceWord = inputData.Length > 1 ? inputData[1] : "";
                    
                    // command to give new word
                    GiveNewWord(user, false, forceWord);
                }
                else 
                {
                    var m = "Это бот для набора определений слов в игру <b>Шляпа</b>. " +
                            "Он присылает кривое определение из словаря, а вам нужно вместо него написать " +
                            "<i>хорошее</i> определение для игры.\n\n<b>Как писать:</b>\n" +
                            "Вместе с заданием бот будет подсказывать первую букву слова, поэтому определение " +
                            "должно быть не слишком простым по смыслу, но коротким по длине: " +
                            "в идеале 1-4 слова. Думайте, как такое слово было бы определено <b>в сканворде</b>. " +
                            "Используйте ассоциации и переносные значения. Но если слово само по себе сложное, можно " +
                            "описывать его буквально." +
                            "\n\n<b>Хорошо</b>:\n" +
                            "<i>Усатый плавун (сом)</i>; <i>«Оркестр» для монаха (звонница)</i>;\n" +
                            "<b>Плохо:</b>\n" +
                            "<i>Большая пресноводная рыба с усами (сом)</i>; <i>Колокольный блок в церкви (звонница)</i>;" +
                            "\n<b>Допустимо</b>:\n<i>Военное пальто с подкладкой " +
                            "(бушлат, определение буквальное, потому что слово сложное)</i>" +
                            "\n\n/word — получить новое слово\nПо всем вопросам — @peshekhonov";

                    if (user.WordsProcessed > 0)
                    {
                        var count = _dbService.Collection<WordData>().Count(w => w.Status == WordStatus.Ready);
                        m += $"\n\nВы обработали слов: <b>{user.WordsProcessed}</b>" +
                             $"\nВсего в базе обработанных слов: <b>{count}</b>";
                    }

                    _telegram.SendTextMessageAsync(new ChatId(userId.Value), m, ParseMode.Html);
                }
            }
            else if (user.LastWord != null)
            {
                if (update.Message?.Text != null && update.Message.Text != "")
                {
                    var input = update.Message.Text;

                    if (input == "/word")
                    {
                        GiveNewWord(user, true);
                        return;
                    }

                    string m;
                    if (input == "/keep")
                    {
                        m = $"Определение слова оставлено как есть.\n\n{GetWordInfo(user.LastWord, true)}\n\n" +
                            "Можете изменить его. Для завершения выберите сложность, " +
                            "как вы думаете, насколько это сложное слово?";
                    }
                    else
                    {
                        var tokens = Regex.Split(input.ToLower(), @"[\.,\-\s\(\)""'!?—:;]+");
                        if (tokens.Count(t => t.Length > 3) > 4)
                        {
                            _telegram.SendTextMessageAsync(
                                new ChatId(userId.Value),
                                "Определение слишком длинное. Используйте переносные значения, метафоры, игру слов. " +
                                "Или можете пропустить слово командой /word",
                                ParseMode.Html
                            );
                            return;
                        }

                        user.LastWord.Definition = input.ToLowerFirst();
                        _dbService.Update(user);
                        
                        m = $"Определение слова обновлено.\n\n{GetWordInfo(user.LastWord, true)}\n\n" +
                            "Можете изменить его ещё раз. Для завершения выберите сложность, " +
                            "как вы думаете, насколько это сложное слово?";
                    }

                    var kb = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        new()
                        {
                            CallbackData = Complexity.Low.ToString(),
                            Text = "Простое"
                        },
                        new()
                        {
                            CallbackData = Complexity.Medium.ToString(),
                            Text = "Среднее"
                        },
                        new()
                        {
                            CallbackData = Complexity.Low.ToString(),
                            Text = "Сложное"
                        },
                    });
                    
                    _telegram.SendTextMessageAsync(new ChatId(userId.Value), m, ParseMode.Html, replyMarkup: kb);
                } 
                else if (update.CallbackQuery != null)
                {
                    if (update.CallbackQuery.Data == "show_word")
                    {
                        var m = $"{GetWordInfo(user.LastWord, true)}\n\n" +
                                "Напишите мне текстом определение <b>в стиле сканвордов</b>: 1-3 слова по возможности. " +
                                "\n\n/keep — оставить текущее определение\n/word — пропустить слово";

                        _telegram.AnswerCallbackQueryAsync(update.CallbackQuery.Id);

                       _telegram.EditMessageTextAsync(
                            new ChatId(userId.Value),
                            update.CallbackQuery.Message.MessageId,
                            m,
                            ParseMode.Html,
                            replyMarkup: new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton>())
                        );
                    }
                    else if (Enum.TryParse<Complexity>(update.CallbackQuery.Data, out var complexity))
                    {
                        // update word
                        user.LastWord.Complexity = complexity;
                        user.LastWord.Status = WordStatus.Ready;
                        
                        _dbService.Update(user.LastWord);
                        
                        // update user
                        user.WordsProcessed++;
                        user.LastWord = null;
                        
                        _dbService.Update(user);
                        
                        _telegram.AnswerCallbackQueryAsync(update.CallbackQuery.Id);

                        var allCount = "";
                        if (user.WordsProcessed % 10 == 0)
                        {
                            var count = _dbService.Collection<WordData>().Count(w => w.Status == WordStatus.Ready);
                            allCount = $"\nВсего в базе обработанных слов: <b>{count}</b>";
                        }
                        var m = $"Готово, слово записано!\nВы обработали слов: <b>{user.WordsProcessed}</b>{allCount}\n\n" +
                                "/word — ещё слово";

                        _telegram.EditMessageReplyMarkupAsync(
                            new ChatId(userId.Value),
                            update.CallbackQuery.Message.MessageId,
                            new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton>())
                        );
                        
                        _telegram.SendTextMessageAsync(new ChatId(userId.Value), m, ParseMode.Html);
                    }
                }
            }
        }

        private void GiveNewWord(TgUser user, bool skipped = false, string forceWord = "")
        {
            WordData word;

            if (forceWord.IsNullOrEmpty())
            {
                // select random word
                var wordsCount = _dbService.Collection<WordData>()
                    .Count(w => w.Status == WordStatus.Untouched);

                var index = _random.Next(0, wordsCount);

                word = _dbService.Collection<WordData>()
                    .Where(w => w.Status == WordStatus.Untouched)
                    .Skip(index)
                    .First();
            }
            else
            {
                word = _dbService.Collection<WordData>().First(w => w.Word == forceWord);
            }

            word.Definition = word.Definition.ToLowerFirst();

            // update word status
            word.Status = WordStatus.Taken;
            _dbService.Update(word);
            
            // update user data
            user.LastWord = word;
            user.LastTimeWord = DateTime.UtcNow;
            _dbService.Update(user);
            
            // send message
            var m = $"{(skipped ? "Предыдущее слово пропущено. " : "")}Следующее слово:\n\n{GetWordInfo(word, false)}" +
                    "\n\nМожете подумать, что это, а затем нажать кнопку ниже и узнать ответ.";
            
            _telegram.SendTextMessageAsync(
                new ChatId(int.Parse(user.Id)),
                m,
                ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton
                {
                    CallbackData = "show_word",
                    Text = "Показать слово"
                }));
        }

        private string GetWordInfo(WordData w, bool withWord)
        {
            var def = $"{w.Definition}; на букву {w.Word[0].ToString().ToUpper()}";
            return withWord ? $"<i>{w.Word} — {def}</i>" : $"<i>{def}</i>";
        }
    }
}