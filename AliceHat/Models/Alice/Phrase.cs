using System.Linq;
using System.Text.RegularExpressions;
using AliceHat.Models.Alice.Abstract;

namespace AliceHat.Models.Alice
{
    public class Phrase
    {
        private static readonly Regex Pattern = new("\\[[a-z\\+]{1,10}\\|?[^\\[\\]]*\\]");
        private static readonly Regex HasScreenPattern = new("\\[hasScreen\\|([^\\|]*)\\|([^\\|]*)\\]");

        public string Text { get; }
        public string[] Buttons { get; }
        public string[] Images { get; }

        private Phrase() { }

        public Phrase(string text, string[] buttons = null, string[] images = null)
        {
            Text = text;
            Buttons = buttons;
            Images = images;
        }

        public AliceResponse Generate(AliceRequest request, UserState newUserState = null, SessionState newSessionState = null)
        {
            var (text, tts) = GetTextTtsPair(request.HasScreen());
            var response = new AliceResponse(request, newSessionState, newUserState)
            {
                Response =
                {
                    Text = text,
                    Tts = tts,
                    Buttons = Buttons?.Select(b =>
                    {
                        var bData = b.Split("!");
                        return bData.Length == 1 ? new Button(b) : new Button(bData[0]) {Url = bData[1], Hide = false};
                    }).ToList()
                }
            };

            if (Images != null && Images.Length > 0)
            {
                var description = text
                    .Replace("\\n", " ")
                    .Replace("  ", " ")
                    .SafeSubstring(256);
                
                if (Images.Length == 1)
                {
                    var imgData = Images[0].Split("!");
                    response.Response.Card = imgData.Length > 1 
                        ? new SingleCard(imgData[0], new Button(imgData[1])) {Description = description}
                        : new SingleCard(imgData[0]) {Description = description};
                }
                else
                {
                    response.Response.Card = new GalleryCard(Images.Select(i =>
                    {
                        var iData = i.Split("!");
                        return iData.Length > 1
                            ? new GalleryItem(iData[0], iData[1])
                            : new GalleryItem(i);
                    }));
                }
            }

            return response;
        }

        public (string text, string tts) GetTextTtsPair(bool hasScreen)
        {
            var t = Text;
            var text = "";
            var tts = "";
            
            var match = Pattern.Match(t);
			
			// check if no found
            if (!match.Success)
            {
                return (Text, Text.Replace("\\n", " sil <[100]> ").Replace("\n", " sil <[100]> "));
            }
            
            // if found
            while (match.Success)
            {
                // remove beginning of string
                if (match.Index > 0)
                {
                    var before = t.Substring(0, match.Index);
                    text += before;
                    tts += before;
                }

                // remove match from string
                t = t.Substring(match.Index + match.Length);
                
                // process match
                var pars = match.Value.Substring(1, match.Value.Length - 2).Split("|");
                var type = pars[0];

                switch (type)
                {
                    case "+": // accent
                        tts += "+";
                        break;
                    case "p": // pause
                        var delay = pars.Length > 1 ? int.Parse(pars[1]) : 0;
                        if (delay > 0)
                        {
                            tts += $" sil <[{delay}]> ";
                        }
                        else
                        {
                            tts += " - ";
                        }
                        break;
                    case "audio": // audio resource
                        tts += $"<speaker audio=\"{pars[1]}\">";
                        break;
                    case "screen": // only on screen
                        text += pars[1];
                        break;
                    case "voice": // only in voice
                        tts += pars[1];
                        break;
                    case "hasScreen":
                        if (hasScreen)
                        {
                            text += pars[1];
                            tts += pars[1];
                        }
                        else
                        {
                            text += pars[2];
                            tts += pars[2];
                        }
                        break;
                }
                
                // find pattern again
                match = Pattern.Match(t);
            }

            text += t;
            tts += t;
            
            // additional match hasScreen because of nesting
            text = HasScreenPattern.Replace(text, hasScreen ? "$1" : "$2");
            tts = HasScreenPattern.Replace(tts, hasScreen ? "$1" : "$2");
            
            // replace \n by small pauses
            tts = tts.Replace("\\n", " sil <[100]> ").Replace("\n", " sil <[100]> ");
            
            return (text, tts);
        }
        
        public static Phrase operator +(Phrase a, Phrase b)
        {
            var sumText = a.Text + b.Text;
            var sumButtons = ConcatArrays(a.Buttons, b.Buttons);
            var sumImages = ConcatArrays(a.Images, b.Images);
            
            return new Phrase(sumText, sumButtons, sumImages);
        }
        
        private static T[] ConcatArrays<T>(T[] a, T[] b)
        {
            T[] arr = null;
            if (a != null && b  != null)
                arr = a.Concat(b).ToArray();
            else if (a != null)
                arr = a.ToArray();
            else if (b != null) 
                arr = b.ToArray();

            return arr;
        }
    }
}