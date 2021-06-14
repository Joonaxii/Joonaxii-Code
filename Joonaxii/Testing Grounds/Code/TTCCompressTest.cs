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
            bool asBytes = false;

            if (fromFile)
            {
                Console.WriteLine("Do you want to load the text as bytes or just plain ol' text? (Y/N)");
                while (true)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Y)
                    {
                        asBytes = true;
                        break;
                    }
                    if (key == ConsoleKey.N) { break; }
                }
            }

        file:
            Console.WriteLine(fromFile ? "Please enter the path to the text file that should be read" : "Please enter a string you'd like to compress");
            string compressable = "";
            byte[] dataASBytes = null;

            int origSize = 0;

            string path = "";
            if (fromFile)
            {
                path = Console.ReadLine().Replace("\"", "");
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Path '{path}' is invalid!");
                    goto file;
                }
                if (asBytes)
                {
                    dataASBytes = File.ReadAllBytes(path);
                    origSize = dataASBytes.Length;
                }
                else
                {
                    compressable = File.ReadAllText(path);
                    origSize = compressable.Length * compressable.GetCharSize();
                }
            }
            else
            {
                compressable = Console.ReadLine();
                origSize = compressable.Length * compressable.GetCharSize();
            }

            byte[] data = null;
            int compressedSize = origSize;

            string elapsed = "";

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(stream))
            using (TimeStamper ts = new TimeStamper("TTC (Compression)"))
            {
                if (asBytes)
                {
                    TTC.Compress(dataASBytes, bw, ts);
                }
                else
                {
                    TTC.Compress(compressable, bw, ts);
                }

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
                if (asBytes)
                {
                    dataASBytes = TTC.DecompressAsData(br, ts);
                }
                else
                {
                    decompressed = TTC.Decompress(br, ts);
                }

                if (fromFile)
                {
                    string pathSave = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_DECOMPRESSED{Path.GetExtension(path)}";

                    if (asBytes)
                    {
                        File.WriteAllBytes(pathSave, dataASBytes);
                    }
                    else
                    {
                        File.WriteAllText(pathSave, decompressed);
                    }
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
            dataASBytes = null;
            compressable = null;
            elapsed = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

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