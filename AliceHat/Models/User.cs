using AliceHat.Models.Abstract;

namespace AliceHat.Models
{
    public class User : IIdentity
    {
        public string Id { get; set; }
    }
}