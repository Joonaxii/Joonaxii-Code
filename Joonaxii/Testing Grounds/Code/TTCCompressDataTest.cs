using Joonaxii.IO;
using Joonaxii.IO.BitStream;
using Joonaxii.Text.Compression;
using System;
using System.IO;

namespace Testing_Grounds
{
    public class TTCCompressDataTest : MenuItem
    {
        public TTCCompressDataTest(string name, bool enabled = true) : base(name, enabled)
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

                if (key == ConsoleKey.Y) { useBitStream = true; break; }
                if (key == ConsoleKey.N) { break; }
            }

            if (useBitStream)
            {
                using (FileStream stream = new FileStream(path, FileMode.Open))
                using (BitReader br = new BitReader(stream))
                {
                    string str = br.ReadString();
                    Console.WriteLine($"Read string: {str}");
                    TTC.Decompress(br);
                }

                Console.WriteLine($"\nPress enter to go back to the menu.");
                while (true)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Enter) { break; }
                }
                return true;
            }

            using (FileStream stream = new FileStream(path, FileMode.Open))
            using (BinaryReader br = new BinaryReader(stream))
            {
                string str = br.ReadString();
                Console.WriteLine($"Read string: {str}");
                TTC.Decompress(br);
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