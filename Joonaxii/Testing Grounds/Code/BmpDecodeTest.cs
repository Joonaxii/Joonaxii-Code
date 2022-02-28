using Joonaxii.Image.Codecs;
using Joonaxii.Image.Codecs.BMP;
using System;
using System.Drawing;
using System.IO;

namespace Testing_Grounds
{
    public class BmpDecodeTest : MenuItem
    {
        public BmpDecodeTest(string name, bool enabled = true) : base(name, enabled)
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

            using(FileStream stream = new FileStream(path, FileMode.Open))
            using(BMPDecoder bmpDec = new BMPDecoder(stream))
            {
                var res = bmpDec.Decode(false);
                var tex = bmpDec.GetTexture();
                Console.WriteLine($"Bmp Decode! {res}");
                switch (res)
                {
                    case ImageDecodeResult.Success:
                        string pngPath = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_PNG.png";
                        using(Bitmap bm = new Bitmap(bmpDec.Width, bmpDec.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        {
                            for (int y = 0; y < bmpDec.Height; y++)
                            {
                                for (int x = 0; x < bmpDec.Width; x++)
                                {
                                    var c = tex.GetPixel(x, y);
                                    bm.SetPixel(x, y, Color.FromArgb(c.a, c.r, c.g, c.b));
                                }
                            }
                            bm.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
                            Console.WriteLine($"PNG saved to '{pngPath}'");
                        }
                        break;
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