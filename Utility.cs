using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DisRipper
{
    public class Utility
    {
        public static CancellationTokenSource TokenSource { get; private set; } = new CancellationTokenSource();

        public static bool ResetToken()
        {
            TokenSource.Cancel();
            TokenSource.Dispose();
            TokenSource = new CancellationTokenSource();
            return TokenSource.Token.IsCancellationRequested;
        }

        public static bool IsTokenCanceled()
        {
            return TokenSource.Token.IsCancellationRequested;
        }

        public string GetExtension(bool IsAnimated)
        {
            if (IsAnimated)
                return ".gif";

            return ".webp";
        }

        public string GetExtension(int FormatType)
        {
            if (FormatType == 2)
                return ".png";

            return ".webp";
        }
    }
}