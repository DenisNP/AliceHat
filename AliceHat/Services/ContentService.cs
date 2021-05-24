using System;
using System.Collections.Generic;
using System.Linq;
using AliceHat.Models;
using AliceHat.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace AliceHat.Services
{
    public class ContentService
    {
        private const int AllowedOperations = 1000;
        
        private readonly ILogger<ContentService> _logger;
        private readonly IDbService _dbService;
        private readonly Dictionary<Complexity, List<WordData>> _words = new();
        
        public ContentService(ILogger<ContentService> logger, IDbService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        public void LoadWords()
        {
            _logger.LogInformation("Loading words");

            List<WordData> allWords = _dbService.Collection<WordData>()
                .Where(w => w.Status == WordStatus.Ready)
                .ToList();
            
            foreach (WordData wordData in allWords)
            {
                if (!_words.ContainsKey(wordData.Complexity))
                    _words.Add(wordData.Complexity, new List<WordData>());
                
                _words[wordData.Complexity].Add(wordData);
            }

            _logger.LogInformation($"Done. Words loaded: {_words.Values.Sum(l => l.Count)}");
        }
        
        public List<WordData> GetByComplexity(int wordsCount, Complexity complexity, List<string> excludeIds = null)
        {
            List<WordData> wordsAvailable = _words[complexity];
            if (wordsAvailable.Count < wordsCount)
                throw new ArgumentException("There are not enough words in storage");

            var selectedIndexes = new HashSet<int>();
            var selectedWords = new List<WordData>();
            int operationsLeft = AllowedOperations;

            // check and add new index to pool
            void CheckAddIndex(int i)
            {
                // check if index was already selected
                if (selectedIndexes.Contains(i)) return;
                
                // check if word is excluded
                WordData w = wordsAvailable[i];
                if (excludeIds != null && excludeIds.Contains(w.Id)) return;

                // add word to output list
                selectedWords.Add(w);
                selectedIndexes.Add(i);
            }

            // generate random indexes until need count selected
            var random = new Random();
            while (operationsLeft-- > 0 && selectedWords.Count < wordsCount)
            {
                // check if index was already selected
                int randomIdx = random.Next(0, wordsAvailable.Count);
                CheckAddIndex(randomIdx);
            }
            
            // check if operations are over
            var idx = 0;
            while (selectedWords.Count < wordsCount && idx < wordsAvailable.Count)
            {
                CheckAddIndex(idx++);
            }
            
            // check if pool is still not full enough
            if (selectedIndexes.Count < wordsCount)
                throw new ArgumentException("There are not enough words in storage consider exclusions");
            
            return selectedWords;
        }
    }
}