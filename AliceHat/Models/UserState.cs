using AliceHat.Models.Alice.Abstract;

namespace AliceHat.Models
{
    public class UserState : ICloneable<UserState>
    {
        public UserState Clone()
        {
            return new();
        }
    }
}