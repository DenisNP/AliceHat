using AliceHat.Models.Abstract;

namespace AliceHat.Models
{
    public class WordData : IIdentity
    {
        public string Id { get; set; }
        public string Word { get; set; }
        public Complexity Complexity { get; set; }
        public WordStatus Status { get; set; }
        public string Definition { get; set; }
        public string[] Mispronounce { get; set; }
    }

    public enum WordStatus
    {
        Untouched = 0,
        Taken = 1,
        Ready = 2
    }
}