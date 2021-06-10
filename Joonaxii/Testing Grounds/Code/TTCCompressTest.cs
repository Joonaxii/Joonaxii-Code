using Joonaxii.Debugging;
using Joonaxii.IO;
using Joonaxii.Text.Compression;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

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

            int origSize = compressable.Length * compressable.GetCharSize();

            byte[] data = null;

            int compressedSize = origSize;

            string elapsed = "";

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(stream))
            using (TimeStamper ts = new TimeStamper("TTC (Compression)"))
            {
                TTC.Compress(compressable, bw, ts);

                data = stream.ToArray();
                if (fromFile)
                {
                    string pathSave = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_COMPRESSED.dat";
                    File.WriteAllBytes(pathSave, data);
                    Console.WriteLine($"\nSaved compressed string to '{pathSave}'");
                }
                else
                {
                    bool isTTC;
                    if ((isTTC = CompressionHelpers.IsTTC(data)) || CompressionHelpers.IsLZW(data))
                    {
                        StringBuilder sb = new StringBuilder();
                        if (isTTC)
                        {
                            sb.Append($"Header: [{(char)data[0]},{(char)data[1]},{(char)data[2]}, {data[3]}, {data.ReadInt(4)}, {data.ReadInt(8)}]\n");
                            sb.Append($"Data: [");
                            for (int i = TTC.HEADER_SIZE; i < data.Length; i++)
                            {
                                sb.Append($"{(i < data.Length - 1 ? $"{data[i]}," : $"{data[i]}")}");
                            }
                            sb.Append($"]");
                        }
                        else
                        {
                            sb.Append($"Header: [{(char)data[0]},{(char)data[1]},{(char)data[2]}, {data.ReadInt(3)}, {data[7]}, {data.ReadUshort(8)}]\n");
                            sb.Append($"Data: [");
                            for (int i = LZW.HEADER_SIZE; i < data.Length; i++)
                            {
                                sb.Append($"{(i < data.Length - 1 ? $"{data[i]}," : $"{data[i]}")}");
                            }
                            sb.Append($"]");
                        }
                        Console.WriteLine($"\n{sb.ToString()}");
                    }
                }
                compressedSize = data.Length;
                elapsed = ts.ToString();
            }
            Console.WriteLine($"{elapsed}\n");

            string decompressed = "";

            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(stream))
            using (TimeStamper ts = new TimeStamper("TTC (Decompression)"))
            {
                decompressed = TTC.Decompress(br, ts);
                if (fromFile)
                {
                    string pathSave = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_DECOMPRESSED{Path.GetExtension(path)}";
                    File.WriteAllText(pathSave, decompressed);
                    Console.WriteLine($"Saved decompressed string to '{pathSave}'");
                }
                elapsed = ts.ToString();
            }

            Console.WriteLine($"{elapsed}");
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

        public void TTCCompress()
        {
            byte[] data;

            string testString = "This is a test";
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                TTC.Compress(testString, bw);
                data = stream.ToArray();
            }
            //Result:
            //Header: [T,T,C, 1, 4, 4]
            //Data: [84,79,75,1,0,0,0,0,4,0,0,0,5,0,0,0,84,104,105,115,32,3,0,0,0,105,115,32,2,0,0,0,97,32,4,0,0,0,116,101,115,116,0,1,2,3]
        }

        public void TTCDecompress()
        {
            byte[] data = new byte[] { 84, 84, 67, 1, 4, 4, 84, 79, 75, 1, 0, 0, 0, 0, 4, 0, 0, 0, 5, 0, 0, 0, 84, 104, 105, 115, 32, 3, 0, 0, 0, 105, 115, 32, 2, 0, 0, 0, 97, 32, 4, 0, 0, 0, 116, 101, 115, 116, 0, 1, 2, 3 };

            string testString;
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(stream))
            {
                testString = TTC.Decompress(br);
            }
            //Result: This is a test
        }
    }
}