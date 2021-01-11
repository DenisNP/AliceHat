using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AliceHat.Models;
using Microsoft.Extensions.Logging;

namespace AliceHat.Services
{
    public class ContentService
    {
        private readonly ILogger<ContentService> _logger;
        private readonly Dictionary<Complexity, List<WordData>> _words = new();
        
        public ContentService(ILogger<ContentService> logger)
        {
            _logger = logger;
        }

        public void LoadWords(string csv)
        {
            _logger.LogInformation($"Loading file: {csv}");

            var lines = File.ReadLines(csv);
            foreach (var line in lines)
            {
                if (line.IsNullOrEmpty()) continue;

                // extract data from line
                var data = line.Split("|");
                
                var word = data[0];
                var complexity = Enum.Parse<Complexity>(data[1]);
                var definitions = data.Skip(2).Select(x => x.ToUpperFirst()).ToArray();
                
                // write new data
                var wordData = new WordData
                {
                    Word = word,
                    Complexity = complexity,
                    Definitions = definitions
                };
                
                if (!_words.ContainsKey(complexity)) _words.Add(complexity, new List<WordData>());
                _words[complexity].Add(wordData);
            }
            
            _logger.LogInformation($"Done. Words loaded: {_words.Values.Sum(l => l.Count)}");
        }
    }
}