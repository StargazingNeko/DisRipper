using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisRipper
{
    public class Utility
    {
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