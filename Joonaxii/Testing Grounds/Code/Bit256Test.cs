using Joonaxii.IO;
using System;

namespace Testing_Grounds
{
    public class Bit256Test : MenuItem
    {
        public Bit256Test(string name, bool enabled = true) : base(name, enabled)
        {
        }

        public override bool OnClick()
        {
            start:
            Console.Clear();
            Console.WriteLine($"Please enter a value either from 0 to 256 as the amount of bits to be set or");
            Console.WriteLine($"from values 0 to 64 for each inner ulong");

            string[] split = Console.ReadLine().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (split.Length < 1) { goto start; }

            bool[] bits = new bool[256];
            if(bits.Length == 1)
            {
                if (!int.TryParse(split[0].Trim(), out int bit)) { goto start; }
                for (int j = 0; j < Math.Min(bit, 256); j++)
                {
                    bits[j] = true;
                }
            }
            else
            {
                for (int i = 0; i < Math.Min(split.Length, 4); i++)
                {
                    if (!int.TryParse(split[i].Trim(), out int bit)) { continue; }

                    int start = i * 64;
                    int end = start + Math.Min(64, bit);
                    for (int j = start; j < end; j++)
                    {
                        bits[j] = true;
                    }
                }
            }

            Bit256 bit256 = new Bit256(bits);
            Console.WriteLine($"Here are the set bits: \n\n{bit256.ToString('\n', true)}");

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