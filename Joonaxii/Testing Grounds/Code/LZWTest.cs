using Joonaxii.IO;
using Joonaxii.Text.Compression;
using System;
using System.Collections.Generic;
using System.IO;

namespace Testing_Grounds
{
    public class LZWTest : MenuItem
    {
        public LZWTest(string name, bool enabled = true) : base(name, enabled)
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

            List<int> compressed = LZW.Compress(compressable);
            string decompressed = LZW.Decompress(compressed);

            if (fromFile)
            {
                using(MemoryStream stream = new MemoryStream())
                using(BinaryWriter bw = new BinaryWriter(stream))
                {
                    for (int i = 0; i < compressed.Count; i++)
                    {
                        bw.Write(compressed[i]);
                    }

                    string pathSave = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}_COMPRESSED.txt";
                    File.WriteAllBytes(pathSave, stream.ToArray());
                    Console.WriteLine($"Saved compressed string to '{pathSave}'");
                }

                Console.WriteLine($"\nOriginal String [{compressable.Length} chars]");
                Console.WriteLine($"Compressed String [{compressed.Count} chars/groups]");

                Console.WriteLine($"\nSize of original string: {compressable.Length * compressable.GetCharSize()} bytes");
                Console.WriteLine($"Size of compressed string: {compressed.Count * sizeof(int)} bytes");
            }
            else
            {
                Console.WriteLine($"\nOriginal String [{compressable}]");
                Console.WriteLine($"Compressed String [{string.Join(",", compressed)}]");


                Console.WriteLine($"\nDecompressed String [{decompressed}]");

                Console.WriteLine($"\nSize of original string: {compressable.Length * compressable.GetCharSize()} bytes");
                Console.WriteLine($"Size of compressed string: {compressed.Count * sizeof(int)} bytes");
            }

            Console.WriteLine($"\nPress enter to go back to the menu.");
            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if(key == ConsoleKey.Enter) { break; }
            }
            return true;
        }

        public void CompressLZW()
        {
            string testString = "This is a test";
            List<int> compressed = LZW.Compress(testString);

            //Result: [104, 105, 115, 32, 65538, 32, 97, 32, 116, 101, 115, 116]
        }

        public void DecompressLZW()
        {
            List<int> compressed = new List<int> { 104, 105, 115, 32, 65538, 32, 97, 32, 116, 101, 115, 116 };
            string decompressed = LZW.Decompress(compressed);

            //Result: This is a test
        }
    }
}