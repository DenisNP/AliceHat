using System;
using System.Collections.Generic;
using System.Linq;

namespace AliceHat
{
    public static class Utils
    {
        public static string SafeSubstring(this string s, int len)
        {
            return s.Length <= len ? s : s.Substring(0, len);
        }

        public static string ToUpperFirst(this string s)
        {
            if (s.IsNullOrEmpty()) return s;
            if (s.Length == 1) return s.ToUpper();

            return s[0].ToString().ToUpper() + s.Substring(1);
        }

        public static string ToLowerFirst(this string s)
        {
            if (s.IsNullOrEmpty()) return s;
            if (s.Length == 1) return s.ToLower();

            return s[0].ToString().ToLower() + s.Substring(1);
        }

        public static T PickRandom<T>(this IList<T> list)
        {
            var rng = new Random();
            return list[rng.Next(list.Count)];
        }
        
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }
        
        public static string Join(this IEnumerable<string> s, string separator)
        {
            return string.Join(separator, s);
        }
        
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list, int? seed = null)  
        {
            Random rng = seed == null ? new Random() : new Random(seed.Value);
            var buffer = list.ToList();
            for (var i = 0; i < buffer.Count; i++)
            {
                var j = rng.Next(i, buffer.Count);
                yield return buffer[j];
                buffer[j] = buffer[i];
            }
        }

        public static string GetNumericPhrase(int num, string one, string few, string many)
        {
            num = num < 0 ? 0 : num;
            string postfix;

            if (num < 10)
            {
                if (num == 1) postfix = one;
                else if (num > 1 && num < 5) postfix = few;
                else postfix = many;
            }
            else if (num <= 20)
            {
                postfix = many;
            }
            else if (num <= 99)
            {
                var lastOne = num - ((int)Math.Floor((double)num / 10)) * 10;
                postfix = GetNumericPhrase(lastOne, one, few, many);
            }
            else
            {
                var lastTwo = num - ((int)Math.Floor((double)num / 100)) * 100;
                postfix = GetNumericPhrase(lastTwo, one, few, many);
            }
            return postfix;
        }

        public static string ToPhrase(this int num, string one, string few, string many)
        {
            return num + " " + GetNumericPhrase(num, one, few, many);
        }
        
        public static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
                return 0;
            if (string.IsNullOrEmpty(a))
                return b.Length;
            if (string.IsNullOrEmpty(b))
                return a.Length;
            
            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (var i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (var j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (var i = 1; i <= lengthA; i++)
            for (var j = 1; j <= lengthB; j++)
            {
                int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                distances[i, j] = Math.Min
                (
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost
                );
            }
            return distances[lengthA, lengthB];
        }

        public static double LevenshteinMatchRatio(string a, string b)
        {
            int maxLen = Math.Max(a.Length, b.Length);
            if (maxLen == 0)
                return 0.0;

            int levDist = LevenshteinDistance(a, b);
            return 1.0 - (double) levDist / maxLen;
        }
    }
}