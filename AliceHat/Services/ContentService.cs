using Microsoft.Extensions.Logging;

namespace AliceHat.Services
{
    public class ContentService
    {
        private readonly ILogger<ContentService> _logger;
        
        public ContentService(ILogger<ContentService> logger)
        {
            _logger = logger;
        }

        public void LoadWords(string csv)
        {
            _logger.LogInformation($"Loading file: {csv}");
            throw new System.NotImplementedException();
            _logger.LogInformation("Done");
        }
    }
}