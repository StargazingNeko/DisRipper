using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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

        private void ImageList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateImageList(sender, e);
        }

        private async Task UpdateImageList (object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Add emote to database here
            if (e.NewItems == null)
                return;

            System.Windows.Controls.Image ImageControl = new() { Height = 100, Width = 100};
            foreach(Structs.Img img in e.NewItems)
            {
                if (Utility.IsTokenCanceled())
                    return;

                MemoryStream ms = img.MemStream;

                /*
                This loses almost all data, for now we'll just save it as an apng...

                if (img.Extension == ".gif")
                    System.Drawing.Image.FromStream(img.MemStream).Save(ms = new MemoryStream(), System.Drawing.Imaging.ImageFormat.Gif);*/

                BitmapFrame bitmap = BitmapFrame.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                ImageControl.Source = bitmap;
                
                await Utility.db.CreateTable(img.GuildName);
                await Utility.db.WriteEmotes(img.GuildId, img.GuildName, img.EmoteId, img.EmoteName, img.Extension, img.IsSticker, ms);

                DrawEmote(ImageControl);
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

            foreach (string table in Utility.db.GetTables().Result.Where(table => table != "Config" && !Utility.IsTokenCanceled()))
            {
                List<Structs.Img> Emotes = Utility.db.ReadEmotes(table).Result;
                foreach (Structs.Img item in Emotes.Where(item => !Utility.IsTokenCanceled()))
                {
                    string loc = item.IsSticker ? "stickers" : "emotes";
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

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        private void SaveImage(Structs.Img Item, string FileExtension, string Location)
        {
            FileInfo fileInfo =
                new FileInfo(
                    NamingUtility.ReplaceInvalidPath(
                        $"Exports/{Item.GuildName}/{Location}/{Item.EmoteName}{FileExtension}", "_"));
            if (!Directory.Exists(fileInfo.DirectoryName))
                Directory.CreateDirectory(fileInfo.DirectoryName);

            using (FileStream fs = new(fileInfo.FullName, FileMode.Create))
            {
                if (Item.MemStream == null)
                {
                    MessageBox.Show("Null MemStream!");
                    return;
                }

                Item.MemStream.Position = 0;
                Item.MemStream.CopyTo(fs);
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


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Utility.PrintToResponseBox(this, new Utility.PrintEventArgs($"Canceled"));

            if (Utility.ResetToken())
            {
                throw new Exception("Failed to reset cancelation token!");
            }
        }
    }
}