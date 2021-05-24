using System;
using System.IO;
using System.Threading.Tasks;
using AliceHat.Models;
using AliceHat.Models.Alice;
using AliceHat.Models.Alice.Abstract;
using AliceHat.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AliceHat.Controllers
{
    [ApiController]
    [Route("/alice")]
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

        public AliceController(AliceService aliceService)
        {
            _aliceService = aliceService;
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
                AliceResponseBase<UserState, SessionState> pong = new AliceResponse(request).ToPong();
                string pongResponse = JsonConvert.SerializeObject(pong, ConverterSettings);
                return Response.WriteAsync(pongResponse);
            }

            Console.WriteLine($"REQUEST:\n{JsonConvert.SerializeObject(request, ConverterSettings)}\n");

            try
            {
                AliceResponse response = _aliceService.HandleRequest(request);
                string stringResponse = JsonConvert.SerializeObject(response, ConverterSettings);

                Console.WriteLine($"RESPONSE:\n{stringResponse}\n");
            
                return Response.WriteAsync(stringResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return Response.WriteAsync(
                    JsonConvert.SerializeObject(
                        new Phrase("Возникла какая-то ошибка, разработчик уже уведомлён").Generate(request),
                        ConverterSettings
                    )
                );
            }
        }
    }
}