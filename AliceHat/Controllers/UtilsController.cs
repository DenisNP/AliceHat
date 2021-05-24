using System;
using System.Collections.Generic;
using AliceHat.Models;
using AliceHat.Services;
using Microsoft.AspNetCore.Mvc;

namespace AliceHat.Controllers
{
    [ApiController]
    [Route("/utils")]
    public class UtilsController
    {
        private readonly ContentService _contentService;
        private readonly TelegramService _telegramService;

        public UtilsController(ContentService contentService, TelegramService telegramService)
        {
            _contentService = contentService;
            _telegramService = telegramService;
        }
        
        [HttpGet]
        public string Get()
        {
            return "It works!";
        }

        // [HttpGet("word/{complexity}")]
        [HttpGet("word")]
        public ContentResult Word(/*string complexity*/)
        {
            // var c = Enum.Parse<Complexity>(complexity);
            List<WordData> w = _contentService.GetByComplexity(1/*, c*/);
            var text = $"<p>{w[0].Definition}</p><p><font color='white'>{w[0].Word}</font></p>";
            
            return new ContentResult
            {
                ContentType = "text/html",
                Content = $"<html><head><meta charset=\"utf-8\"></head><body>{text}</body></html>"
            };
        }

        [HttpGet("send")]
        public ContentResult SendToAll([FromQuery(Name = "text")] string text, [FromQuery(Name="me")] int me)
        {
            if (me == 0)
            {
                _telegramService.SendAll(text.Replace(@"\n", "\n"));
            }
            else
            {
                _telegramService.SendMe(text.Replace(@"\n", "\n"));
            }

            return new ContentResult
            {
                ContentType = "text/html",
                Content = $"<html><head><meta charset=\"utf-8\"></head><body>{text}</body></html>"
            };
        }
    }
}