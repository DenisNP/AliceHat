using System;
using AliceHat.Services.Abstract;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace AliceHat.Services
{
    public class TelegramService
    {
        private readonly ILogger<TelegramService> _logger;
        private readonly IDbService _dbService;
        private readonly ContentService _contentService;
        private readonly string _botToken;
        private readonly TelegramBotClient _telegram;
        
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
            var req = new SendMessageRequest(update.Message.Chat.Id, "Тест");
            _telegram.MakeRequestAsync(req);
        }
    }
}