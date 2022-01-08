using Joonaxii.Data.Image;
using Joonaxii.Data.Image.IO;
using Joonaxii.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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

            bool outputRaw = false;
            Console.Clear();
            Console.WriteLine("Would you like to ouput raw textures as well? (Y/N)");
            while (true)
            {
                ConsoleKey k = Console.ReadKey().Key;
                if(k == ConsoleKey.Y) { outputRaw = true; break; }
                if(k == ConsoleKey.N) { outputRaw = false; break; }
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
                        string relativeName = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}";
                        string pngPath = $"{relativeName}_PNG.png";

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
                            bm.Save(pngPath, ImageFormat.Png);
                            Console.WriteLine($"PNG saved to '{pngPath}'");
                        }

                        if (outputRaw)
                        {
                            List<string> allRaw = new List<string>()
                            {
                                $"{relativeName}_NONE.raw",
                                $"{relativeName}_HUFF.raw",
                                $"{relativeName}_RLE.raw",
                                $"{relativeName}_RLEHuff.raw",
                                $"{relativeName}_Auto.raw",
                            };
                            using (RawTextureEncoder encRaw = new RawTextureEncoder(0, 0, 32))
                            using (FileStream streamRaw = new FileStream(allRaw[0], FileMode.Create))
                            using (FileStream streamHuff = new FileStream(allRaw[1], FileMode.Create))
                            using (FileStream streamRLE = new FileStream(allRaw[2], FileMode.Create))
                            using (FileStream streamRLEHuff = new FileStream(allRaw[3], FileMode.Create))
                            using (FileStream streamAuto = new FileStream(allRaw[4], FileMode.Create))
                            {
                                encRaw.CopyFrom(webpDec);

                                encRaw.Encode(RawTextureCompressMode.None, streamRaw, true);
                                Console.WriteLine($"RAW saved with {RawTextureCompressMode.None} to relative dir {relativeName}");

                                encRaw.Encode(RawTextureCompressMode.RLE, streamRLE, true);
                                Console.WriteLine($"RAW saved with {RawTextureCompressMode.RLE} to relative dir {relativeName}");

                                encRaw.Encode(RawTextureCompressMode.RLEHuffman, streamRLEHuff, true);
                                Console.WriteLine($"RAW saved with {RawTextureCompressMode.RLEHuffman} to relative dir {relativeName}");

                                encRaw.Encode(RawTextureCompressMode.Huffman, streamHuff, true);
                                Console.WriteLine($"RAW saved with {RawTextureCompressMode.Huffman} to relative dir {relativeName}");

                                encRaw.Encode(RawTextureCompressMode.Auto, streamAuto, true);
                                Console.WriteLine($"RAW saved with {RawTextureCompressMode.Auto} to relative dir {relativeName}");

                            }

                            string debugPath = $"{Path.GetDirectoryName(path)}/Raw Out/";
                            if (!Directory.Exists(debugPath)) { Directory.CreateDirectory(debugPath); }

                            for (int i = 0; i < allRaw.Count; i++)
                            {
                                if (!File.Exists(allRaw[i])) { continue; }
                                string name = Path.GetFileNameWithoutExtension(allRaw[i]);
                                FileStream rawStream = new FileStream(allRaw[i], FileMode.Open);
                                //MemoryStream memStream = new MemoryStream();
                                //rawStream.CopyToWithPos(memStream);
                                using (RawTextureDecoder rawDec = new RawTextureDecoder(rawStream))
                                {
                                    var resul = rawDec.Decode(false);
                                    switch (resul)
                                    {
                                        case ImageDecodeResult.Success:
                                            using(Bitmap bm = new Bitmap(rawDec.Width, rawDec.Height))
                                            {
                                                for (int y = 0; y < rawDec.Height; y++)
                                                {
                                                    for (int x = 0; x < rawDec.Width; x++)
                                                    {
                                                        var p = rawDec.GetPixel(x, y);
                                                        bm.SetPixel(x, y, Color.FromArgb(p.a, p.r, p.g, p.b));
                                                    }
                                                }

                                                bm.Save($"{debugPath}/{name}_OUT.png", ImageFormat.Png);
                                                Console.WriteLine($"Saved PNG of read '{name}'!");
                                            }
                                            continue;
                                    }
                                    Console.WriteLine($"Failed to read '{name}'! [{resul}]");
                                }
                                rawStream.Dispose();
                               // memStream.Dispose();
                            }
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