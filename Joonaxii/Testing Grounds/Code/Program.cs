using Joonaxii.Debugging;
using Joonaxii.IO;
using Joonaxii.Text.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Testing_Grounds
{
    public class Program
    {
        private static MenuItem[] _menu = new MenuItem[]
        {
            new TTCCompressTest("TTC Compression/Decompression"),

            new LZWTest("LZW Compression/Decompression"),
            new LZWTestBinaryRW("LZW Compression/Decompression Binary R/W"),

            new Bit256Test("Bit256 Testing"),

            new QuitItem(),
        };

        private static int _selectedItem = 0;

        public static void Main(string[] args)
        {

            FileDebugger debug;
            string testThing = "This is a test string! ";

            int originalSize = testThing.Length;
            int compressedSize = 0;

            int max = 0;
            List<int> codes = new List<int>();
            for (int i = 0; i < testThing.Length; i++)
            {
                var a = testThing[i];
                codes.Add(a);
                max = max < a ? a : max;
            }

            byte[] test = new byte[0];
            using(MemoryStream streamIn = new MemoryStream())
            using(BinaryWriter bw = new BinaryWriter(streamIn))
            {
                debug = new FileDebugger("Huffman Test", streamIn);
                Huffman.CompressToStream(codes, (byte)IOExtensions.BitsNeeded(max), bw, null, debug);
                test = streamIn.ToArray();
                compressedSize = test.Length;

                File.WriteAllBytes("Test Stuff.dat", test);
            }
            Console.WriteLine($"Compressed Huffman [{testThing}]");

            Console.WriteLine($" -Original size: {originalSize} bytes");
            Console.WriteLine($" -Compressed size: {compressedSize} bytes");
            Console.WriteLine($"\n{debug.ToString()}");

            codes.Clear();
            using (MemoryStream streamOut = new MemoryStream(test))
            using (BinaryReader br = new BinaryReader(streamOut))
            {
                Huffman.DecompressFromStream(codes, br);
            }
            testThing = "";
            for (int i = 0; i < codes.Count; i++)
            {
                testThing += (char)codes[i];
            }

            Console.WriteLine($"Decompressed Huffman [{testThing}]");

            Console.ReadKey();

            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            _selectedItem = MoveSelection(0, 0);
            HandleMenu();
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
                        if(!item.OnClick())
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

                if(input == start) { break; }
                itm = _menu[input];
            }
            return input;
        }
    }
}
