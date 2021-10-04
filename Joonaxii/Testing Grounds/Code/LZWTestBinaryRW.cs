using Joonaxii.IO;
using Joonaxii.Text.Compression;
using System;
using System.IO;

namespace Testing_Grounds
{
    public class LZWTestBinaryRW : MenuItem
    {
        public LZWTestBinaryRW(string name, bool enabled = true) : base(name, enabled)
        {
        }

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

            byte[] bytes = null;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                LZW.Compress(compressable, bw);
                bytes = stream.ToArray();

                if (fromFile)
                {
                    string pathSave = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_COMPRESSED.txt";
                    File.WriteAllBytes(pathSave, bytes);
                    Console.WriteLine($"Saved compressed string to '{pathSave}'");
                }
            }

            string decompressed = "";

            int count = 0;
            byte size = 0;
            ushort charLimit = 0;

            int[] data = null;

            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader br = new BinaryReader(stream))
            {
                decompressed = new string(LZW.Decompress(br));

                br.BaseStream.Position = 0;

                br.ReadBytes(3);

                count = br.ReadInt32();
                size = br.ReadByte();
                charLimit = br.ReadUInt16();

                data = new int[count];
                switch (size)
                {
                    default:
                        for (int i = 0; i < count; i++)
                        {
                            data[i] = br.ReadByte();
                        }
                        break;
                    case 2:
                        for (int i = 0; i < count; i++)
                        {
                            data[i] = br.ReadUInt16();
                        }
                        break;
                    case 4:
                        for (int i = 0; i < count; i++)
                        {
                            data[i] = br.ReadInt32();
                        }
                        break;
                }
            }

            if (fromFile)
            {
                Console.WriteLine($"\nOriginal String [{compressable.Length} chars]");

                Console.WriteLine($"\nCompressed String: ");
                Console.WriteLine($"Header: [{count}, {size}, {charLimit}]");

                Console.WriteLine($"\nDecompressed String [{decompressed.Length} chars]");

                Console.WriteLine($"\nSize of original string (Inc. length (4 bytes)): {compressable.Length * compressable.GetCharSize() + (4)} bytes");
                Console.WriteLine($"Size of compressed string (Inc. Header ({LZW.HEADER_SIZE} bytes)): {data.Length * size + (LZW.HEADER_SIZE)} bytes");
            }
            else
            {
                Console.WriteLine($"\nOriginal String [{compressable}]");

                Console.WriteLine($"\nCompressed String: ");
                Console.WriteLine($"Header: [{count}, {size}, {charLimit}]");
                Console.WriteLine($"Data: [{string.Join(",", data)}]");

                Console.WriteLine($"\nDecompressed String [{decompressed}]");

                Console.WriteLine($"\nSize of original string (Inc. length (4 bytes)): {compressable.Length * compressable.GetCharSize() + (4)} bytes");
                Console.WriteLine($"Size of compressed string (Inc. Header ({LZW.HEADER_SIZE} bytes)): {data.Length * size + (LZW.HEADER_SIZE)} bytes");
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