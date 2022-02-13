using Joonaxii.Image;
using Joonaxii.Image.Codecs;
using Joonaxii.Image.Codecs.BMP;
using Joonaxii.Image.Processing;
using System;
using System.Drawing;
using System.IO;

namespace Testing_Grounds
{
    public class ImageNoisinessTest : MenuItem
    {
        public ImageNoisinessTest(string name, bool enabled = true) : base(name, enabled)
        {
        }

        public override bool OnClick()
        {
            file:
            Console.WriteLine("Please enter the path to the bmp file that should be read");

            string path;
            path = Console.ReadLine().Replace("\"", "");
            if (!File.Exists(path))
            {
                Console.WriteLine($"Path '{path}' is invalid!");
                goto file;
            }

            using(Bitmap bm = new Bitmap(path))
            {
                FastColor[] pixels = new FastColor[bm.Width * bm.Height];
                bool hasAlpha = false;
                for (int y = 0; y < bm.Height; y++)
                {
                    int yP = y * bm.Width;
                    for (int x = 0; x < bm.Width; x++)
                    {
                        int i = yP + x;
                        var c = bm.GetPixel(x, y);
                        pixels[i] = new FastColor(c.R, c.G, c.B, c.A);

                        hasAlpha |= c.A < 255;
                    }
                }
                ImageNoiseDetector det = new ImageNoiseDetector();

                float sqrRMS = det.Process(new PixelArray(pixels), bm.Width, bm.Height, 32);
                float rms = (float)Math.Sqrt(sqrRMS);

                string name = Path.GetFileNameWithoutExtension(path);

                Console.WriteLine($"{name} Statistics: ");
                Console.WriteLine($"    -Width: {bm.Width}");
                Console.WriteLine($"    -Height: {bm.Height}");

                Console.WriteLine($"    -RMS: {rms}");
                Console.WriteLine($"    -Sqr RMS: {sqrRMS}");

                FastColor[] temp = new FastColor[pixels.Length];
                ReadWirtePixelArray rw = new ReadWirtePixelArray(pixels, temp);
                ImageNoisinessDebugger deb = new ImageNoisinessDebugger(8);
                deb.Process(rw, bm.Width, bm.Height, 32);

                Console.WriteLine($"{name} Debug Statistics: ");
                Console.WriteLine($"    -Bits Per RMS: {deb.GetBitsPerRMS}");

                using(BMPEncoder bmp = new BMPEncoder(bm.Width, bm.Height, (byte)(hasAlpha ? 32 : 24)))
                {
                    bmp.SetPixels(temp);
                    using(MemoryStream ms = new MemoryStream())
                    {
                        var result = bmp.Encode(ms, true);
                        switch (result)
                        {
                            case ImageEncodeResult.Success:
                                ms.Flush();
                                File.WriteAllBytes($"{Path.GetDirectoryName(path)}/{name}_DEBUG.bmp", ms.ToArray());
                                Console.WriteLine($"Saved Debug BMP of '{name}'!");
                                break;
                            default:
                                Console.WriteLine($"BMP encode failed! [{result}]");
                                break;
                        }
                    }     
                }
            }

            Console.WriteLine($"\nPress enter to go back to the menu.");
            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Enter) { break; }
            }
            return true;
        }
    }
}