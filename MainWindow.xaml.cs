using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DiscordTokenDecrypter;

namespace DisRipper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HttpHandler httpHandler;
        private readonly EmoteWindow emoteWindow = new();

        private bool bIsTokenSet;
        private bool bIsEmoteWindowShown;
        private readonly Structs.Discord discord;
        private ObservableCollection<Structs.GuildInfo>? GuildList;
        private DatabaseHandler databaseHandler = new();
        private readonly Utility utility = new Utility();

        public MainWindow()
        {
            InitializeComponent();

            Task.Run(Utility.db.CreateConfigTable).Wait();
            httpHandler = new HttpHandler();
            discord = new Structs.Discord();
            GetAllEmotesButton.IsEnabled = false;
            GetGuildButton.IsEnabled = false;

            if (Config.bIsTokenRetrievalEnabled)
            {
                TokenBox.Password = new DTD().GetDiscordToken();
                AutoTokenCheckBox.IsChecked = true;
                SetToken();
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TokenButton_Click(object sender, RoutedEventArgs e)
        {
            SetToken();
        }

        private void SetToken()
        {
            if (!string.IsNullOrEmpty(TokenBox.Password))
            {
                bIsTokenSet = httpHandler.SetToken(TokenBox.Password);
            }

            if (bIsTokenSet)
            {
                ResponseBox.Text = $" {httpHandler.GetLastStatusCode()}: Successfully connected to Discord!, TokenSource confirmed! \n {JObject.Parse(httpHandler.GetTestResponse())}";
                Thread.Sleep(1);
                GetGuilds();
                return;
            }

            ResponseBox.Text = $"Discord connection failed: {httpHandler.GetLastStatusCode()}";
        }
        private JObject? GetGuild(ulong id) { return ParseJObjectResponse($"{discord.Guild}{id}"); }



        private async Task GetGuilds()
        {
            GuildList = new();
            GuildGrid.ItemsSource = GuildList;
            Structs.GuildInfo guildInfo = new();

            JArray? response = ParseJArrayResponse($"{discord.Guilds}".ToString());
            if (response != null)
            {
                foreach (var item in response)
                {
                    if (Utility.IsTokenCanceled())
                        return;

                    if (item != null && item["id"] != null && item["name"] != null)
                        GuildList.Add(guildInfo.Create((ulong)item["id"], (string)item["name"]));
                }

                ResponseBox.Clear();

                foreach (var item in GuildList)
                {
                    if (Utility.IsTokenCanceled())
                        return;

                    ResponseBox.Text = $"{ResponseBox.Text}{item.Get}\n";
                }

                if(GuildList != null)
                {
                    GetGuildButton.IsEnabled = true;
                    GetAllEmotesButton.IsEnabled = true;
                }
            }
        }

        private async Task GetEmotes()
        {
            emoteWindow.ClearEmotesAndStickers();

            if (GuildList == null) { MessageBox.Show("GuildList was null!"); return; }

            foreach (Structs.GuildInfo item in GuildList)
            {
                if(Utility.IsTokenCanceled())
                    return;

                await IterateEmotes(GetGuild(item.Id));
                emoteWindow.IncreaseGuildProgress();
                await Task.Delay(250);
            }

            emoteWindow.Reset();
        }

        private async Task Continue()
        {
            Dictionary<ulong, List<ulong>> EmoteCollection = new Dictionary<ulong, List<ulong>>(); // Use GuildID as key for dictionary, store EmoteIDs in list of respective GuildID
            List<string> Tables = await Utility.db.GetTables();
            foreach (string Table in Tables)
            {
                if (Utility.IsTokenCanceled())
                    return;

                foreach (Structs.Img Img in Utility.db.ReadEmotes(Table).Result)
                {
                    if (Utility.IsTokenCanceled())
                        return;

                    //EmoteIds.Add(Img.EmoteId);
                    if (!EmoteCollection.TryAdd(Img.GuildId, new List<ulong>() { Img.EmoteId }))
                    {
                        EmoteCollection[Img.GuildId].Add(Img.EmoteId);
                    }
                }
            }

            /*if (Guilds == null)
                throw new NullReferenceException("EmoteWindow->Continue(): Guilds is null.");

            foreach (Structs.GuildInfo guild in Guilds)
            {
                if (Utility.TokenSource.IsCancellationRequested)
                    return;

                JObject JGuild = httpHandler?.GetGuild(guild.Id)?.Result;
                if (JGuild == null)
                    throw new NullReferenceException("EmoteWindow->Continue(): JGuild is null.");

                foreach(JToken Emote in JGuild["emotes"])
                {
                    if (Utility.TokenSource.IsCancellationRequested)
                        return;

                    if (!EmoteIds.Contains((ulong)Emote["id"]))
                    {
                        MemoryStream? ms = await httpHandler?.SendRequest((ulong)Emote["id"], (string)Emote[""], false)?.Content.ReadAsStreamAsync() as MemoryStream;
                        ImageList.Add(new Structs.Img().Create(guild.Id, guild.Name, (ulong)Emote["id"], (string)Emote["name"], utility.GetExtension((bool)Emote["animated"]), false, ms));
                    }
                }
            }*/
        }

        private async Task GetEmotes(ulong id)
        {
            emoteWindow.ClearEmotesAndStickers();
            await IterateEmotes(GetGuild(id));
        }

        private async Task IterateEmotes(JObject? Guild)
        {
            if (Guild == null)
                return;

            emoteWindow.SetEmoteCount(Guild["emojis"].ToList().Count+Guild["stickers"].ToList().Count);
            emoteWindow.SetCurrentGuild((ulong)Guild["id"], (string)Guild["name"]);
            MemoryStream? ms;
            string Ext = string.Empty;
            await Continue();

            foreach (JToken e in Guild["emojis"])
            {
                if (Utility.IsTokenCanceled())
                    return;

                Ext = utility.GetExtension((bool)e["animated"]);
                HttpResponseMessage? response = httpHandler?.SendRequest((ulong)e["id"], Ext,false);

                if (response?.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    using (FileStream fs = File.OpenRead("Error.bmp"))
                    {
                        ms = new MemoryStream((byte)fs.Length);
                        fs.CopyTo(ms);
                    }
                }
                else
                {
                    MemoryStream ResponseStream = response.Content?.ReadAsStreamAsync().Result as MemoryStream;
                    ms = new MemoryStream(0);
                    ms.SetLength(ResponseStream.Length);
                    ResponseStream.CopyTo(ms);
                }

                await emoteWindow.AddImage((ulong)Guild["id"], NamingUtility.ReplaceInvalidFilename(Guild["name"].ToString().Replace(":", "").Replace(",", "").Replace(".", ""), "_"), (ulong)e["id"], NamingUtility.ReplaceInvalidFilename($"{e["name"].ToString().Replace(":", "").Replace(",", "").Replace(".", "").Replace(" ", "")}", "_"), Ext, false, ms as MemoryStream);

                emoteWindow.IncreaseEmoteProgress();
                await Task.Delay(1);
            }

            foreach(JToken s in Guild["stickers"])
            {
                if (Utility.IsTokenCanceled())
                    return;

                HttpResponseMessage? response = httpHandler?.SendRequest((ulong)s["id"], ".png", true);

                if (response?.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    using (FileStream fs = File.OpenRead("Error.bmp"))
                    {
                        ms = new MemoryStream((byte)fs.Length);
                        fs.CopyTo(ms);
                    }
                }
                else
                {
                    Ext = utility.GetExtension((int)s["format_type"]);
                    ms = response.Content?.ReadAsStreamAsync().Result as MemoryStream;
                }

                await emoteWindow.AddImage((ulong)Guild["id"], NamingUtility.ReplaceInvalidFilename(Guild["name"].ToString().Replace(":", "").Replace(",", "").Replace(".", ""), "_"), (ulong)s["id"], NamingUtility.ReplaceInvalidFilename($"{s["name"].ToString().Replace(":", "").Replace(",", "").Replace(".", "").Replace(" ", "")}", "_"), Ext, true, ms);
                emoteWindow.IncreaseEmoteProgress();
                await Task.Delay(1500);
            }

            emoteWindow.ResetEmoteCount();
        }

        private JArray? ParseJArrayResponse(string _discord)
        {
            string json = httpHandler?.SendRequest(_discord)?.Content.ReadAsStringAsync().Result ?? string.Empty;
            if (string.IsNullOrEmpty(json))
                return null;

            return JArray.Parse(json);
        }

        private JObject? ParseJObjectResponse(string _discord)
        {
            string json = httpHandler?.SendRequest(_discord)?.Content.ReadAsStringAsync().Result ?? string.Empty;
            if (string.IsNullOrEmpty(json))
                return null;

            return JObject.Parse(json);
        }

        private async void GetAllEmotes_Click(object sender, RoutedEventArgs e)
        {
            await EmoteWindow();
            await Task.Delay(1);
        }

        private async Task EmoteWindow(ulong GuildID = 0)
        {
            if (!bIsEmoteWindowShown) { emoteWindow.Show(); emoteWindow.SetHttpHandler(httpHandler); bIsEmoteWindowShown = true; }

            emoteWindow.BringIntoView();
            emoteWindow.SetGuilds(GuildList);

            if (GuildID == 0)
            {
                await GetEmotes();
            }
            else
            {
                await GetEmotes(GuildID);
            }
        }

        private async void GetGuildButton_Click(object sender, RoutedEventArgs e)
        {
            Structs.GuildInfo guildInfo = new Structs.GuildInfo();
            JObject? Guild = new();
            try
            {
                 Guild = GetGuild(Convert.ToUInt64(GuildID.Text));
            }
            catch (FormatException ex)
            {
                MessageBox.Show("You must click a guild before clicking this button!");
                return;
            }

            if (Guild == null)
                return;

            await EmoteWindow((ulong)Guild["id"]);
        }

        private async void GuildGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Structs.GuildInfo guildInfo = (Structs.GuildInfo)GuildGrid.SelectedItem;
            await EmoteWindow(guildInfo.Id);
        }

        private void GuildGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Structs.GuildInfo gid = (Structs.GuildInfo)GuildGrid.SelectedItem;
            GuildID.Text = gid.Id.ToString();
        }

        private void AutoTokenCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            Config.SetAutomaticTokenRetrieval(AutoTokenCheckBox.IsChecked ?? false);

            if (!bIsTokenSet)
            {
                TokenBox.Password = new DTD().GetDiscordToken();
                SetToken();
            }
        }
    }
}