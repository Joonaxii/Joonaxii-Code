using Joonaxii.Image.Codecs;
using Joonaxii.Image.Codecs.BMP;
using Joonaxii.Image.Codecs.PNG;
using Joonaxii.Image.Codecs.Raw;
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
                        using(BMPEncoder bmp = new BMPEncoder(png.Width, png.Height, png.ColorMode))
                        {
                            bmp.Encode(png, fs, true);
                        }

                        using (FileStream fsRLEA = new FileStream($"{pth}/{Path.GetFileNameWithoutExtension(path)}_DECODED RLEa.raw", FileMode.Create))
                        using (BMPEncoder rawRLEA = new BMPEncoder(png.Width, png.Height, png.ColorMode))
                        using (FileStream fsRLEAID = new FileStream($"{pth}/{Path.GetFileNameWithoutExtension(path)}_DECODED IDXRLEa.raw", FileMode.Create))
                        using (RawTextureEncoder rawRLEAA = new RawTextureEncoder(png.Width, png.Height, png.ColorMode))
                        {
                            rawRLEAA.CopyFrom(png);
                            rawRLEAA.SetCompressionMode(RawTextureCompressMode.aRLE, true);
                            rawRLEAA.Encode(fsRLEA, true);
                                   
                            rawRLEAA.SetCompressionMode(RawTextureCompressMode.IdxaRLE, true);
                            rawRLEAA.Encode(fsRLEAID, true);
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