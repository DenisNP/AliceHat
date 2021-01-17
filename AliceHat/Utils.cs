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
            var rng = seed == null ? new Random() : new Random(seed.Value);
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
    }
}