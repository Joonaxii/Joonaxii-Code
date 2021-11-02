using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Debugging
{
    public static class DebugExtensions
    {
        private static readonly string[] SIZE_SUFFIXES = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string GetFileSizeString(long bytes, int decimalPlaces = 1)
        {
            if (bytes == 0) { return $"{bytes} bytes"; }
            decimalPlaces = decimalPlaces < 0 ? 0 : decimalPlaces;
            bool isNegative = bytes < 0;

            bytes = isNegative ? -bytes : bytes;

            int mag = (int)Math.Log(bytes, 1024);
            decimal size = (decimal)bytes / (1L << (mag * 10));

            if (Math.Round(size, decimalPlaces) >= 1000)
            {
                mag++;
                size /= 1024;
            }

            return string.Format($"{(isNegative ? "-" : "")}{"{0:n"}{decimalPlaces}{"}"} {"{1}"}", size, SIZE_SUFFIXES[mag]);
        }
    }
}
