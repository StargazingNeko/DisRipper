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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DisEmoteRipper
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

        public MainWindow()
        {
            InitializeComponent();
            httpHandler = new HttpHandler();
            discord = new Structs.Discord();
            GetAllEmotesButton.IsEnabled = false;
            GetGuildButton.IsEnabled = false;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TokenButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TokenBox.Password))
            {
                bIsTokenSet = httpHandler.SetToken(TokenBox.Password);
            }

            if (bIsTokenSet)
            {
                ResponseBox.Text = $" {httpHandler.GetLastStatusCode()}: Successfully connected to Discord!, Token confirmed! \n {JObject.Parse(httpHandler.GetTestResponse())}";
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
                    if (item != null && item["id"] != null && item["name"] != null)
                        GuildList.Add(guildInfo.Add((ulong)item["id"], (string)item["name"]));
                }

                ResponseBox.Clear();

                foreach (var item in GuildList)
                {
                    ResponseBox.Text = $"{ResponseBox.Text}{item.Get}\n";
                    databaseHandler.Write(item.ID, item.Name, false);

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

            foreach (var item in GuildList)
            {
                await IterateEmotes(GetGuild(item.ID));
                emoteWindow.IncreaseGuildProgress();
                await Task.Delay(250);
            }

            emoteWindow.Reset();
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
            foreach (JToken e in Guild["emojis"])
            {
                await emoteWindow.AddEmote((ulong)e["id"], NamingUtility.ReplaceInvalidFilename($"{e["name"].ToString().Replace(":", "").Replace(",", "").Replace(".", "").Replace(" ", "")} {e["animated"]}", "_"));
                emoteWindow.IncreaseEmoteProgress();
            }

            foreach(JToken s in Guild["stickers"])
            {
                bool bIsAnimated = false;
                if ((int)s["format_type"] == 2)
                    bIsAnimated = true;

                await emoteWindow.AddSticker((ulong)s["id"], NamingUtility.ReplaceInvalidFilename($"{NamingUtility.CleanName(s["name"].ToString())} {bIsAnimated}", "_"));
                emoteWindow.IncreaseEmoteProgress();
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

            emoteWindow.BeginDrawing();
        }

        private void GetGuildButton_Click(object sender, RoutedEventArgs e) => GetGuild(Convert.ToUInt64(GuildID));

        private async void GuildGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Structs.GuildInfo guildInfo = (Structs.GuildInfo)GuildGrid.SelectedItem;
            await EmoteWindow(guildInfo.ID);
        }

        private void GuildGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Structs.GuildInfo gid = (Structs.GuildInfo)GuildGrid.SelectedItem;
            GuildID.Text = gid.ID.ToString();
        }

    }
}