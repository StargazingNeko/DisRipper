using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DisRipper
{
    /// <summary>
    /// Interaction logic for EmoteWindow.xaml
    /// </summary>
    public partial class EmoteWindow : Window
    {

        #region Global Variables
        private HttpHandler? httpHandler;
        private readonly ObservableCollection<KeyValuePair<ulong, string>> Emotes = new();
        private readonly ObservableCollection<KeyValuePair<ulong, string>> Stickers = new();
        private ObservableCollection<Structs.GuildInfo>? Guilds;
        private ObservableCollection<Structs.Img> ImageList = new();
        private BackgroundWorker EmoteWorker;
        private BackgroundWorker GuildWorker;
        private int EmoteProgressCount = 0;
        private int GuildProgressCount = 0;
        private Structs.GuildInfo CurrentGuild;
        private readonly DatabaseHandler db = new();
        private readonly Utility utility = new Utility();
        #endregion

        public EmoteWindow()
        {
            InitializeComponent();
            EmoteWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            EmoteWorker.ProgressChanged += EmoteWorker_ProgressChanged;

            GuildWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            GuildWorker.ProgressChanged += GuildWorker_ProgressChanged;

            ImageList.CollectionChanged += ImageList_CollectionChanged;

        }


        private async void ImageList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Add emote to database here
            if (e.NewItems == null)
                return;

            System.Windows.Controls.Image image = new() { Height = 100, Width = 100 };
            foreach(Structs.Img img in e.NewItems)
            {
                BitmapFrame bitmap = BitmapFrame.Create(img.Stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                //if (img.Stream == null)
                //{
                //    MessageBox.Show("Null Stream!");
                //    return;
                //}

                image.Source = bitmap;
                MemoryStream ms = img.Stream;

                if(img.Extension == ".gif")
                {
                    ms.Position = 0;
                    System.Drawing.Image.FromStream(img.Stream).Save(ms = new MemoryStream(), System.Drawing.Imaging.ImageFormat.Gif);
                }

                await db.CreateTable(img.GuildName);
                await db.WriteEmotes(img.GuildId, img.GuildName, img.EmoteId, img.EmoteName, img.Extension, img.IsSticker, ms);

                DrawEmote(image);
            }

        }

        public void SetHttpHandler(HttpHandler handler) => httpHandler = handler;

        public void SetGuilds(ObservableCollection<Structs.GuildInfo> guilds)
        {
            Guilds = guilds;
            GuildProgress.Maximum = Guilds.Count;
            GuildLabel.Content = $"Servers ({GuildProgress.Value}/{GuildProgress.Maximum})";
        }

        public void SetCurrentGuild(params dynamic[] currentGuild) { CurrentGuild = new Structs.GuildInfo().Create(currentGuild[0], currentGuild[1]); }

        private void DrawEmote(System.Windows.Controls.Image image)
        {
            EmotePanel.Children.Add(image);
        }


        public async Task AddImage(ulong GuildId, string GuildName, ulong Id, string Name, string Extension, bool IsSticker, MemoryStream Stream)
        { 
            ImageList.Add(new Structs.Img().Create(GuildId, GuildName, Id, Name, Extension, IsSticker, Stream));
            await Task.Delay(1);
        }
        
        public void ClearEmotesAndStickers() { Emotes.Clear(); Stickers.Clear(); ImageList.Clear(); EmotePanel.Children.Clear(); }


        private void EmoteWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            EmoteProgress.Value = e.ProgressPercentage;
            EmoteLabel.Content = $"Emotes ({EmoteProgress.Value}/{EmoteProgress.Maximum})";
        }

        private void GuildWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            GuildProgress.Value = e.ProgressPercentage;
            GuildLabel.Content = $"Servers ({GuildProgress.Value}/{GuildProgress.Maximum})";
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ImageList.Clear();

            foreach (string table in db.GetTables().Result)
            {
                List<Structs.Img> Emotes = db.ReadEmotes(table).Result;

                foreach (Structs.Img item in Emotes)
                {

                    string loc = "emotes";

                    if (item.IsSticker)
                        loc = "stickers";

                    SaveImage(item, item.Extension, loc);
                }

            }
        }

        public void IncreaseGuildProgress()
        {
            GuildProgressCount++;
            GuildWorker.ReportProgress(GuildProgressCount);
        }

        public void IncreaseEmoteProgress()
        {
            EmoteProgressCount++;
            EmoteWorker.ReportProgress(EmoteProgressCount);
        }

        public void Reset()
        {
            GuildProgressCount = 0;
            EmoteProgressCount = 0;
            EmoteWorker.ReportProgress(GuildProgressCount);
            EmoteWorker.ReportProgress(EmoteProgressCount);
        }

        public void ResetEmoteCount()
        {
            EmoteProgressCount = 0;
            EmoteWorker.ReportProgress(EmoteProgressCount);
        }

        internal void SetEmoteCount(int count)
        {
            EmoteProgress.Maximum = count;
        }

        private void SaveImage(Structs.Img Item, string FileExtension, string Location)
        {
            FileInfo fileInfo = new FileInfo(NamingUtility.ReplaceInvalidPath($"Exports/{Item.GuildName}/{Location}/{Item.EmoteName}{FileExtension}", "_"));
            if(!Directory.Exists(fileInfo.DirectoryName))
                Directory.CreateDirectory(fileInfo.DirectoryName);

            using (FileStream fs = new(fileInfo.FullName, FileMode.Create))
            {
                if (Item.Stream == null)
                {
                    MessageBox.Show("Null Stream!");
                    return;
                }

                Item.Stream.Position = 0;
                Item.Stream.CopyTo(fs);
            }
        }

        private async Task Continue()
        {
            List<ulong> EmoteIds = new List<ulong>();
            List<ulong> GuildIds = new List<ulong>();
            List<string> Tables = await db.GetTables();
            foreach(string Table in Tables)
            {
                foreach(Structs.Img Img in db.ReadEmotes(Table).Result)
                {
                    EmoteIds.Add(Img.EmoteId);
                    GuildIds.Add(Img.GuildId);
                }
            }

            if(Guilds == null)
                throw new NullReferenceException("EmoteWindow->Continue(): Guilds is null.");

            foreach (Structs.GuildInfo guild in Guilds)
            {
                JObject JGuild = httpHandler?.GetGuild(guild.Id)?.Result;
                if (JGuild == null)
                    throw new NullReferenceException("EmoteWindow->Continue(): JGuild is null.");

                foreach(JToken Emote in JGuild["emotes"])
                {
                    if (!EmoteIds.Contains((ulong)Emote["id"]))
                    {
                        MemoryStream? ms = await httpHandler?.SendRequest((ulong)Emote["id"], false)?.Content.ReadAsStreamAsync() as MemoryStream;
                        ImageList.Add(new Structs.Img().Create(guild.Id, guild.Name, (ulong)Emote["id"], (string)Emote["name"], utility.GetExtension((bool)Emote["animated"]), false, ms));
                    }
                }
            }

        }

        bool AutoScroll;
        private void EmoteScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if(e.ExtentHeightChange == 0)
            {
                if (EmoteScroller.VerticalOffset == EmoteScroller.ScrollableHeight) { AutoScroll = true; }
                else { AutoScroll = false; }
            }

            if(AutoScroll && e.ExtentHeightChange !=  0) { EmoteScroller.ScrollToVerticalOffset(EmoteScroller.ScrollableHeight); }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            await db.GetTables();
        }
    }
}