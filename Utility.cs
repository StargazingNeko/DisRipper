using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DisRipper
{
    public class Utility
    {
        private static CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();
        public static DatabaseHandler db { get; private set; } = new();

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
            return FormatType == 2 ? ".gif" : ".png";
        }
    }
}