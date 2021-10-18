using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Net.Http;
using System.Net;

namespace Steam_Launcher
{
    [PluginActionId("steamlauncher.recentlyplayed")]
    public class PluginAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.steamId = String.Empty;
                instance.index = String.Empty;
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "steamid")]
            public string steamId { get; set; }

            [JsonProperty(PropertyName = "index")]
            public string index { get; set; }

            [JsonProperty(PropertyName = "apitoken")]
            public string apiToken { get; set; }
        }

        private const string STEAM_API_KEY_ENV = "STEAMLAUNCHER_STEAM_API_KEY";
        private const string STEAM_LAUNCH_URL = @"steam://run/{0}";
        private const string STEAM_APP_ICON_URL = @"https://media.steampowered.com/steamcommunity/public/images/apps/{0}/{1}.jpg";
        private string title = "";
        private string appId = "";
        private Bitmap appImage;


        #region Private Members

        private PluginSettings settings;

        #endregion
        public PluginAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            refreshGame();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"App Launched");
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            System.Diagnostics.Process.Start(String.Format(STEAM_LAUNCH_URL, appId));
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick() {

            if (appImage != null)
            {
                await Connection.SetImageAsync(appImage);
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
            refreshGame();
        }

        private async void refreshGame()
        {
            var client = new HttpClient();
            var response = await client.GetAsync(
                String.Format(
                    "http://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v0001/?key={0}&steamid={1}&format=json",
                    settings.apiToken,
                    settings.steamId
                )
            );

            var content = await response.Content.ReadAsStringAsync();
            Logger.Instance.LogMessage(TracingLevel.ERROR, content);
            var jObj = JObject.Parse(content);

            try
            {
                var name = jObj["response"]["games"][Int32.Parse(settings.index)]["name"].ToString();
                appId = jObj["response"]["games"][Int32.Parse(settings.index)]["appid"].ToString();

                var imgId = jObj["response"]["games"][Int32.Parse(settings.index)]["img_icon_url"].ToString();

                appImage = FetchImage(String.Format(STEAM_APP_ICON_URL, appId, imgId));
                //appImage = SetImageFit(img);

                await Connection.SetTitleAsync(name);
                await Connection.SetImageAsync(appImage);
            }
            catch (FormatException)
            {
              //  Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to fetch image: {imageUrl} {ex}");
            }
        }

        private Bitmap FetchImage(string imageUrl)
        {
            try
            {
                if (String.IsNullOrEmpty(imageUrl))
                {
                    return null;
                }

                WebClient client = new WebClient();
                Stream stream = client.OpenRead(imageUrl);
                Bitmap image = new Bitmap(stream);
                return image;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to fetch image: {imageUrl} {ex}");
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to fetch image: {imageUrl} {ex}");

            }
            return null;
        }

        private Bitmap SetImageFit(Bitmap img)
        {
            if (img == null)
            {
                return null;
            }

            Image tmpImage;
            var newImage = Tools.GenerateGenericKeyImage(out Graphics graphics);
            if (img.Width > img.Height)
            {
                tmpImage = (Bitmap)ResizeImageByHeight(img, newImage.Height);
            }
            else
            {
                tmpImage = (Bitmap)ResizeImageByWidth(img, newImage.Width);
            }


            int startX;
            startX = (tmpImage.Width / 2) - (newImage.Width / 2);
            if (startX < 0)
            {
                startX = 0;
            }
            graphics.DrawImage(tmpImage, new Rectangle(0, 0, newImage.Width, newImage.Height), new Rectangle(startX, 0, newImage.Width, newImage.Height), GraphicsUnit.Pixel);

            return newImage;
        }

        private Image ResizeImageByHeight(Image img, int newHeight)
        {
            if (img == null)
            {
                return null;
            }

            int originalWidth = img.Width;
            int originalHeight = img.Height;

            // Figure out the ratio
            double ratio = (double)newHeight / (double)originalHeight;
            int newWidth = (int)(originalWidth * ratio);
            return ResizeImage(img, newHeight, newWidth);
        }

        private Image ResizeImageByWidth(Image img, int newWidth)
        {
            if (img == null)
            {
                return null;
            }

            int originalWidth = img.Width;
            int originalHeight = img.Height;

            // Figure out the ratio
            double ratio = (double)newWidth / (double)originalWidth;
            int newHeight = (int)(originalHeight * ratio);
            return ResizeImage(img, newHeight, newWidth);
        }

        private Image ResizeImage(Image original, int newHeight, int newWidth)
        {
            Image canvas = new Bitmap(newWidth, newHeight);
            Graphics graphic = Graphics.FromImage(canvas);

            //graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            //graphic.SmoothingMode = SmoothingMode.HighQuality;
            //graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
            //graphic.CompositingQuality = CompositingQuality.HighQuality;

            graphic.Clear(Color.Black); // Padding
            graphic.DrawImage(original, 0, 0, newWidth, newHeight);

            return canvas;
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion
    }
}