using System.Threading.Tasks;
using AliceHat.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace AliceHat.Controllers
{
    [ApiController]
    [Route("/telegram")]
    public class TelegramController : ControllerBase
    {
        private readonly TelegramService _telegramService;

        public TelegramController(TelegramService telegramService)
        {
            _telegramService = telegramService;
        }

        [HttpPost("/{token}")]
        public Task Post([FromBody]Update update, string token)
        {
            if (token != _telegramService.GetToken())
                return Response.WriteAsync("Token is wrong");
            
            _telegramService.HandleUpdate(update);
            return Response.WriteAsync("ok");
        }
    }
}