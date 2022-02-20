using System.IO;

namespace Joonaxii.Image.Codecs.DXT
{
    public class BC1Decoder
    {
        public static void SeekPast(Stream stream, int width, int height)
        {
            int wh = ((width + 3) >> 2) * ((height + 3) >> 2);  
            stream.Seek(wh * 8, SeekOrigin.Current);
        }

        public static void Decode(BinaryReader br, FastColor[] pixels, int width, int height)
        {
            FastColor[] colBuf = new FastColor[4];

            int w4 = width >> 2;
            int h4 = height >> 2;

            for (int h = 0; h < h4; h++)
            {
                int hP = h * 4;
                for (int w = 0; w < w4; w++)
                {
                    int wP = w * 4;
                    DXTUtils.Interpolate(br, colBuf, true);
                    uint colorIndices = br.ReadUInt32();

                    for (int y = 3; y >= 0; y--)
                    {
                        int yP = (3 - y) * 4;
                        for (int x = 3; x >= 0; x--)
                        {
                            int rgbaI = (hP + y) * width + (wP + (3 - x));
                            int pixI = x + yP;
                            uint colI = (colorIndices >> (2 * (15 - pixI))) & 0x03U;
                            pixels[rgbaI] = colBuf[colI];
                        }
                    }
                }
            }
        }
    }
}
