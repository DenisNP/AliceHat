using AliceHat.Models.Abstract;

namespace AliceHat.Models.Alice
{
    public class AliceSoundEngine : ISoundEngine
    {
        public string GetNextWordSound()
        {
            return "[audio|dialogs-upload/008dafcd-99bc-4fd1-9561-4686c375eec6/1c5d73a2-0ec2-420e-8745-66ffc77a6ae2.opus]";
        }

        public string GetLetterPronounce(string letter, string letterTts)
        {
            return $"[screen|{letter}][voice|{letterTts}]";
        }

        public string GetPause(int pause)
        {
            return $"[p|{pause}]";
        }
    }
}