
namespace Joonaxii.Image.Processing
{
    public class ColorLUT
    {
        private static ulong MASKS = (128) | (64 << 8) | (32 << 16) | (16 << 24) | (8 << 32) | (4 << 40) | (2 << 48) | (1 << 56);
        private static int[] PRE_DISTANCE = new int[256 * 256];

        private static int GetMask(int i) => (int)((MASKS >> (i << 3)) & 0xFF);
        private FastColor[] _colors;

        static ColorLUT()
        {
            for (int y = 0; y < 256; y++)
            {
                int yP = y * 256;
                for (int x = 0; x < 256; x++)
                {
                    int i = yP + x;
                    int d = x - y;
                    PRE_DISTANCE[i] = d * d;
                }
            }
        }

       // internal class
    }
}
