using AliceHat.Models.Alice.Abstract;

namespace AliceHat.Models
{
    public class SessionState : ICloneable<SessionState>
    {
        
        
        public SessionState Clone()
        {
            return new ();
        }
    }
}