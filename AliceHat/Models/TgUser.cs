using System;
using AliceHat.Models.Abstract;

namespace AliceHat.Models
{
    public class TgUser : IIdentity
    {
        public string Id { get; set; }
        public int WordsProcessed { get; set; }

        public WordData LastWord { get; set; } = null;
        public DateTime LastTimeWord { get; set; }
    }
}