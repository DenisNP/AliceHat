using System;
using AliceHat.Models.Alice;

namespace AliceHat.Services
{
    public class AliceService
    {
        private readonly ContentService _contentService;
        
        public AliceService(ContentService contentService)
        {
            _contentService = contentService;
        }
        
        public AliceResponse HandleRequest(AliceRequest request)
        {
            throw new NotImplementedException();
        }
    }
}