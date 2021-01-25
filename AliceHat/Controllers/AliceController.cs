using System;
using System.IO;
using System.Threading.Tasks;
using AliceHat.Models;
using AliceHat.Models.Alice;
using AliceHat.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AliceHat.Controllers
{
    [ApiController]
    [Route("/")]
    public class AliceController : ControllerBase
    {
        private static readonly JsonSerializerSettings ConverterSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Include
        };
        private readonly AliceService _aliceService;
        private readonly ContentService _contentService;
        private readonly TelegramService _telegramService;

        public AliceController(AliceService aliceService, ContentService contentService, TelegramService telegramService)
        {
            _aliceService = aliceService;
            _contentService = contentService;
            _telegramService = telegramService;
        }
        
        [HttpGet]
        public string Get()
        {
            return "It works!";
        }

        [HttpGet("/word/{complexity}")]
        public ContentResult Word(string complexity)
        {
            var c = Enum.Parse<Complexity>(complexity);
            var w = _contentService.GetByComplexity(1, c);
            var text = $"<p>{w[0].Definition}</p><p><font color='white'>{w[0].Word}</font></p>";
            
            return new ContentResult
            {
                ContentType = "text/html",
                Content = $"<html><head><meta charset=\"utf-8\"></head><body>{text}</body></html>"
            };
        }

        [HttpGet("/send")]
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

        [HttpPost]
        public Task Post()
        {
            using var reader = new StreamReader(Request.Body);
            string body = reader.ReadToEnd();

            var request = JsonConvert.DeserializeObject<AliceRequest>(body, ConverterSettings);
            if (request == null)
            {
                Console.WriteLine("Request is null:");
                Console.WriteLine(body);
                return Response.WriteAsync("Request is null");
            }
            
            if (request.IsPing())
            {
                var pong = new AliceResponse(request).ToPong();
                string pongResponse = JsonConvert.SerializeObject(pong, ConverterSettings);
                return Response.WriteAsync(pongResponse);
            }

            Console.WriteLine($"REQUEST:\n{JsonConvert.SerializeObject(request, ConverterSettings)}\n");
            
            AliceResponse response = _aliceService.HandleRequest(request);
            string stringResponse = JsonConvert.SerializeObject(response, ConverterSettings);

            Console.WriteLine($"RESPONSE:\n{stringResponse}\n");
            
            return Response.WriteAsync(stringResponse);
        }
    }
}