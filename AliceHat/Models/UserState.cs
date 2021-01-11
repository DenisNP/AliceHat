using System;
using AliceHat.Models.Alice.Abstract;

namespace AliceHat.Models
{
    public class UserState : ICloneable<UserState>
    {
        public DateTime LastOperation { get; set; } = DateTime.MinValue;

        public bool IsOld()
        {
            return DateTime.UtcNow - LastOperation > TimeSpan.FromDays(14);
        }

        public UserState Clone()
        {
            return new()
            {
                LastOperation = LastOperation,
            };
        }
    }
}