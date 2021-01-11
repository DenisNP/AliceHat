using System.Collections.Generic;
using System.Linq;

namespace AliceHat.Models.Alice.Abstract
{
    public class AliceEmpty
    {
    }
    
    public class Interfaces
    {
        public AliceEmpty Screen { get; set; }
        public AliceEmpty Payments { get; set; }
        public AliceEmpty AccountLinking { get; set; }
    }

    public class Meta
    {
        public string Locale { get; set; }
        public string Timezone { get; set; }
        public string ClientId { get; set; }
        public Interfaces Interfaces { get; set; }
    }

    public class Markup
    {
        public bool DangerousContext { get; set; }
    }

    public class Intent
    {
        public Dictionary<string, IntentSlot> Slots { get; set; }
    }

    public class IntentSlot
    {
        public string Type { get; set; }
        public object Value { get; set; }
    }

    public class TokensRange
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class Entity
    {
        public TokensRange Tokens { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
    }

    public class Nlu
    {
        public List<string> Tokens { get; set; }
        public List<Entity> Entities { get; set; }
        public Dictionary<string, Intent> Intents { get; set; }
    }

    public class Request
    {
        public string Command { get; set; }
        public string OriginalUtterance { get; set; }
        public string Type { get; set; }
        public Markup Markup { get; set; }
        public Dictionary<string, string> Payload { get; set; }
        public Nlu Nlu { get; set; }
    }

    public class User
    {
        public string UserId { get; set; }
    }

    public class Device
    {
        public string DeviceId { get; set; }
    }

    public class Session
    {
        public bool New { get; set; }
        public int MessageId { get; set; }
        public string SessionId { get; set; }
        public string SkillId { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public Device Device { get; set; }
    }

    public class State<TUserState, TSessionState> where TUserState : new() where TSessionState : new()
    {
        public TUserState User { get; set; } = new TUserState();
        public TSessionState Session { get; set; } = new TSessionState();
    }

    public class AliceRequestBase<TUserState, TSessionState> where TUserState : new() where TSessionState : new()
    {
        public Meta Meta { get; set; }
        public AliceEmpty AccountLinkingCompleteEvent { get; set; }
        public Request Request { get; set; }
        public Session Session { get; set; }
        public string Version { get; set; }
        public State<TUserState, TSessionState> State { get; set; } = new State<TUserState, TSessionState>();

        public bool HasScreen()
        {
            return Meta?.Interfaces?.Screen != null;
        }
        
        public bool IsAccountLinking()
        {
            return AccountLinkingCompleteEvent != null;
        }

        public bool IsPing()
        {
            if (Request == null)
            {
                return false;
            }

            return Request.Command.ToLower() == "ping";
        }
        
        public bool IsEnter()
        {
            return Request != null && string.IsNullOrEmpty(Request.Command);
        }

        public Intent GetIntent(string name)
        {
            if (Request?.Nlu?.Intents == null)
            {
                return null;
            }
            
            if (!Request.Nlu.Intents.ContainsKey(name))
            {
                return null;
            }

            return Request.Nlu.Intents[name];
        }
        
        public bool HasIntent(string name, bool withSlots = false)
        {
            return withSlots ? GetIntent(name)?.Slots != null : GetIntent(name) != null;
        }

        public bool HasSlot(string intentName, string slotName)
        {
            return !string.IsNullOrEmpty(GetSlot(intentName, slotName));
        }

        public bool HasOneOfSlots(string intentName, params string[] slotNames)
        {
            return slotNames.Any(s => HasSlot(intentName, s));
        }

        public bool HasAllSlots(string intentName, params string[] slotNames)
        {
            return slotNames.All(s => HasSlot(intentName, s));
        }

        public string GetSlot(string intentName, string slotName)
        {
            var intent = GetIntent(intentName);
            if (intent?.Slots == null || !intent.Slots.ContainsKey(slotName))
            {
                return "";
            }

            return intent.Slots[slotName].Value.ToString();
        }
    }
}