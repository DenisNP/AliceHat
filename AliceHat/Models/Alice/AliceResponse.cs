using AliceHat.Models.Alice.Abstract;

namespace AliceHat.Models.Alice
{
    public class AliceResponse : AliceResponseBase<UserState, SessionState>
    {
        public AliceResponse(
            AliceRequest request,
            SessionState sessionState = default,
            UserState userState = default
        ) : base(request, sessionState, userState) { }
    }
}