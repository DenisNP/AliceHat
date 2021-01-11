using System;
using System.IO;
using System.Threading.Tasks;
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

        public AliceController(AliceService aliceService)
        {
            _aliceService = aliceService;
        }
        
        [HttpGet]
        public string Get()
        {
            return "It works!";
        }

        [HttpPost]
        public Task Post()
        {
            using var reader = new StreamReader(Request.Body);
            var body = reader.ReadToEnd();

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
                var pongResponse = JsonConvert.SerializeObject(pong, ConverterSettings);
                return Response.WriteAsync(pongResponse);
            }

            Console.WriteLine($"REQUEST:\n{JsonConvert.SerializeObject(request, ConverterSettings)}\n");
            
            var response = _aliceService.HandleRequest(request);
            var stringResponse = JsonConvert.SerializeObject(response, ConverterSettings);

            Console.WriteLine($"RESPONSE:\n{stringResponse}\n");
            
            return Response.WriteAsync(stringResponse);
        }
    }
}