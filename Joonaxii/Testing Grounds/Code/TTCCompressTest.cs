using Joonaxii.IO;
using Joonaxii.Text.Compression;
using System;
using System.Diagnostics;
using System.IO;

namespace Testing_Grounds
{
    public class TTCCompressTest : MenuItem
    {
        public TTCCompressTest(string name, bool enabled = true) : base(name, enabled) { }

        public override bool OnClick()
        {
            Console.WriteLine("Do you want to load the text from a file? (Y/N)");
            bool fromFile = false;

            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Y)
                {
                    fromFile = true;
                    break;
                }
                if (key == ConsoleKey.N) { break; }
            }

        file:
            Console.WriteLine(fromFile ? "Please enter the path to the text file that should be read" : "Please enter a string you'd like to compress");
            string compressable = "";

            string path = "";
            if (fromFile)
            {
                path = Console.ReadLine().Replace("\"", "");
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Path '{path}' is invalid!");
                    goto file;
                }
                compressable = File.ReadAllText(path);
            }
            else
            {
                compressable = Console.ReadLine();
            }

            int origSize = compressable.Length * sizeof(char);

            Stopwatch sw = new Stopwatch();
            byte[] data = null;

            int compressedSize = origSize;
          
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                sw.Start();
                TTC.Compress(compressable, bw);
                sw.Stop();

                data = stream.ToArray();
                if (fromFile)
                {
                    string pathSave = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_COMPRESSED.dat";
                    File.WriteAllBytes(pathSave, data);
                    Console.WriteLine($"\nSaved compressed string to '{pathSave}'");
                }

                compressedSize = data.Length;
            }  
            Console.WriteLine($"Text Compression took: {sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks\n");
          
            string decompressed = "";
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(stream))
            {
                sw.Restart();
                decompressed = TTC.Decompress(br);
                sw.Stop();

                if (fromFile)
                {
                    string pathSave = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_DECOMPRESSED{Path.GetExtension(path)}";
                    File.WriteAllText(pathSave, decompressed);
                    Console.WriteLine($"Saved decompressed string to '{pathSave}'");
                }
            }
           
            Console.WriteLine($"Text Decompression took: {sw.Elapsed} sec, {sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");
            float percent = compressedSize / (float)origSize;
        
            Console.WriteLine($"\nFile Size Results: [{((1.0f - percent) * 100.0f).ToString("F2")}% saved!]");
            Console.WriteLine($"Original Size: [{IOExtensions.GetFileSizeString(origSize)}]");
            Console.WriteLine($"Compressed Size: [{IOExtensions.GetFileSizeString(compressedSize)}]");

            if (!fromFile)
            {
                Console.WriteLine($"\nOriginal String: {compressable}");
                Console.WriteLine($"Decompressed String: {decompressed}");
            }

            data = null;
            GC.Collect();

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