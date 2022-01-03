using Joonaxii.Data.Image.IO;
using System;
using System.Drawing;
using System.IO;

namespace Testing_Grounds
{
    public class WebpDecodeTest : MenuItem
    {
        public WebpDecodeTest(string name, bool enabled = true) : base(name, enabled)
        {
        }

        public override bool OnClick()
        {
            file:
            Console.Clear();
            Console.WriteLine("Please enter the path to the webp file that should be read");

            string path;
            path = Console.ReadLine().Replace("\"", "");
            if (!File.Exists(path))
            {
                Console.WriteLine($"Path '{path}' is invalid!");
                goto file;
            }

            webpP:
            Console.Clear();
            Console.WriteLine("Please enter the path to the webp decoder");

            string webpPath;
            webpPath = Console.ReadLine().Replace("\"", "");

            if (webpPath != string.Empty && !File.Exists(path))
            {
                Console.WriteLine($"Path '{webpPath}' is invalid!");
                goto webpP;
            }

            using (FileStream stream = new FileStream(path, FileMode.Open))
            using (ImageDecoder webpDec = new ImageDecoder(stream, webpPath))
            {
                var res = webpDec.Decode(false);
                Console.WriteLine($"Webp Decode! {res}");
                switch (res)
                {
                    case ImageDecodeResult.Success:

                        string pngPath = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_PNG.png";
                        using (Bitmap bm = new Bitmap(webpDec.Width, webpDec.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        {
                            for (int y = 0; y < webpDec.Height; y++)
                            {
                                for (int x = 0; x < webpDec.Width; x++)
                                {
                                    var c = webpDec.GetPixel(x, y);
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