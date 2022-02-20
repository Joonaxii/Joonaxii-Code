﻿using Joonaxii.Audio;
using Joonaxii.Image;
using Joonaxii.Image.Codecs;
using Joonaxii.Image.Codecs.PNG;
using Joonaxii.Debugging;
using Joonaxii.IO;
using Joonaxii.MathJX;
using Joonaxii.Radio;
using Joonaxii.Radio.RTTY;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Joonaxii.Image.Codecs.BMP;
using Joonaxii.Collections;
using New.JPEG;
using Joonaxii.Image.Codecs.VTF;
using Joonaxii.Image.Codecs.Raw;
using Joonaxii.Image.Texturing;

namespace Testing_Grounds
{
    public class Program
    {
        private static MenuItem[] _menu = new MenuItem[]
        {
            new TTCCompressTest("TTC Compression/Decompression"),
            new TTCCompressDataTest("TTC Data Decompress"),
            new ImageNoisinessTest("Image Block Noise Test"),

            new PNGDecodeTest("PNG Decode Test"),

            new BmpDecodeTest("Bmp Decode Test"),
            new WebpDecodeTest("Webp Decode Test"),

            new HuffmanCompressTest("Huffman Compression/Decompression"),

            new SteganographyTest("Steganography Testing"),

            new LZWTest("LZW Compression/Decompression"),
            new LZWTestBinaryRW("LZW Compression/Decompression Binary R/W"),

            new Bit256Test("Bit256 Testing"),

            new QuitItem(),
        };
        private static int _selectedItem;

        public static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            string path = "";

            startTEXTURE:
            Console.Clear();
            Console.WriteLine("Enter the full path of the PNG");
            path = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(path)) { goto startTEXTURE; }

            using (FileStream fs = new FileStream(path, FileMode.Open))
            using (PNGDecoder png = new PNGDecoder(fs))
            {
                var res = png.Decode(false);
                Console.WriteLine($"PNG Done [{res}]");

                if (res == ImageDecodeResult.Success)
                {
                    using (Texture tex = new Texture(png.Width, png.Height, ColorMode.RGB24))
                    using (FileStream fsP = new FileStream($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_PNG.png", FileMode.Create))
                    using (FileStream fsPGR = new FileStream($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_PNG GR.png", FileMode.Create))
                    using (PNGEncoder pngEnc = new PNGEncoder(png.Width, png.Height, ColorMode.RGB24))
                    {
                        pngEnc.Flags = ImageDecoderFlags.ForceRGB;
                        var pix = png.GetPixelsRef();
                        tex.SetPixels(pix);
                        pix = tex.GetPixels();
                        pngEnc.SetPixelsRef(ref pix);

                        var pngRes = pngEnc.Encode(fsP, true);
                        Console.WriteLine($"PNG Done [{pngRes}]");

                        tex.SetPixels(pix);
                        tex.Format = ColorMode.Grayscale;
                        pix = tex.GetPixels();
                        pngEnc.SetPixelsRef(ref pix);

                        pngRes = pngEnc.Encode(fsPGR, true);
                        Console.WriteLine($"PNG Gray Done [{pngRes}]");
                    }
                }
                Console.ReadKey();
            }

            //startVTF:
            //Console.Clear();
            //Console.WriteLine("Enter the full path of the VTF file");
            //path = Console.ReadLine().Replace("\"", "");

            //if (!File.Exists(path)) { goto startVTF; }

            //using (FileStream fs = new FileStream(path, FileMode.Open))
            //using (VTFDecoder vtf = new VTFDecoder(fs))
            //{
            //    var res = vtf.Decode(false);
            //    Console.WriteLine($"VTF Done [{res}]");

            //    if (res == ImageDecodeResult.Success)
            //    {
            //        if (vtf.HasThumbnail)
            //        {
            //            using (FileStream fsP = new FileStream($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_Thumb_PNG.png", FileMode.Create))
            //            using (PNGEncoder pngEnc = new PNGEncoder(vtf.ThumbnailHeight, vtf.ThumbnailWidth, ColorMode.RGB24))
            //            {
            //                pngEnc.SetFlags(PNGFlags.ForceRGB | PNGFlags.ForceNoAlpha);
            //                var pix = vtf.GetThumbnailRef();
            //                pngEnc.SetPixelsRef(ref pix);
            //                var pngRes = pngEnc.Encode(fsP, true);
            //                Console.WriteLine($"PNG Thumb Done [{pngRes}]");
            //            }
            //        }

            //        using (FileStream fsP = new FileStream($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_PNG.png", FileMode.Create))
            //        using (PNGEncoder pngEnc = new PNGEncoder(vtf.Width, vtf.Height, ColorMode.RGB24))
            //        {
            //            pngEnc.SetFlags(PNGFlags.ForceRGB | PNGFlags.ForceNoAlpha);
            //            var pix = vtf.GetPixelsRef();
            //            pngEnc.SetPixelsRef(ref pix);
            //            var pngRes = pngEnc.Encode(fsP, true);
            //            Console.WriteLine($"PNG Done [{pngRes}]");
            //        }

            //    }

            //    Console.ReadKey();
            //}


            //startJPG:
            //Console.Clear();
            //Console.WriteLine("Enter the full path of the testable JPG");
            //path = Console.ReadLine().Replace("\"", "");

            //if (!File.Exists(path)) { goto startJPG; }

            //using (FileStream fs = new FileStream(path, FileMode.Open))
            //using (JPEGDecoder jpeg = new JPEGDecoder(fs))
            //{
            //    var res = jpeg.Decode(false);
            //    Console.WriteLine($"JPEG Done [{res}]");

            //    if (res == ImageDecodeResult.Success)
            //    {
            //        using (FileStream fsP = new FileStream($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_PNG.png", FileMode.Create))
            //        using (PNGEncoder pngEnc = new PNGEncoder(jpeg.Width, jpeg.Height, ColorMode.RGB24))
            //        {
            //            pngEnc.SetFlags(PNGFlags.ForceRGB);
            //            var pix = jpeg.GetPixelsRef();
            //            pngEnc.SetPixelsRef(ref pix);
            //            var pngRes = pngEnc.Encode(fsP, true);
            //            Console.WriteLine($"PNG Done [{pngRes}]");
            //        }

            //    }

            //    Console.ReadKey();
            //}

            startPNG:
            Console.Clear();
            Console.WriteLine("Enter the full path of the testable PNG");
            path = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(path)) { goto startPNG; }

            string dirP = Path.GetDirectoryName(path);
            string namP = Path.GetFileNameWithoutExtension(path);

            using (FileStream fs = new FileStream(path, FileMode.Open))
            using (PNGDecoder decPNG = new PNGDecoder(fs))
            {
                var res = decPNG.Decode(false);
                switch (res)
                {
                    default: Console.WriteLine($"PNG Decode Failed: [{res}]"); break;
                    case ImageDecodeResult.Success:
                        Console.WriteLine($"PNG Decode {res}!");
                        using (FileStream fsEnc = new FileStream($"{dirP}/{namP}_PAL.png", FileMode.Create))
                        using (FileStream fsEncFilt = new FileStream($"{dirP}/{namP}_PAL_Filter.png", FileMode.Create))
                        using (FileStream fsEncFiltF = new FileStream($"{dirP}/{namP}_PAL_Forced_Filter.png", FileMode.Create))
                        using (FileStream fsEncBroken = new FileStream($"{dirP}/{namP}_PAL_Broken.png", FileMode.Create))
                        using (PNGEncoder encPNG = new PNGEncoder(decPNG.Width, decPNG.Height, decPNG.ColorMode))
                        {
                            var pix = decPNG.GetPixelsRef();
                            encPNG.SetPixelsRef(ref pix);
                       
                            encPNG.Flags = ImageDecoderFlags.AllowBigIndices | ImageDecoderFlags.ForceRGB;
                            encPNG.PNGFlags = PNGFlags.UseBrokenSubFilter | PNGFlags.OverrideFilter;
                            encPNG.SetOverrideFilter(PNGFilterMethod.Average);

                            var resEnc = encPNG.Encode(fsEnc, false);
                            switch (resEnc)
                            {
                                default: Console.WriteLine($"PNG Encode Failed: [{resEnc}, {encPNG.Flags}]"); break;
                                case ImageEncodeResult.Success:
                                    Console.WriteLine($"PNG Encode [{resEnc}, {encPNG.Flags}]!");
                                    break;
                            }

                            encPNG.Flags = (ImageDecoderFlags.AllowBigIndices | ImageDecoderFlags.ForcePalette  /*| PNGFlags.OverrideFilter*/);
                            encPNG.PNGFlags = PNGFlags.ForceFilter;
                            //encPNG.SetOverrideFilter(PNGFilterMethod.Paeth);

                            resEnc = encPNG.Encode(fsEncFilt, false);
                            switch (resEnc)
                            {
                                default: Console.WriteLine($"PNG Encode Failed: [{resEnc}, {encPNG.Flags}]"); break;
                                case ImageEncodeResult.Success:
                                    Console.WriteLine($"PNG Encode [{resEnc}, {encPNG.Flags}]!");
                                    break;
                            }

                            encPNG.Flags = (ImageDecoderFlags.AllowBigIndices | ImageDecoderFlags.ForcePalette /*| PNGFlags.OverrideFilter*/);
                            encPNG.PNGFlags = PNGFlags.ForceFilter | PNGFlags.OverrideFilter;
                            encPNG.SetOverrideFilter(PNGFilterMethod.Paeth);

                            resEnc = encPNG.Encode(fsEncFiltF, false);
                            switch (resEnc)
                            {
                                default: Console.WriteLine($"PNG Encode Failed: [{resEnc}, {encPNG.Flags}]"); break;
                                case ImageEncodeResult.Success:
                                    Console.WriteLine($"PNG Encode [{resEnc}, {encPNG.Flags}]!");
                                    break;
                            }

                            encPNG.Flags = (ImageDecoderFlags.AllowBigIndices | ImageDecoderFlags.ForcePalette/*| PNGFlags.OverrideFilter*/);
                            encPNG.PNGFlags = PNGFlags.None;
                           //encPNG.SetOverrideFilter(PNGFilterMethod.Paeth);

                           resEnc = encPNG.Encode(fsEncBroken, false);
                            switch (resEnc)
                            {
                                default: Console.WriteLine($"PNG Encode Failed: [{resEnc}, {encPNG.Flags}]"); break;
                                case ImageEncodeResult.Success:
                                    Console.WriteLine($"PNG Encode [{resEnc}, {encPNG.Flags}]!");
                                    break;
                            }
                        }
                        break;
                }
            }

            Console.ReadKey();
            start:
            Console.Clear();
            Console.WriteLine("Enter the full path of the 320x256 png to be converted to SSTV");
            path = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(path)) { goto start; }

            FastColor[] pixels = null;

            int w = 320;
            int h = 256;

            using (FileStream fsB = new FileStream(path, FileMode.Open))
            using (PNGDecoder png = new PNGDecoder(fsB))
            {
                var res = png.Decode(false);

                if (res != ImageDecodeResult.Success)
                {
                    Console.WriteLine($"PNG decode failed! [{res}]");
                    Console.ReadKey();
                    goto start;
                }
                w = png.Width;
                h = png.Height;
                pixels = new FastColor[w * h];

                png.GetPixels(pixels);
            }

            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);

            SSTVProtocol[] protocols = new SSTVProtocol[]
            {
                //SSTVProtocol.Martin1,
                //SSTVProtocol.Martin2,

                //SSTVProtocol.PD50,
                //SSTVProtocol.PD90,
                //SSTVProtocol.PD120,
                //SSTVProtocol.PD160,
                //SSTVProtocol.PD180,
                //SSTVProtocol.PD240,
                //SSTVProtocol.PD290,

                //SSTVProtocol.Scottie1,
                //SSTVProtocol.Scottie2,
                //SSTVProtocol.Scottie3,
                //SSTVProtocol.Scottie4,
                //
                //SSTVProtocol.ScottieDX,
                //SSTVProtocol.ScottieDX2,

                SSTVProtocol.Robot12,
                SSTVProtocol.Robot24,
                SSTVProtocol.Robot36,
                SSTVProtocol.Robot72,

                //SSTVProtocol.Scottie1,
                //SSTVProtocol.Scottie2,
                //SSTVProtocol.Scottie3,
                //SSTVProtocol.Scottie4,
            };

            for (int i = 0; i < protocols.Length; i++)
            {
                using (FileStream fs = new FileStream($"{dir}/{name} SSTV ({protocols[i]}) 24.wav", FileMode.Create))
                using (WavEncoder wav = new WavEncoder(fs))
                {
                    wav.Setup(1, 44100 >> 2, 16);
                    wav.WriteStaticData();
                    using (SSTVEncoder sstv = new SSTVEncoder(wav, false, protocols[i], SSTVFlags.CenterX, SSTVEncoder.TheRollVOX))
                    {
                        sstv.VOXToneDurationScale = 0.5;
                        var res = sstv.Encode(pixels, w, h);
                        Console.WriteLine($"SSTV Encode Done ({sstv.Protocol})! [{res}]");
                    }

                    System.Diagnostics.Debug.Print($"Samples [{wav.BitsPerSample}/{wav.Samples.BytesPerValue}]: {wav.SampleDataWritten} bytes");
                }
            }

            Console.WriteLine("Done!");
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            //FastColor[] pixels = new FastColor[100];
            //FastColor[] pixelsOut = new FastColor[100];
            //for (int i = 0; i < pixels.Length; i++)
            //{
            //    byte ii = (byte)(i % 255);
            //    pixels[i] = new FastColor(ii);
            //}

            ////ulong a = 0xFF_00_00_00_00_00_00_FF;
            ////ulong b = 0xF0;
            ////const byte o = 5;
            //byte[] bytes = null;

            //using (FileStream ms = new FileStream($"Test.dat", FileMode.Create))
            //using (BitWriter bw = new BitWriter(ms))
            //{
            //    for (int i = 0; i < pixels.Length; i++)
            //    {
            //        var c = pixels[i];
            //        bw.Write(c.r);
            //        bw.Write(c.g);
            //        bw.Write(c.b);
            //    }
            //    bw.FlushBitBuffer();
            //    ms.Flush();

            //    MemoryStream mss = new MemoryStream();
            //    ms.CopyToWithPos(mss);
            //    bytes = mss.ToArray();
            //}

            //using (MemoryStream ms = new MemoryStream(bytes))
            //using (BitReader br = new BitReader(ms))
            //{
            //    for (int i = 0; i < pixels.Length; i++)
            //    {
            //        pixelsOut[i] = new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte());
            //    }
            //}
            //bool areSame = true;
            //for (int i = 0; i < pixelsOut.Length; i++)
            //{
            //    if(pixelsOut[i] != pixels[i])
            //    {
            //        Console.WriteLine($"Pixels At index {i} aren't the same! [{pixels[i]} ==> {pixelsOut[i]}]");
            //        areSame = false;
            //        break;
            //    }
            //}
            //if (areSame)
            //{
            //    Console.WriteLine($"Pixels Are all same!");
            //}

            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            //Console.WriteLine("Begin Test");
            //Console.ReadKey();

            //const int TEST_COUNT = 1_000;
            //const int BYTES_TO_WRITE = 1_00_000;
            //long[] ticks = new long[TEST_COUNT];

            //long maxTicks;
            //long minTicks;
            //double avgTicks;

            //Stopwatch sw = new Stopwatch();
            //for (int i = 0; i < TEST_COUNT; i++)
            //{
            //    using(MemoryStream ms = new MemoryStream())
            //    using(BinaryWriter bw = new BinaryWriter(ms))
            //    {
            //        sw.Restart();
            //        for (int j = 0; j < BYTES_TO_WRITE; j++)
            //        {
            //            bw.Write((ulong)j);
            //        }
            //        sw.Stop();
            //    }
            //    ticks[i] = sw.ElapsedTicks;
            //}
            //GetTicks(ticks, out minTicks, out maxTicks, out avgTicks);
            //Console.WriteLine($"Binary Writer [{TEST_COUNT}, {BYTES_TO_WRITE}] => AVG: {(long)(avgTicks / 10000.0)} ms, MIN: {(long)(minTicks / 10000.0)} ms, MAX: {(long)(maxTicks / 10000.0)} ms");

            //for (int i = 0; i < TEST_COUNT; i++)
            //{
            //    using (MemoryStream ms = new MemoryStream())
            //    using (BitWriter bw = new BitWriter(ms))
            //    {
            //        sw.Restart();
            //        for (int j = 0; j < BYTES_TO_WRITE; j++)
            //        {
            //            bw.Write((ulong)j);
            //        }
            //        sw.Stop();
            //    }
            //    ticks[i] = sw.ElapsedTicks;
            //}
            //GetTicks(ticks, out minTicks, out maxTicks, out avgTicks);
            //Console.WriteLine($"Bit Writer [{TEST_COUNT}, {BYTES_TO_WRITE}] => AVG: {(long)(avgTicks / 10000.0)} ms, MIN: {(long)(minTicks / 10000.0)} ms, MAX: {(long)(maxTicks / 10000.0)} ms");

            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            //Console.ReadKey();
            _selectedItem = MoveSelection(0, 0);
            HandleMenu();
        }

        private static void GetTicks(long[] ticks, out long min, out long max, out double avg)
        {
            min = long.MaxValue;
            max = 0;
            avg = 0;

            for (int i = 0; i < ticks.Length; i++)
            {
                var t = ticks[i];
                avg += t;

                min = t < min ? t : min;
                max = t > max ? t : max;
            }
            avg /= ticks.Length;
        }

        private static void ResetTitle()
        {
            Console.Clear();

            Console.WriteLine($"Welcome to Joonaxii's IO Testing Grounds!");
            Console.WriteLine($"Select the IO process to test from the list below");
        }

        private static void HandleMenu()
        {
            ResetTitle();
            int y = Console.CursorTop + 1;

            DrawMenuItems(y);

            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.DownArrow:
                        _selectedItem = MoveSelection(_selectedItem, 1);
                        DrawMenuItems(y);
                        break;

                    case ConsoleKey.UpArrow:
                        _selectedItem = MoveSelection(_selectedItem, -1);
                        DrawMenuItems(y);
                        break;

                    case ConsoleKey.Enter:
                        MenuItem item = _menu[_selectedItem];
                        if (!item.enabled) { break; }

                        Console.Clear();
                        if (!item.OnClick())
                        {
                            return;
                        }

                        ResetTitle();
                        DrawMenuItems(y);
                        break;
                }
            }
        }

        private static void DrawMenuItems(int y)
        {
            for (int i = 0; i < _menu.Length; i++)
            {
                var itm = _menu[i];
                bool selected = i == _selectedItem;

                Console.SetCursorPosition(0, y + i);
                Console.ForegroundColor = itm.enabled ? (selected ? ConsoleColor.Yellow : ConsoleColor.Gray) : (selected ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray);
                Console.Write((selected ? $"<{itm.name}>" : itm.name).PadRight(itm.name.Length + 2));
            }
        }

        private static int MoveSelection(int input, int dir)
        {
            input += dir;
            input = input >= _menu.Length ? 0 : input < 0 ? _menu.Length - 1 : input;

            int start = input;
            dir = dir == 0 ? 1 : dir;

            MenuItem itm = _menu[input];
            while (!itm.enabled)
            {
                input += dir;
                input = input >= _menu.Length ? 0 : input < 0 ? _menu.Length - 1 : input;

                if (input == start) { break; }
                itm = _menu[input];
            }
            return input;
        }
    }
}
