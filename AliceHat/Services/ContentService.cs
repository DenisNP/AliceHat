using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AliceHat.Models;
using Microsoft.Extensions.Logging;

namespace AliceHat.Services
{
    public class ContentService
    {
        private const int LowestDefLenWords = 1;
        private const int HighestDefLenWords = 9;
        private const int HighestDefLenChars = 50;
        
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
                var definitions = data
                    .Skip(2)
                    .Select(x => Regex.Replace(x.ToUpperFirst().Trim(), @"\s+", " "))
                    .Where(x => !x.IsNullOrEmpty())
                    .Select(x => (full: x, tokens: Regex.Split(x, @"[\s,\.\-]+")))
                    .Where(x => ScoreDefinition(x) < 3)
                    .OrderBy(ScoreDefinition)
                    .ToArray();
                
                if (definitions.Length == 0) continue;

                // check some additional conditions
                var score = ScoreDefinition(definitions[0]);
                if (score > 0)
                {
                    continue; // TODO
                }

                // write new data
                var wordData = new WordData
                {
                    Word = word,
                    Complexity = complexity,
                    Definitions = definitions.Select(x => x.full).ToArray()
                };
                
                if (!_words.ContainsKey(complexity)) _words.Add(complexity, new List<WordData>());
                _words[complexity].Add(wordData);
            }
            
            _logger.LogInformation($"Done. Words loaded: {_words.Values.Sum(l => l.Count)}");
        }

        private static int ScoreDefinition((string, string[]) _)
        {
            var (full, tokens) = _;

            return tokens.Length switch
            {
                <= LowestDefLenWords => 2,
                >= HighestDefLenWords => full.Length >= HighestDefLenChars ? 3 : 1,
                _ => 0
            };
        }
    }
}