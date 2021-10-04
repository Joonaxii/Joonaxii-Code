using Joonaxii.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Joonaxii.Stegaography;

namespace Testing_Grounds
{
    public class SteganographyTest : MenuItem
    {
        public SteganographyTest(string name, bool enabled = true) : base(name, enabled)
        {
        }

        public override bool OnClick()
        {
            Console.Clear();
            Console.WriteLine($"Select option: \n1: Embed a file into a file.\n2: Read embedded file from a file\nESC: Quit");
            while (true)
            {
                ConsoleKey k = Console.ReadKey(true).Key;
                if (k == ConsoleKey.D1)
                {
                    WriteFile();
                    Console.Clear();
                    Console.WriteLine($"Select option: \n1: Embed a file into a file.\n2: Read embedded file from a file\nESC: Quit");
                }

                if (k == ConsoleKey.D2)
                {
                    ReadFile();
                    Console.Clear();
                    Console.WriteLine($"Select option: \n1: Embed a file into a file.\n2: Read embedded file from a file\nESC: Quit");
                }

                if (k == ConsoleKey.Escape)
                {
                    break;
                }
            }

            return true;
        }

        private static void WriteFile()
        {
        start:
            Console.Clear();
            Console.WriteLine($"Enter the file you want to hide in a file");

            string fileA = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(fileA)) { goto start; }

            Console.Clear();
            Console.WriteLine($"Enter the number of least signifigant bits to use");

        bits:
            byte leastSigBits = 0;

            if (!byte.TryParse(Console.ReadLine(), out leastSigBits) || leastSigBits < 1 | leastSigBits > 32) { goto bits; }
            byte[] bitsToSave = Steganography.GetData(File.ReadAllBytes(fileA), leastSigBits, Path.GetFileName(fileA), out ulong requiredBits, out ulong requiredBytes);

        referenceFile:

            Console.Clear();
            Console.WriteLine($"Steganography at '{leastSigBits}' least significant bits \non '{requiredBits}' bits of data requires at least '{requiredBytes}' bytes\n");

            Console.WriteLine($"Enter the file you want to hide the data in");
            string fileB = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(fileB)) { goto referenceFile; }

            string ext = Path.GetExtension(fileB).ToLowerInvariant();
            switch (ext)
            {
                default:
                    goto referenceFile;
                case ".wav":
                    Steganography.WriteWav(fileB, leastSigBits, bitsToSave, requiredBits, requiredBytes);

                    Console.WriteLine($"Press any key to continue...");
                    Console.ReadKey();
                    break;
                case ".jpeg":
                case ".jpg":
                case ".bmp":
                case ".png":
                case ".tiff":
                case ".tif":
                    Steganography.WriteImage(fileB, leastSigBits, bitsToSave, requiredBits, requiredBytes);

                    Console.WriteLine($"Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }

        public static void ReadFile()
        {
        start:
            Console.Clear();
            Console.WriteLine($"Enter the file you want to find a hidden file in");
            string file = Console.ReadLine().Replace("\"", "");

            if (!File.Exists(file)) { goto start; }

            string ext = Path.GetExtension(file).ToLowerInvariant();
            switch (ext)
            {
                default:
                    goto start;
                case ".wav":
                    Steganography.ReadFromWav(file);
                    break;
                case ".png":
                    Steganography.ReadFromImage(file);
                    break;
            }

            Console.WriteLine($"Press any key to continue...");
            Console.ReadKey();
        }

    }
}
