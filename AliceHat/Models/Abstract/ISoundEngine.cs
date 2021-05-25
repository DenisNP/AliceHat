namespace AliceHat.Models.Abstract
{
    public interface ISoundEngine
    {
        public string GetNextWordSound();
        public string GetLetterPronounce(string letter, string letterTts);
        public string GetPause(int pause);
    }
}