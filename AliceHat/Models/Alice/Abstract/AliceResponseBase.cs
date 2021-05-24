using System.Collections.Generic;
using System.Linq;

namespace AliceHat.Models.Alice.Abstract
{
    public class Button
    {
        public string Title { get; set; }
        public Dictionary<string, string> Payload { get; set; }
        public string Url { get; set; }
        public bool Hide { get; set; } = true;

        public Button() { }

        public Button(string b)
        {
            Title = b;
        }
    }

    public class Response
    {
        public string Text { get; set; }
        public string Tts { get; set; }
        public List<Button> Buttons { get; set; }
        public ICard Card { get; set; }
        public bool EndSession { get; set; }
    }

    public class ImageMeta
    {
        public string ImageId { get; }
        public Button Button { get;  }

        public ImageMeta(string imageId, Button button = null)
        {
            ImageId = imageId;
            Button = button;
        }
    }

    public class SingleCard : ImageMeta, ICard
    {
        public string Type { get; } = "BigImage";
        public string Description { get; set; }

        public SingleCard(string imageId, Button button = null) : base(imageId, button) { }
    }

    public class ItemsListCard : ICard
    {
        public string Type { get; } = "ItemsList";
        public ImageMeta[] Items { get; }
        public ItemsListFooter Footer { get; set; }

        public ItemsListCard(IEnumerable<ImageMeta> items)
        {
            Items = items.ToArray();
        }
    }

    public class ItemsListFooter
    {
        public string Text { get; }

        public ItemsListFooter(string text)
        {
            Text = text;
        }
    }

    public class GalleryCard : ICard
    {
        public string Type { get; } = "ImageGallery";
        public GalleryItem[] Items { get; }

        public GalleryCard(IEnumerable<GalleryItem> items)
        {
            Items = items.ToArray();
        }
    }

    public class GalleryItem
    {
        public string ImageId { get; }
        public string Title { get; }

        public GalleryItem(string imageId, string title = "")
        {
            ImageId = imageId;
            Title = title;
        }
    }

    public interface ICard
    {
        string Type { get; }
    }

    public class AliceResponseBase<TUserState, TSessionState>
        where TUserState : class, new()
        where TSessionState : class, new()
    {
        public AliceEmpty StartAccountLinking { get; set; }
        public Response Response { get; set; } = new Response();
        public Session Session { get; set; }
        public string Version { get; set; }

        public TUserState UserStateUpdate { get; set; }
        public TSessionState SessionState { get; set; }

        public AliceResponseBase(
            AliceRequestBase<TUserState, TSessionState> request,
            TSessionState sessionState = default,
            TUserState userState = default
        )
        {
            Session = request.Session;
            Version = request.Version;
            SessionState = sessionState ?? request.State.Session;
            UserStateUpdate = userState ?? request.State.User;
        }
        
        public AliceResponseBase<TUserState, TSessionState> ToAuthorizationResponse()
        {
            Response = null;
            StartAccountLinking = new AliceEmpty();
            return this;
        }

        public AliceResponseBase<TUserState, TSessionState> ToPong()
        {
            Response = new Response
            {
                Text = "pong"
            };
            return this;
        }
    }
}