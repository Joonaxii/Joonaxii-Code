using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Joonaxii.Data.Compression.Huffman;
using Joonaxii.IO;
using Joonaxii.IO.BitStream;

namespace Testing_Grounds
{
    public class HuffmanCompressTest : MenuItem
    {
        public HuffmanCompressTest(string name, bool enabled = true) : base(name, enabled)
        {
        }


        public override bool OnClick()
        {
            file:
            Console.WriteLine("Please enter the path to the text file that should be read");

            string path;
            path = Console.ReadLine().Replace("\"", "");
            if (!File.Exists(path))
            {
                Console.WriteLine($"Path '{path}' is invalid!");
                goto file;
            }

            bool useBitStream = false;
            Console.WriteLine("Do you want to use the BitStream? (Y/N)");

            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                if(key == ConsoleKey.Y) { useBitStream = true; break; }
                if(key == ConsoleKey.N) { break; }
            }

            Stopwatch sw = new Stopwatch();

            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ogExt = Path.GetExtension(path);

            byte[] data = File.ReadAllBytes(path);
            string pathComp = $"{dir}/{name}_COMPRESSED.dat";

            byte paddedBytes;
            List<long> dataOut = new List<long>();

            if (useBitStream)
            {
                using (FileStream streamCmp = new FileStream(pathComp, FileMode.Create))
                using (BitWriter bw = new BitWriter(streamCmp))
                {
                    sw.Start();
                    Huffman.CompressToStream(bw, data, true, out paddedBytes);
                    sw.Stop();
                }
                Console.WriteLine($"Huffman Compression [BitStream] took: {sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");
                sw.Reset();

                using (FileStream streamCmp = new FileStream(pathComp, FileMode.Open))
                using (BitReader bw = new BitReader(streamCmp))
                {
                    sw.Start();
                    Huffman.DecompressFromStream(bw, dataOut);
                    sw.Stop();
                }
                Console.WriteLine($"Huffman Decompression [BitStream] took: {sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");
                sw.Reset();

                using (FileStream streamUncmp = new FileStream($"{dir}/{name}_DECOMPRESSED.{ogExt}", FileMode.Create))
                using (BinaryWriter bw = new BinaryWriter(streamUncmp))
                {
                    byte[] bytes = new byte[dataOut.Count];
                    for (int i = 0; i < dataOut.Count; i++)
                    {
                        long dataA = dataOut[i];
                        bytes[i] = (byte)dataA;
                    }
                    bw.Write(bytes);
                    bytes = null;
                }

                Console.WriteLine($"\nPress enter to go back to the menu.");
                while (true)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Enter) { break; }
                }
                return true;
            }

            using (FileStream streamCmp = new FileStream(pathComp, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(streamCmp))
            {
                sw.Start();
                Huffman.CompressToStream(bw, data, true, out paddedBytes);
                sw.Stop();
            }
            Console.WriteLine($"Huffman Compression took: {sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");
            sw.Reset();

            using (FileStream streamCmp = new FileStream(pathComp, FileMode.Open))
            using (BinaryReader bw = new BinaryReader(streamCmp))
            {
                sw.Start();
                Huffman.DecompressFromStream(bw, dataOut);
                sw.Stop();
            }
            Console.WriteLine($"Huffman Decompression took: {sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");
            sw.Reset();

            using (FileStream streamUncmp = new FileStream($"{dir}/{name}_DECOMPRESSED{ogExt}", FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(streamUncmp))
            {
                byte[] bytes = new byte[dataOut.Count];
                for (int i = 0; i < dataOut.Count; i++)
                {
                    long dataA = dataOut[i];
                    bytes[i] = (byte)dataA;
                }
                bw.Write(bytes);
                bytes = null;
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