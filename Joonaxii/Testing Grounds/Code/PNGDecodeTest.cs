using Joonaxii.Data.Image.Conversion;
using Joonaxii.Data.Image.Conversion.Encoders;
using Joonaxii.Data.Image.Conversion.PNG;
using System;
using System.IO;

namespace Testing_Grounds
{
    public class PNGDecodeTest : MenuItem
    {
        public PNGDecodeTest(string name, bool enabled = true) : base(name, enabled)
        {
        }

        public override bool OnClick()
        {
            file:
            Console.WriteLine("Please enter the path to the png file that should be read");

            string path;
            path = Console.ReadLine().Replace("\"", "");
            if (!File.Exists(path))
            {
                Console.WriteLine($"Path '{path}' is invalid!");
                goto file;
            }

            using(FileStream stream = new FileStream(path, FileMode.Open))
            using(PNGDecoder png = new PNGDecoder(stream))
            {
                var res = png.Decode(false);
                switch (res)
                {
                    case ImageDecodeResult.Success:
                        Console.WriteLine("PNG Decode Succeeded!");

                        string pth = Path.GetDirectoryName(path);
                        string file = $"{Path.GetFileNameWithoutExtension(path)}_DECODED.bmp";

                        using(FileStream fs = new FileStream($"{pth}/{file}", FileMode.Create))
                        using(BmpEncoder bmp = new BmpEncoder(png.Width, png.Height, png.ColorMode))
                        {
                            bmp.Encode(png, fs, true);
                        }

                        break;
                    default:
                        Console.WriteLine($"PNG Decode Failed [{res}]!");
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