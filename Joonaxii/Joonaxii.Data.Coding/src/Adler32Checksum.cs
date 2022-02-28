using System;
using System.Collections.Generic;

namespace Joonaxii.Data.Coding
{
    public class Adler32Checksum
    {
        private const int MODULO = 65521;

        public static unsafe int Calculate(byte* data, int length)
        {
            int sum1 = 1;
            int sum2 = 0;

            while (length-- > 0)
            {
                sum1 = (sum1 + *data++) % MODULO;
                sum2 = (sum1 + sum2) % MODULO;
            }

            return sum2 * 65536 + sum1;
        }
    }
}
