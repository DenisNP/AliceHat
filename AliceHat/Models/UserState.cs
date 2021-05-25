using System;
using System.Collections.Generic;

namespace AliceHat.Models
{
    public class UserState
    {
        public DateTime LastEnter { get; set; }
        public List<string> WordIdsGot { get; set; } = new();
        public int TotalScore { get; set; }
    }
}