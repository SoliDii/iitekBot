using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using TestBot.Models;

namespace TestBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;


            if (activity == null) return;

            string text = activity.Text;
            var commandResult = CheckAndFormatCommandMessage(text);

            if (commandResult.isCommand)
            {
                switch (commandResult.command)
                {
                    case "xkcd":
                        await context.PostAsync(HandleXkcdCommands(commandResult.text, context));
                        break;
                    case "fingerpori":
                        await context.PostAsync(HandleFingerporiCommands(commandResult.text, context));
                        break;
                    default:
                        break;
                }
            }
            else
            {
                // Calculate something for us to return
                int length = (activity.Text ?? string.Empty).Length;

                // Return our reply to the user
                await context.PostAsync($"You sent {activity.Text} which was {length} characters");
            }

            context.Wait(MessageReceivedAsync);
        }


        public static (bool isCommand, string command, string text) CheckAndFormatCommandMessage(string message)
        {
            bool isCommand = false;
            string command = "";
            string text = "";

            if (message.StartsWith("/"))
            {
                message = message.Substring(1, message.Length - 1);
                var splitted = message.Split(' ');
                if (splitted.Length > 0)
                {
                    command = splitted[0];
                    if (splitted.Length > 1)
                    {
                        text = splitted[1];
                    }
                }
                isCommand = true;


            }
            return (isCommand, command, text);

        }

        public static IMessageActivity HandleFingerporiCommands(string text, IDialogContext context)
        {
            var fingerporiUrls = WebScraper();
            var replyMessage = context.MakeMessage();

            if (text.StartsWith("new"))
            {
                Attachment attachment = new Attachment
                {
                    ContentType = "image/jpg",
                    ContentUrl = fingerporiUrls.imageUrl,
                    Name = "Fingerpori sarjakuva"
                };
                replyMessage.Attachments.Add(attachment);
                replyMessage.Text = String.Format("Helsingin Sanomien viimeisin Fingerpori\r\nUrl: {0}", fingerporiUrls.comicUrl);
            }
            return replyMessage;
        }

        public static IMessageActivity HandleXkcdCommands(string text, IDialogContext context)
        {
            string xkcdUrlStart = "https://xkcd.com/";
            string latestXkcdUrl = xkcdUrlStart + "info.0.json";
            string completeString = "";
            var replyMessage = context.MakeMessage();

            WebClient wc = new WebClient();

            if (text.StartsWith("random"))
            {
                string json = wc.DownloadString(latestXkcdUrl);
                Xkcd deserializedXkcdJson = JsonConvert.DeserializeObject<Xkcd>(json);
                int maxNum = deserializedXkcdJson.Num;
                Random rnd = new Random();
                int rndNum = rnd.Next(1, maxNum);

                completeString = xkcdUrlStart + "/" + rndNum + "/info.0.json";

            }
            if (text.StartsWith("new"))
            {
                completeString += xkcdUrlStart + "info.0.json";
            }

            if (completeString.Length > 0)
            {
                string json = wc.DownloadString(completeString);
                Xkcd deserializedXkcdJson = JsonConvert.DeserializeObject<Xkcd>(json);


                Attachment attachment = new Attachment
                {
                    ContentType = "image/jpg",
                    ContentUrl = deserializedXkcdJson.Img,
                    Name = deserializedXkcdJson.Title,
                };
                replyMessage.Attachments.Add(attachment);
                replyMessage.Text = String.Format("Title: {0} \r\nAlt: {1} \r\nUrl: {2}", deserializedXkcdJson.Title, deserializedXkcdJson.Alt, xkcdUrlStart +
                    deserializedXkcdJson.Num);

            }
            return replyMessage;
        }

        public static (string imageUrl, string comicUrl) WebScraper()
        {
            WebClient client = new WebClient();

            string urlForComicListWebsite = "https://www.hs.fi/fingerpori/";
            string htmlCode = client.DownloadString(urlForComicListWebsite);

            int latestStartIndex = htmlCode.IndexOf("<a href=\"/fingerpori/");

            htmlCode = htmlCode.Substring(latestStartIndex, 1000);


            int urlEndStartIndex = htmlCode.IndexOf("car");
            int urlEndEndIndex = htmlCode.IndexOf("html") + 4;
            string urlEndForLatestComic = htmlCode.Substring(urlEndStartIndex, urlEndEndIndex - urlEndStartIndex);


            int imageUrlStartIndex = htmlCode.IndexOf("data-srcset=\"//hs.mediadelivery.fi/img/") + 13;
            int imageUrlEndIndex = htmlCode.IndexOf(".jpg 1920w") + 4;

            string https = "https:";
            string urlForLatestComicImage = htmlCode.Substring(imageUrlStartIndex, imageUrlEndIndex - imageUrlStartIndex);



            string imageUrl = https + urlForLatestComicImage;
            string UrlForComicWebsite  = urlForComicListWebsite + urlEndForLatestComic;

            client.Dispose();

            return (imageUrl, UrlForComicWebsite);

        }
    }
}
