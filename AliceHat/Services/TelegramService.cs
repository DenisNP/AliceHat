using System;
using System.Linq;
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
                if (input == "/word")
                {
                    // command to give new word
                    GiveNewWord(user);
                }
                else 
                {
                    var m = "Это бот для набора определений слов в игру <b>Шляпа</b>. " +
                            "Он присылает <i>кривое</i> определение из словаря, а вам нужно вместо него написать " +
                            "<i>хорошее</i> определение для игры.\n\n<b>Как писать:</b>\n" +
                            "Коротко, но ёмко. По одной фразе игрок должен сразу понимать, о чём речь. " +
                            "Старайтесь избегать определений из <i>одного</i> слова, но и больше <i>семи</i> слов — " +
                            "скорее всего неудачный вариант. И объяснение не должно содержать однокоренных слов. " +
                            "Представьте, что вы пишете задание для <i>сканворда</i>.\n\n<b>Хорошо</b>:\n" +
                            "<i>Большая рыба с усами (сом)</i>; <i>Колокольный блок в церкви (звонница)</i>\n" +
                            "<b>Плохо:</b>\n" +
                            "<i>Высшая мера наказания (вышка)</i>; <i>Устройство в виде металлического полотна с зубьями" +
                            " для разрезания древесины или металла (ножовка)</i>" +
                            "\n\n/word — получить новое слово\n/keep — вместо написания определения оставить существующее";

                    if (user.WordsProcessed > 0) m += $"\n\nВы обработали слов: {user.WordsProcessed}";

                    _telegram.SendTextMessageAsync(new ChatId(userId.Value), m, ParseMode.Html);
                }
            }
            else if (user.LastWord != null)
            {
                if (update.Message?.Text != null && update.Message.Text != "")
                {
                    var input = update.Message.Text;

                    string m;
                    if (input != "/keep")
                    {
                        user.LastWord.Definition = input;
                        _dbService.Update(user);
                        
                        m = $"Определение слова обновлено.\n\n{GetWordInfo(user.LastWord, true)}\n\n" +
                            "Можете изменить его ещё раз. Для завершения выберите сложность, " +
                            "как вы думаете, насколько это сложное слово?";
                    }
                    else
                    {
                        m = $"Определение слова сохранено как есть.\n\n<i>{GetWordInfo(user.LastWord, true)}</i>\n\n" +
                            "Можете изменить его. Для завершения выберите сложность, " +
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
                        var m = $"{GetWordInfo(user.LastWord, true)}\n\nНапишите мне текстом определение " +
                                "получше, либо используйте /keep, чтобы оставить это, если оно кажется вам подходящим " +
                                "для игры";

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

                        var m = $"Готово, слово записано! Всего вы обработали слов: <b>{user.WordsProcessed}</b>\n\n" +
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

        private void GiveNewWord(TgUser user)
        {
            // select random word
            var wordsCount = _dbService.Collection<WordData>()
                .Count(w => w.Status == WordStatus.Untouched);

            var index = _random.Next(0, wordsCount);

            var word = _dbService.Collection<WordData>()
                .Where(w => w.Status == WordStatus.Untouched)
                .Skip(index)
                .First();

            word.Definition = word.Definition.ToLowerFirst();

            // update word status
            word.Status = WordStatus.Taken;
            _dbService.Update(word);
            
            // update user data
            user.LastWord = word;
            user.LastTimeWord = DateTime.UtcNow;
            _dbService.Update(user);
            
            // send message
            var m = $"Следующее слово:\n\n{GetWordInfo(word, false)}" +
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
            return withWord ? $"<i>{w.Word} — {w.Definition}</i>" : $"<i>{w.Definition}</i>";
        }
    }
}