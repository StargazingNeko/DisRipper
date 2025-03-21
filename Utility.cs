﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace DisRipper
{
    public class Utility
    {
        private static CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();
        public static DatabaseHandler db { get; private set; } = new();
        public static event System.EventHandler _PrintToResponseBox;

        public static bool ResetToken()
        {
            TokenSource.Cancel();
            TokenSource.Dispose();
            TokenSource = new CancellationTokenSource();
            return TokenSource.Token.IsCancellationRequested;
        }

        public static bool IsTokenCanceled()
        {
            if (TokenSource.Token.IsCancellationRequested)
                MessageBox.Show("Task canceled");

            return TokenSource.Token.IsCancellationRequested;
        }

        public static CancellationToken GetCancellationToken()
        {
            return TokenSource.Token;
        }

        public string GetExtension(bool IsAnimated)
        {
            return IsAnimated ? ".gif" : ".png";
        }

        public string GetExtension(int FormatType)
        {
            //return FormatType == 2 ? ".gif" : ".png";
            //==============================================================
            //* Discord converts and stores gifs to apng.
            //* For whatever reason they will not let you access
            //* the original gifs when it comes to stickers unlike emotes.
            //* Will likely work on gif conversion in the future.
            //* For now save as ".apng" so it's at least
            //* distinguishable which are animated and which one are not.
            //*==============================================================
            return FormatType == 2 ? ".apng" : ".png";
        }

        public class PrintEventArgs : EventArgs
        {
            private string StringToPrint;

            public PrintEventArgs(string StringToPrint)
            {
                this.StringToPrint = StringToPrint;
            }

            public string Str
            {
                get { return this.StringToPrint; }
            }
        }

        public static void PrintToResponseBox(object sender, PrintEventArgs str)
        {
            if (_PrintToResponseBox != null)
                _PrintToResponseBox(sender, str);
        }

    }
}