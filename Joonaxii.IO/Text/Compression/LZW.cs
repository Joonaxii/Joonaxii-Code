﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Joonaxii.Text.Compression
{
    /// <summary>
    /// A simple LZW compression implementation
    /// </summary>
    public static class LZW
    {
        /// <summary>
        /// Compresses a string using LZW
        /// </summary>
        /// <param name="input">The string to be compressed</param>
        /// <returns>A list of compressed chars/groups</returns>
        public static List<int> Compress(string input) => Compress(input, out byte size, out ushort charLimit, true);

        /// <summary>
        /// Decompresses a list of LZW compressed chars/groups.
        /// </summary>
        /// <param name="compressed">The list of comrpessed chars/groups</param>
        /// <returns>Decompressed string</returns>
        public static string Decompress(List<int> compressed) => Decompress(compressed, char.MaxValue);

        /// <summary>
        /// <para>Compresses and writes a string to BinaryWriter. Also writes a small "mini-header" before the list of chars/groups which contains the amount of compressed chars/groups, byte size of each char/group and the highest valued char</para>
        /// Mini-Header Example: ====[CHAR/GROUP COUNT (4 bytes)]====[BYTE SIZE (1 byte)]====[HIGHEST CHAR (2 bytes)]====
        /// </summary>
        /// <param name="input">The string to be compressed</param>
        /// <param name="bw">The BinaryWriter the bytes are going to be written to</param>
        public static void Compress(string input, BinaryWriter bw)
        {
            var compressed = Compress(input, out byte size, out ushort charLimit, false);
            bw.Write(compressed.Count);
            bw.Write(charLimit);
            bw.Write(size);
            WriteValues(bw, size, compressed);
        }

        /// <summary>
        /// <para>Reads a LZW compressed string from a BinaryReader</para>
        /// THE COMPRESSED STRING MUST BE ONE THAT HAS THE "MINI-HEADER"!
        /// </summary>
        /// <param name="br">The BinaryReader to read from</param>
        /// <returns></returns>
        public static string Decompress(BinaryReader br)
        {
            int len = br.ReadInt32();
            ushort charLimit = br.ReadUInt16();
            byte size = br.ReadByte();

            int[] compressed = new int[len];
            ReadValue(br, size, compressed);
            return Decompress(new List<int>(compressed), charLimit);
        }

        private static List<int> Compress(string input, out byte size, out ushort charLimit, bool fixedSize)
        {
            charLimit = fixedSize ? char.MaxValue : GetHighestChar(input);

            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            InitializeLookup(dictionary, charLimit + 1);

            string temp = string.Empty;
            List<int> compressed = new List<int>();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                string composite = temp + c;

                if (dictionary.ContainsKey(composite))
                {
                    temp = composite;
                    continue;
                }
                compressed.Add(dictionary[temp]);

                dictionary.Add(composite, dictionary.Count);
                temp = c.ToString();
            }

            if (!string.IsNullOrEmpty(temp))
            {
                compressed.Add(dictionary[temp]);
            }

            size = GetSize((Math.Max(charLimit, dictionary.Count - 1)));
            return compressed;
        }
        private static string Decompress(List<int> compressed, int initialChars)
        {
            if (compressed.Count < 1) { return string.Empty; }

            Dictionary<int, string> dictionary = new Dictionary<int, string>();
            InitializeLookup(dictionary, initialChars + 1);

            string w = dictionary[compressed[0]];
            compressed.RemoveAt(0);
            StringBuilder sb = new StringBuilder(w);

            for (int i = 0; i < compressed.Count; i++)
            {
                string entry = string.Empty;
                int k = compressed[i];

                entry = k == dictionary.Count ? w + w[0] : dictionary.TryGetValue(k, out string val) ? val : entry;

                sb.Append(entry);
                dictionary.Add(dictionary.Count, w + entry[0]);
                w = entry;
            }
            return sb.ToString();
        }

        #region Binary R/W Helpers
        private static void ReadValue(BinaryReader br, byte size, int[] values)
        {
            switch (size)
            {
                default:
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = br.ReadByte();
                    }
                    break;
                case 2:
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = br.ReadUInt16();
                    }
                    break;
                case 4:
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = br.ReadInt32();
                    }
                    break;
            }
        }
        private static void WriteValues(BinaryWriter bw, byte size, IEnumerable<int> values)
        {
            switch (size)
            {
                default:
                    foreach (int item in values)
                    {
                        bw.Write((byte)item);
                    }
                    break;
                case 1:
                    foreach (int item in values)
                    {
                        bw.Write((ushort)item);
                    }
                    break;
                case 2:
                    foreach (int item in values)
                    {
                        bw.Write(item);
                    }
                    break;
            }
        }
        #endregion

        #region Helpers
        private static byte GetSize(int len)
        {
            if (len > ushort.MaxValue) { return 4; }
            if (len > byte.MaxValue) { return 2; }
            return 1;
        }

        private static void InitializeLookup(Dictionary<int, string> lookup, int length)
        {
            for (int i = 0; i < length; i++)
            {
                lookup.Add(i, ((char)i).ToString());
            }
        }
        private static void InitializeLookup(Dictionary<string, int> lookup, int length)
        {
            for (int i = 0; i < length; i++)
            {
                lookup.Add(((char)i).ToString(), i);
            }
        }

        private static ushort GetHighestChar(string input)
        {
            ushort len = 0;
            for (int i = 0; i < input.Length; i++)
            {
                ushort ii = input[i];
                len = ii > len ? ii : len;
            }
            return len;
        }
        #endregion
    }
}