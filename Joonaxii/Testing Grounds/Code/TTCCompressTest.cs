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
            bool compress = false;
            IndexCompressionMode compMode = IndexCompressionMode.None;

            while (true)
            {
                ConsoleKey key = Console.ReadKey(false).Key;
                if (key == ConsoleKey.Y)
                {
                    fromFile = true;
                    break;
                }
                if (key == ConsoleKey.N) { break; }
            }

            Console.WriteLine("\nDo you want to compress and decompress or just decompress? (Y/N)");
            while (true)
            {
                ConsoleKey key = Console.ReadKey(false).Key;
                if (key == ConsoleKey.Y)
                {
                    compress = true;
                    break;
                }
                if (key == ConsoleKey.N) { break; }
            }

            bool asBytes = false;

            if (fromFile)
            {
                Console.WriteLine("\nDo you want to load the text as bytes or just plain ol' text? (Y/N)");
                while (true)
                {
                    ConsoleKey key = Console.ReadKey(false).Key;
                    if (key == ConsoleKey.Y)
                    {
                        asBytes = true;
                        break;
                    }
                    if (key == ConsoleKey.N) { break; }
                }
            }

            if (compress)
            {
                Console.WriteLine("\nWhat type of Index compression would you like to use?\n  -None: 0\n  -LZW: 1\n  -Huffman: 2");
                while (true)
                {
                    bool selected = false;
                    ConsoleKey key = Console.ReadKey(false).Key;
                    switch (key)
                    {
                        case ConsoleKey.D0:
                            selected = true;
                            break;
                        case ConsoleKey.D1:
                            selected = true;
                            compMode = IndexCompressionMode.LZW;
                            break;
                        case ConsoleKey.D2:
                            selected = true;
                            compMode = IndexCompressionMode.Huffman;
                            break;
                    }
                    if (selected) { break; }
                }
            }

        file:
            Console.WriteLine(fromFile ? "\nPlease enter the path to the text file that should be read" : "Please enter a string you'd like to compress");
            string compressable = "";
            byte[] dataASBytes = null;

            int origSize = 0;
            byte[] data = null;

            string path = "";
            if (fromFile)
            {
                path = Console.ReadLine().Replace("\"", "");
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Path '{path}' is invalid!");
                    goto file;
                }

                if (!compress)
                {
                    data = File.ReadAllBytes(path);
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

                if (!compress)
                {
                    data = Encoding.UTF8.GetBytes(compressable);
                }
            }


            int compressedSize = origSize;

            string elapsed = "";

            if (compress)
            {
                FileDebugger debug;
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(stream))
                using (TimeStamper ts = new TimeStamper("TTC (Compression)"))
                {
                    long pos = bw.BaseStream.Position;
                    debug = new FileDebugger("TTC Compression", stream);

                    if (asBytes)
                    {
                        TTC.Compress(dataASBytes, bw, compMode, ts);
                    }
                    else
                    {
                        TTC.Compress(compressable, bw, compMode, ts, debug);
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
                        if ((isTTC = CompressionHelpers.IsTTC(data, pos)) || CompressionHelpers.IsLZW(data, pos))
                        {
                            StringBuilder sb = new StringBuilder();
                            if (isTTC)
                            {
                                sb.Append($"Header: [{(char)data[0 + pos]},{(char)data[1 + pos]},{(char)data[2 + pos]}, {data[3 + pos]}, {data.ReadInt(4 + (int)pos)}, {data.ReadInt(8 + (int)pos)}]\n");
                                sb.Append($"Data: [");
                                for (long i = TTC.HEADER_SIZE + pos; i < data.Length; i++)
                                {
                                    sb.Append($"{(i < data.Length - 1 ? $"{data[i]}," : $"{data[i]}")}");
                                }
                                sb.Append($"]");
                            }
                            else
                            {
                                sb.Append($"Header: [{(char)data[0 + pos]},{(char)data[1 + pos]},{(char)data[2 + pos]}, {data.ReadInt(3 + (int)pos)}, {data[7 + pos]}, {data.ReadUshort(8 + (int)pos)}]\n");
                                sb.Append($"Data: [");
                                for (long i = LZW.HEADER_SIZE + pos; i < data.Length; i++)
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

                    Console.WriteLine(debug.ToString());
                }
                Console.WriteLine($"{elapsed}\n");
            }
            string decompressed = "";

            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(stream))
            using (TimeStamper ts = new TimeStamper("TTC (Decompression)"))
            {
                //br.ReadString();
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
            Console.WriteLine($"Compressed Size: [{IOExtensions.GetFileSizeString(compressedSize)}] with index compression mode '{compMode}'");

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