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

            var max = Int32.Parse(jObj["response"]["total_count"].ToString());

            var index = Math.Min(max - 1, Int32.Parse(settings.index));

            try
            {
                var name = jObj["response"]["games"][index]["name"].ToString();
                appId = jObj["response"]["games"][index]["appid"].ToString();

                var imgId = jObj["response"]["games"][index]["img_icon_url"].ToString();

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

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion
    }
}