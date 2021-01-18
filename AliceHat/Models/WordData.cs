using AliceHat.Models.Abstract;

namespace AliceHat.Models
{
    public class WordData : IIdentity
    {
        public string Id { get; private set; }
        
        public string Word { get; private set; }
        
        public Complexity Complexity { get; private set; }

        public WordStatus Status { get; private set; }
        
        public string Definition { get; private set; }
    }

    public enum WordStatus
    {
        Untouched = 0,
        Taken = 1,
        Ready = 2
    }
}