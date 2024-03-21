using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DisEmoteRipper
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
        private bool bDrawImages = false;
        private Structs.GuildInfo CurrentGuild;
        private Structs.Img img = new();
        private readonly DatabaseHandler db = new();
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

            Task.Run(async () => await WaitForDrawing());
        }


        private async void ImageList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Add emote to database here
        }

        private async Task WaitForDrawing()
        {
            bool bRun = true;
            while (bRun)
            {
                if (bDrawImages == true)
                {
                    await Application.Current.Dispatcher.Invoke(async () =>
                    {
                        await Images();
                    });
                    bRun = false;
                }
                await Task.Delay(1000);
            }
        }

        public void SetHttpHandler(HttpHandler handler) => httpHandler = handler;

        public void SetGuilds(ObservableCollection<Structs.GuildInfo> guilds)
        {
            Guilds = guilds;
            GuildProgress.Maximum = Guilds.Count;
            GuildLabel.Content = $"Servers ({GuildProgress.Value}/{GuildProgress.Maximum})";
        }

        public void SetCurrentGuild(params dynamic[] currentGuild) { CurrentGuild.ID = currentGuild[0]; CurrentGuild.Name = currentGuild[1]; }

        public void BeginDrawing()
        {
            bDrawImages = true;
        }

        private void DrawEmote(Image image)
        {
            EmotePanel.Children.Add(image);
        }

        public async Task AddEmote(ulong EmoteID, string EmoteName) { Emotes.Add(new KeyValuePair<ulong, string>(EmoteID, EmoteName)); await Task.Delay(1); }
        public async Task AddSticker(ulong StickerID, string StickerName) { Stickers.Add(new KeyValuePair<ulong, string>(StickerID, StickerName)); await Task.Delay(1); }
        public void ClearEmotesAndStickers() { Emotes.Clear(); Stickers.Clear(); ImageList.Clear(); EmotePanel.Children.Clear(); }

        public async Task Images()
        {
            GuildLabel.Content = $"Servers ({GuildProgress.Value}/{GuildProgress.Maximum})";
            try
            {
                EmoteProgress.Maximum = Emotes.Count+Stickers.Count;
                GuildProgress.Maximum = Convert.ToDouble(Guilds?.Count);
                ImageList.Clear();

                foreach (KeyValuePair<ulong, string> item in Emotes)
                {
                    await ProcessPreview(item, false);
                    await Task.Delay(250);
                }

                foreach (KeyValuePair<ulong, string> item in Stickers)
                {
                    await ProcessPreview(item, true);
                    await Task.Delay(250);
                }

                IncreaseGuildProgress();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async Task ProcessPreview(KeyValuePair<ulong, string> item, bool bIsSticker)
        {
            Image image = new() { Height = 100, Width = 100 };
            HttpResponseMessage? response = httpHandler?.SendRequest(item.Key, bIsSticker);

            if (response?.StatusCode != System.Net.HttpStatusCode.OK)
            {
                using (FileStream fs = File.OpenRead("Error.bmp"))
                {
                    image.Source = BitmapFrame.Create(fs, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    IncreaseEmoteProgress();
                }

                return;
            }

            Stream? stream = response?.Content?.ReadAsStreamAsync()?.Result;
            BitmapFrame bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            img.GuildId = CurrentGuild.ID;
            img.GuildName = CurrentGuild.Name;
            img.Id = item.Key;
            img.Name = item.Value.Split(" ")[0];
            img.IsSticker = bIsSticker;
            if (Convert.ToBoolean(item.Value.Split(" ")[1]))
            {
                if(img.IsSticker)
                {
                    img.Extension = ".apng";
                }
                else
                {
                    img.Extension = ".gif";
                }
            }
            else
            {
                img.Extension = ".webp";
            }

            if (stream == null)
            {
                MessageBox.Show("Null stream!");
                return;
            }
            img.stream = stream as MemoryStream;

            ImageList.Add(img);
            image.Source = bitmap;

            MemoryStream ms;
            await db.Write(img.GuildId, img.GuildName, img.Id, img.Name, img.Extension, img.IsSticker, img.stream.ToArray(), false);
            IncreaseEmoteProgress();

            DrawEmote(image);
            await Task.Delay(1);
        }

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

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentGuild.Name))
                return;

            foreach (Structs.Img item in ImageList)
            {
                if (string.IsNullOrEmpty(item.Name))
                    return;

                if (!Directory.Exists(item.GuildName))
                {
                    Directory.CreateDirectory(NamingUtility.ReplaceInvalidPath(item.GuildName + "/emotes", "_").Replace("_", ""));
                    Directory.CreateDirectory(NamingUtility.ReplaceInvalidPath(item.GuildName + "/stickers", "_").Replace("_", ""));
                }

                string fe = ".webp";
                string loc = "emotes";

                if (item.IsSticker)
                    loc = "stickers";

                SaveImage(item, fe, loc);
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

        private void SaveImage(Structs.Img Item, string Location, string FileExtension)
        {
            using (FileStream fs = new(NamingUtility.ReplaceInvalidPath($"{Item.GuildName}/{Location}/{Item.Name}{FileExtension}", "_"), FileMode.Create))
            {
                if (Item.stream == null)
                {
                    MessageBox.Show("Null stream!");
                    return;
                }
                Item.stream.Position = 0;
                Item.stream.CopyTo(fs);
            }

        }

        private void Continue()
        {
            List<Structs.Img> i = db.ReadEmotes("Emotes").Result;
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

        }
    }
}