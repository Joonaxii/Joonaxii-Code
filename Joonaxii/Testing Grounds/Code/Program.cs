using Joonaxii.Audio;
using Joonaxii.Image;
using Joonaxii.Image.Codecs;
using Joonaxii.Image.Codecs.PNG;
using Joonaxii.Debugging;
using Joonaxii.IO;
using Joonaxii.MathJX;
using Joonaxii.Audio.Codecs.OGG;
using Joonaxii.Radio;
using Joonaxii.Radio.RTTY;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Joonaxii.Image.Codecs.BMP;
using Joonaxii.Collections.Unmanaged;
using New.JPEG;
using Joonaxii.Image.Codecs.VTF;
using Joonaxii.Image.Codecs.Raw;
using Joonaxii.Image.Texturing;
using Joonaxii.Data.Coding;
using Joonaxii.Audio.Codecs.WAV;
using Joonaxii.Audio.Codecs.MP3;

namespace Testing_Grounds
{
    public class Program
    {
        public class IntComparer : IComparer<int>
        {
            public int Compare(int x, int y) => x.CompareTo(y);
        }

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
            Stopwatch sw = new Stopwatch();
            const int SIZE = 1024 * 1024 * 1024;

            Console.WriteLine($"Press enter to allocate [{SIZE} bytes, {SIZE / 1024.0 / 1024.0 / 1024.0} GB]");
            while (true)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter) { break; }
            }
            UnmanagedArray<byte> arrUm = new UnmanagedArray<byte>(SIZE);

            arrUm[0] = 255;

            Console.WriteLine("Press enter to release Memory!");
            while (true)
            {
                if(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter) { break; }
            }
            Console.ReadKey();
            arrUm.Free();

            Console.ReadKey();
            const int ARR_SIZE = 1024 << 4;
            const int TEST_COUNT = 1000;

            long total = 0;
            long min = 0;
            long max = 0;
            unsafe
            {
                Random rng = new Random();
                int* stackArr = stackalloc int[ARR_SIZE];
                int* stackUMArr = stackalloc int[ARR_SIZE];
                IntComparer comp = new IntComparer();

                UnmanagedArray<int> umArr = new UnmanagedArray<int>(ARR_SIZE);
                UnmanagedArray<int> umArrStack = new UnmanagedArray<int>(stackUMArr, ARR_SIZE);
                int[] arr = new int[ARR_SIZE];

                #region JIT
                Console.WriteLine("Performing Randomizations & Sorts to run JIT...");
                for (int i = 0; i < ARR_SIZE; i++)
                {
                    umArr[i] = i;
                    stackUMArr[i] = i;
                    arr[i] = i;
                    stackArr[i] = i;
                }

                Randomize(umArr);
                Randomize(umArrStack);
                Randomize(arr);
                RandomizePtr(stackArr, ARR_SIZE);

                umArr.Sort(comp);
                umArrStack.Sort(comp);
                Array.Sort(arr, comp);
                UnmanagedArray.Sort(stackArr, ARR_SIZE, 0, ARR_SIZE, comp);
                #endregion

                Console.WriteLine($"Starting Tests [{TEST_COUNT} tests, {ARR_SIZE} elements]...\n");
                TimeSpan span;
                total = 0;
                min = long.MaxValue;
                max = 0;

                for (int i = 0; i < TEST_COUNT; i++)
                {
                    Randomize(umArr);
                    sw.Restart();
                    umArr.Sort(comp);
                    sw.Stop();

                    total += sw.ElapsedTicks;
                    min = sw.ElapsedTicks < min ? sw.ElapsedTicks : min;
                    max = sw.ElapsedTicks > max ? sw.ElapsedTicks : max;
                }
                WriteTime("Unmanaged Array Sort (Heap)");
                WriteSummary(umArr, 10);

                total = 0;
                min = long.MaxValue;
                max = 0;

                for (int i = 0; i < TEST_COUNT; i++)
                {
                    Randomize(umArr);
                    sw.Restart();
                    umArrStack.Sort(comp);
                    sw.Stop();

                    total += sw.ElapsedTicks;
                    min = sw.ElapsedTicks < min ? sw.ElapsedTicks : min;
                    max = sw.ElapsedTicks > max ? sw.ElapsedTicks : max;
                }
                WriteTime("Unmanaged Array Sort (Stack)");
                WriteSummary(umArrStack, 10);

                total = 0;
                min = long.MaxValue;
                max = 0;

                for (int i = 0; i < TEST_COUNT; i++)
                {
                    Randomize(umArr);
                    sw.Restart();
                    Array.Sort(arr, comp);
                    sw.Stop();

                    total += sw.ElapsedTicks;
                    min = sw.ElapsedTicks < min ? sw.ElapsedTicks : min;
                    max = sw.ElapsedTicks > max ? sw.ElapsedTicks : max;
                }
                WriteTime("Raw Array Sort (Heap)");
                WriteSummary(arr, 10);

                total = 0;
                min = long.MaxValue;
                max = 0;

                for (int i = 0; i < TEST_COUNT; i++)
                {
                    Randomize(umArr);
                    sw.Restart();
                    UnmanagedArray.Sort(stackArr, ARR_SIZE, 0, ARR_SIZE, comp);
                    sw.Stop();

                    total += sw.ElapsedTicks;
                    min = sw.ElapsedTicks < min ? sw.ElapsedTicks : min;
                    max = sw.ElapsedTicks > max ? sw.ElapsedTicks : max;
                }
                WriteTime("Raw Array Sort (Stack)");
                WriteSummaryPtr(stackArr, 10);

                void WriteTime(string header)
                {
                    span = TimeSpan.FromTicks(total);
                    Console.WriteLine($"|'{header}' [{TEST_COUNT} tests, {ARR_SIZE} elements]");
                    Console.WriteLine($"|----Tot: {span.TotalSeconds:F4} sec, {span.TotalMilliseconds:F0} ms, {span.Ticks:F0} ticks");

                    span = TimeSpan.FromTicks((total / TEST_COUNT));
                    Console.WriteLine($"|----Avg: {span.TotalSeconds:F4} sec, {span.TotalMilliseconds:F0} ms, {span.Ticks:F0} ticks");

                    span = TimeSpan.FromTicks((min / TEST_COUNT));
                    Console.WriteLine($"|----Min: {span.TotalSeconds:F4} sec, {span.TotalMilliseconds:F0} ms, {span.Ticks:F0} ticks");

                    span = TimeSpan.FromTicks((max / TEST_COUNT));
                    Console.WriteLine($"|----Max: {span.TotalSeconds:F4} sec, {span.TotalMilliseconds:F0} ms, {span.Ticks:F0} ticks");
                }

                void WriteSummary(IList<int> list, int toPrint)
                {
                    Console.WriteLine(new string('=', 48));
                    for (int i = 0; i < toPrint; i++)
                    {
                        Console.WriteLine($"|{list[i].ToString().PadRight(46, ' ')}|");
                    }
                    Console.WriteLine(new string('=', 48) + "\n");
                }

                void WriteSummaryPtr(int* list, int toPrint)
                {
                    Console.WriteLine(new string('=', 48));
                    for (int i = 0; i < toPrint; i++)
                    {
                        Console.WriteLine($"|{list++->ToString().PadRight(46, ' ')}|");
                    }
                    Console.WriteLine(new string('=', 48) + "\n");
                }


                void Randomize(IList<int> list)
                {
                    int n = list.Count;
                    while (n > 1)
                    {
                        int k = rng.Next(n--);
                        int temp = list[n];
                        list[n] = list[k];
                        list[k] = temp;
                    }
                }

                void RandomizePtr(int* list, int length)
                {
                    int n = length;
                    while (n > 1)
                    {
                        int k = rng.Next(n--);
                        int temp = list[n];
                        list[n] = list[k];
                        list[k] = temp;
                    }
                }
            }

            Console.ReadKey();

            string path = "";
            startMp3:
            Console.Clear();
            Console.WriteLine("Enter the full path of the MP3 file");
            path = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(path)) { goto startMp3; }

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                var res = Mp3Decoder.IsFileMp3(stream, out var hdr);

                if (res)
                {
                    Console.WriteLine($"Mp3 Decode Successful!\n\n{hdr.ToString()}\n");
                }
                else
                {
                    Console.WriteLine($"Mp3 Decode Failed!");
                }

            }

            Console.ReadKey();

            startOgg:
            Console.Clear();
            Console.WriteLine("Enter the full path of the OGG file");
            path = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(path)) { goto startOgg; }

            using(FileStream stream = new FileStream(path, FileMode.Open))
            using(OggDecoder oggDec = new OggDecoder(stream))
            {
                var res = oggDec.Decode();
                switch (res)
                {
                    case AudioDecodeResult.Success:
                        Console.WriteLine($"Ogg Decode Successful!");
                        break;
                    default:
                        Console.WriteLine($"Ogg Decode Failed [{res}]!");
                        break;
                }
            }

            Console.ReadKey();


            startTEXTURE:
            Console.Clear();
            Console.WriteLine("Enter the full path of the PNG");
            path = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(path)) { goto startTEXTURE; }

            using (FileStream fs = new FileStream(path, FileMode.Open))
            using (PNGDecoder png = new PNGDecoder(fs))
            {
                png.LoadGeneralInformation(fs.Position);
                int dataCRC = png.GetDataCRC(fs.Position);

                Console.WriteLine($"\n-|PNG General Info [{png.Width}x{png.Height}, {png.BitsPerPixel}, {png.ColorMode}]");
                Console.WriteLine($"-|--Data CRC [All]: 0x{Convert.ToString(dataCRC, 16).PadLeft(8, '0')}");
                Console.WriteLine($"-|-------------------------------------------------------------------------");

                sw.Restart();
                var res = png.Decode(false);
                sw.Stop();
                Console.WriteLine($"-|PNG Decode Done [{res}]");
                Console.WriteLine($"-|-{sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");
                Console.WriteLine($"-|---------------------------------------------------------------------------");

                if (res == ImageDecodeResult.Success)
                {
                    using(FileStream testOut = new FileStream($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_OUT.png", FileMode.Create))
                    using(PNGEncoder enc = new PNGEncoder(png.GetTexture(), TextureFormat.Indexed))
                    {
                        enc.Flags = ImageDecoderFlags.ForcePalette | ImageDecoderFlags.AllowBigIndices;

                        var encR = enc.Encode(testOut, false);
                        switch (encR)
                        {
                            default: Console.WriteLine($"Encode Failed! [{encR}]"); break;
                            case ImageEncodeResult.Success:
                                Console.WriteLine($"Encode Success!");
                                break;
                        }
                    }

                    Console.ReadKey();
                    //Texture tex = png.GetTexture();
                    //FastColor[] temp = new FastColor[tex.Width * tex.Height];

                    //TextureDataMode[] all = Enum.GetValues(typeof(TextureDataMode)) as TextureDataMode[];
                    //const int TEST_COUNT = 256;

                    //long min = long.MaxValue;
                    //long max = 0;
                    //long total = 0;
                    //double avg = 0;
                    //int y = Console.CursorTop;

                    //foreach (var mode in all)
                    //{
                    //    min = long.MaxValue;
                    //    max = 0;
                    //    total = 0;

                    //    y = Console.CursorTop;

                    //    tex.PixelIterationMode = mode;
                    //    if (tex.PixelIterationMode != mode) { continue; }

                    //    Console.SetCursorPosition(0, y);
                    //    Console.Write($"-|Test[{tex.PixelIterationMode }] {0}/{TEST_COUNT}".PadRight(64, ' '));
                    //    for (int i = 0; i < TEST_COUNT; i++)
                    //    {
                    //        sw.Restart();
                    //        tex.GetPixels(temp);
                    //        sw.Stop();
                    //        var l = sw.ElapsedTicks;
                    //        min = l < min ? l : min;
                    //        max = l > max ? l : max;
                    //        total += l;

                    //        Console.SetCursorPosition(0, y);
                    //        Console.Write($"-|Test[{tex.PixelIterationMode }] {i + 1}/{TEST_COUNT}".PadRight(64, ' '));
                    //    }
                    //    Console.SetCursorPosition(0, y);

                    //    avg = total / (double)TEST_COUNT;
                    //    Console.WriteLine($"-|-Copy Pixels to FastColor Array VIA '{tex.PixelIterationMode}' [{TEST_COUNT} times]:".PadRight(64, ' '));
                    //    Console.WriteLine($"-|   -Max: {((max / 10000.0) / 1000.0):F4} sec, {(long)((max / 10000.0))} ms, {max} ticks");
                    //    Console.WriteLine($"-|   -Min: {((min / 10000.0) / 1000.0):F4} sec, {(long)((min / 10000.0))} ms, {min} ticks");
                    //    Console.WriteLine($"-|   -Avg: {((avg / 10000.0) / 1000.0):F4} sec, {(long)((avg / 10000.0))} ms, {avg} ticks");
                    //    Console.WriteLine($"-|-----------------------------------------------------");
                    //}

                    //try
                    //{
                    //    //using (FileStream fsP = new FileStream($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_PNG.png", FileMode.Create))
                    //    //using (PNGEncoder pngEnc = new PNGEncoder(png.Width, png.Height, png.ColorMode))
                    //    //{
                    //    //    //pngEnc.Flags = ImageDecoderFlags.ForceRGB;
                    //    //    var pix = png.GetPixelsRef();
                    //    //    pngEnc.SetPixelsRef(ref pix);

                    //    //    var pngRes = pngEnc.Encode(fsP, true);
                    //    //    Console.WriteLine($"PNG Encode Done [{pngRes}]");

                    //    //    //sw = new Stopwatch();
                    //    //    //sw.Start();
                    //    //    //unsafe
                    //    //    //{
                    //    //    //    byte* ptr = (byte*)tex.LockBits();
                    //    //    //    int bpp = tex.BitsPerPixel >> 3;
                    //    //    //    for (int y = 0; y < tex.Height; y++)
                    //    //    //    {
                    //    //    //        int yP = y * tex.ScanSize;
                    //    //    //        ptr = (byte*)(tex.Scan + yP);
                    //    //    //        byte yS = (byte)Maths.Lerp(0, 255, (y / (tex.Height - 1.0f)));
                    //    //    //        for (int x = 0; x < tex.Width; x++)
                    //    //    //        {
                    //    //    //            FastColor cc = new FastColor(yS, (byte)Maths.Lerp(0, 255, (x / (tex.Width - 1.0f))), 0);
                    //    //    //            for (int i = 0; i < bpp; i++)
                    //    //    //            {
                    //    //    //                *ptr = cc[i];
                    //    //    //                ptr++;
                    //    //    //            }
                    //    //    //        }
                    //    //    //    }
                    //    //    //    tex.UnlockBits();
                    //    //    //}
                    //    //    //sw.Stop();
                    //    //    //Console.WriteLine($"{sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");

                    //    //    //pix = tex.GetPixels();
                    //    //    //pngEnc.SetPixelsRef(ref pix);

                    //    //    //var pngRes = pngEnc.Encode(fsPGR, true);
                    //    //    //Console.WriteLine($"PNG Lock Bits Done [{pngRes}]");


                    //    //    //sw.Restart();
                    //    //    //for (int y = 0; y < tex.Height; y++)
                    //    //    //{
                    //    //    //    int yP = y * tex.ScanSize;
                    //    //    //    byte yS = (byte)Maths.Lerp(0, 255, (y / (tex.Height - 1.0f)));
                    //    //    //    for (int x = 0; x < tex.Width; x+=2)
                    //    //    //    {
                    //    //    //        FastColor cc = new FastColor(yS, (byte)Maths.Lerp(0, 255, (x / (tex.Width - 1.0f))), 0);
                    //    //    //        tex.SetPixel(yS + x, cc);

                    //    //    //        if(!isEven && x==tex.Width - 1) { continue; }

                    //    //    //        cc = new FastColor(yS, (byte)Maths.Lerp(0, 255, ((x + 1) / (tex.Width - 1.0f))), 0);
                    //    //    //        tex.SetPixel(yS + x + 1, cc);
                    //    //    //    }
                    //    //    //}
                    //    //    //sw.Stop();
                    //    //    //Console.WriteLine($"{sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");

                    //    //    //pix = tex.GetPixels();
                    //    //    //pngEnc.SetPixelsRef(ref pix);

                    //    //    //pngRes = pngEnc.Encode(fsP, true);
                    //    //    //Console.WriteLine($"PNG SetPixel Done [{pngRes}]");
                    //    //}
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine(e.Message);
                    //}
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
            //            //pngEnc.SetFlags(PNGFlags.ForceRGB);
            //            //var pix = jpeg.GetPixelsRef();
            //            //pngEnc.SetPixelsRef(ref pix);
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
                        using (FileStream fsEnc    = new FileStream($"{dirP}/{namP}_PAL.png", FileMode.Create))
                        using (FileStream fsEncFilt = new FileStream($"{dirP}/{namP}_PAL_Filter.png", FileMode.Create))
                        using (FileStream fsEncFiltF = new FileStream($"{dirP}/{namP}_PAL_Forced_Filter.png", FileMode.Create))
                        using (FileStream fsEncBroken = new FileStream($"{dirP}/{namP}_PAL_Broken.png", FileMode.Create))
                        using (PNGEncoder encPNG = new PNGEncoder(decPNG.ColorMode))
                        {
                            //var pix = decPNG.GetPixelsRef();
                           // encPNG.SetPixelsRef(ref pix);

                            encPNG.Flags = ImageDecoderFlags.AllowBigIndices | ImageDecoderFlags.ForceRGB;
                            encPNG.PNGFlags = PNGFlags.UseBrokenSubFilter | PNGFlags.OverrideFilter;
                            encPNG.SetOverrideFilter(PNGFilterMethod.Average);

                            var resEnc = encPNG.Encode(fsEnc, false);
                            switch (resEnc)
                            {
                                default: Console.WriteLine($"PNG Encode Failed: [{resEnc}, {encPNG.Flags}, {encPNG.PNGFlags}]"); break;
                                case ImageEncodeResult.Success:
                                    Console.WriteLine($"PNG Encode [{resEnc}, {encPNG.Flags}, {encPNG.PNGFlags}]!");
                                    break;
                            }

                            encPNG.Flags = (ImageDecoderFlags.AllowBigIndices | ImageDecoderFlags.ForcePalette  /*| PNGFlags.OverrideFilter*/);
                            encPNG.PNGFlags = PNGFlags.ForceFilter;
                            //encPNG.SetOverrideFilter(PNGFilterMethod.Paeth);

                            resEnc = encPNG.Encode(fsEncFilt, false);
                            switch (resEnc)
                            {
                                default: Console.WriteLine($"PNG Encode Failed: [{resEnc}, {encPNG.Flags}, {encPNG.PNGFlags}]"); break;
                                case ImageEncodeResult.Success:
                                    Console.WriteLine($"PNG Encode [{resEnc}, {encPNG.Flags}, {encPNG.PNGFlags}]!");
                                    break;
                            }

                            encPNG.Flags = (ImageDecoderFlags.AllowBigIndices | ImageDecoderFlags.ForcePalette /*| PNGFlags.OverrideFilter*/);
                            encPNG.PNGFlags = PNGFlags.ForceFilter | PNGFlags.OverrideFilter;
                            encPNG.SetOverrideFilter(PNGFilterMethod.Paeth);

                            resEnc = encPNG.Encode(fsEncFiltF, false);
                            switch (resEnc)
                            {
                                default: Console.WriteLine($"PNG Encode Failed: [{resEnc}, {encPNG.Flags}, {encPNG.PNGFlags}]"); break;
                                case ImageEncodeResult.Success:
                                    Console.WriteLine($"PNG Encode [{resEnc}, {encPNG.Flags}, {encPNG.PNGFlags}]!");
                                    break;
                            }

                            encPNG.Flags = (ImageDecoderFlags.AllowBigIndices | ImageDecoderFlags.ForcePalette/*| PNGFlags.OverrideFilter*/);
                            encPNG.PNGFlags = PNGFlags.None;
                            //encPNG.SetOverrideFilter(PNGFilterMethod.Paeth);

                            resEnc = encPNG.Encode(fsEncBroken, false);
                            switch (resEnc)
                            {
                                default: Console.WriteLine($"PNG Encode Failed: [{resEnc}, {encPNG.Flags}, {encPNG.PNGFlags}]"); break;
                                case ImageEncodeResult.Success:
                                    Console.WriteLine($"PNG Encode [{resEnc}, {encPNG.Flags}, {encPNG.PNGFlags}]!");
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

                //png.GetPixels(pixels);
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
