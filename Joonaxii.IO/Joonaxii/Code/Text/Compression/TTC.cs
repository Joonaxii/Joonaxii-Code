using Joonaxii.Text.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Joonaxii.IO.Text.Compression
{
    /// <summary>
    /// A Token Based Text compression algorithm, also utilizes the LZW compression where it makes sense
    /// </summary>
    public static class TTC
    {
        public const string HEADER_STR = "TTC";
        public static readonly byte[] HEADER_STR_BYTES = Encoding.ASCII.GetBytes(HEADER_STR);

        public const string TOKEN_STR = "TOK";
        public static readonly byte[] TOKEN_STR_BYTES = Encoding.ASCII.GetBytes(TOKEN_STR);

        public const int MAX_AVERAGE_CHARS_PER_WORD = 10;

        /// <summary>
        /// <para>Compresses given string with TTC, then writes that data to the given BinaryWriter </para> 
        /// <para>Might also use LZW compression if the average word length is more than the MAX_AVERAGE_CHARS_PER_WORD </para> 
        /// Can potentially use LZW on the TTC compression if it will result in a smaller file
        /// </summary>
        /// <param name="input">The string to be compressed</param>
        /// <param name="bw">The BinaryWriter where the compressed data will be written to</param>
        public static void Compress(string input, BinaryWriter bw)
        {
            Dictionary<WordToken, int> tokenLookup = new Dictionary<WordToken, int>(2048);
            List<WordToken> tokenIntLookup = new List<WordToken>(2048);

            List<Word> tokens = new List<Word>(4096);

            int totalLength = 0;
            int addedTokens = 0;

            bool isWord = false;
            int start = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char cur = input[i];

                bool reachedEnd = i >= input.Length - 1;
                char next = !reachedEnd ? input[i + 1] : char.MinValue;

                if (!isWord)
                {
                    isWord = true;
                    start = i;
                }

                if (isWord)
                {
                    bool isNextDiff = cur > byte.MaxValue ? next <= byte.MaxValue : next > byte.MaxValue;
                    bool nextIsSpace = !isNextDiff & (next == ' ' & cur != ' ');
                    if (nextIsSpace |
                        isNextDiff |
                        (char.IsLetterOrDigit(cur) & char.IsPunctuation(next)) |
                        (char.IsLetterOrDigit(next) & (char.IsPunctuation(cur) & cur != '\'')) |
                        (!char.IsControl(cur) & char.IsControl(next)) | reachedEnd)
                    {
                        bool isChar = next == ' ' & cur != ' ';
                        int nextI = i + (nextIsSpace ? 2 : 1);
                        int l = nextI - start;
                        WordToken token = new WordToken(input.Substring(start, l));
                        if (!tokenLookup.TryGetValue(token, out int index))
                        {
                            index = tokenLookup.Count;
                            tokenLookup.Add(token, index);
                            tokenIntLookup.Add(token);

                            if (token.IsWord)
                            {
                                totalLength += token.word.Length;
                                addedTokens++;
                            }
                        }
                        tokens.Add(new Word(start, nextI, index));
                        isWord = false;

                        if (nextIsSpace) { i++; }
                    }
                }
            }

            tokenIntLookup.Sort();

            tokenLookup.Clear();
            for (int i = 0; i < tokenIntLookup.Count; i++)
            {
                var tok = tokenIntLookup[i];
                tokenLookup.Add(tok, i);
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                var word = tokens[i];     
                tokens[i] = word.SetIndex(tokenLookup[new WordToken(word.GetWordString(input))]);
            }

            int averageWordL = totalLength / (addedTokens < 1 ? 1 : addedTokens);
            if (averageWordL > MAX_AVERAGE_CHARS_PER_WORD)
            {
                System.Diagnostics.Debug.Print($"Average Token Length is too HIGH! ({averageWordL} // {MAX_AVERAGE_CHARS_PER_WORD}) Falling back to LZW!");
                LZW.Compress(input, bw);
                return;
            }

            byte tokenSize = (byte)(tokenIntLookup.Count > byte.MaxValue ? 2 : 1);

            bw.Write(HEADER_STR_BYTES);
            bw.Write(tokenSize);
            bw.Write(tokens.Count);
            bw.Write(tokenIntLookup.Count);

            int startI = 0;
            byte sizeI = tokenIntLookup[0].size;


            for (int i = 0; i < tokenIntLookup.Count; i++)
            {
                var tok = tokenIntLookup[i];

                if(tok.size != sizeI)
                {
                    bw.Write(TOKEN_STR_BYTES);

                    bw.Write(sizeI);
                    bw.Write(startI);
                    bw.Write(i);
                    System.Diagnostics.Debug.Print($"Wrote a token range with a size [{sizeI}]");

                    startI = i;
                    sizeI = tok.size;
                }
            }

            //APPEND THE LAST TOKEN SIZE RANGE
            {
                bw.Write(TOKEN_STR_BYTES);

                bw.Write(sizeI);
                bw.Write(startI);
                bw.Write(tokenIntLookup.Count);
                System.Diagnostics.Debug.Print($"Wrote a token range with a size [{sizeI}]");
            }

            for (int i = 0; i < tokenIntLookup.Count; i++)
            {
                tokenIntLookup[i].WriteBytes(bw);
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var wToken = tokenIntLookup[token.index];
                token.WriteBytes(bw, tokenSize);
            }

            tokens.Clear();
            tokenIntLookup.Clear();
            tokenLookup.Clear();

            byte[] data = (bw.BaseStream as MemoryStream).ToArray();
            byte[] dataLZW = null;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter bw2 = new BinaryWriter(stream))
            {
                LZW.Compress(data, bw2);
                dataLZW = stream.ToArray();
            }

            if (dataLZW.Length >= data.Length)
            {
                System.Diagnostics.Debug.Print($"LZW Resulted in a larger file, Ignoring! (LZW): {dataLZW.Length} bytes, (Token): {data.Length} bytes");
                return;
            }

            bw.Flush();
            bw.BaseStream.SetLength(0);
            bw.BaseStream.Position = 0;

            bw.Write(dataLZW);
            System.Diagnostics.Debug.Print($"Compressed String w/ LZW {dataLZW.Length} bytes");
        }

        /// <summary>
        /// <para>Decompresses data in the given BinaryReader</para>
        /// <para>If the file starts with LZW, then it first uses LZW to decompress the data, if after that the TTC header string is found, It will use the TTC decompression</para>
        /// If no LZW header is found, it checks for the TTC header, if found, decompress with TTC, otherwise return an empty string
        /// </summary>
        /// <param name="br">The BinaryReader to read data from</param>
        /// <returns></returns>
        public static string Decompress(BinaryReader br)
        {
            bool isCorrect = true;
            for (int i = 0; i < 3; i++)
            {
                if (br.ReadByte() != LZW.HEADER_STR[i]) { isCorrect = false; br.BaseStream.Position -= (i + 1); break; }
            }

            br.BaseStream.Position -= isCorrect ? 3 : 0;
            if (isCorrect)
            {
                string uncomp = LZW.Decompress(br);
                byte[] data = new byte[uncomp.Length * 2];

                int dataI = 0;
                for (int i = 0; i < uncomp.Length; i++)
                {
                    ushort c = uncomp[i];
                    data[dataI++] = (byte)(c);
                    data[dataI++] = (byte)(c >> 8);
                }

                isCorrect = true;
                for (int i = 0; i < 3; i++)
                {
                    if (data[i] != HEADER_STR_BYTES[i]) { isCorrect = false; break; }
                }

                if (!isCorrect) { return uncomp; }

                using(MemoryStream stream = new MemoryStream(data))
                using(BinaryReader br2 = new BinaryReader(stream))
                {
                    return DecompressTTC(br2, true);
                }
            }
            return DecompressTTC(br, true);
        }

        private static string DecompressTTC(BinaryReader br, bool includeHeaderString)
        {
            if (includeHeaderString)
            {
                bool isCorrect = true;
                for (int i = 0; i < 3; i++)
                {
                    if (br.ReadByte() != HEADER_STR_BYTES[i]) { isCorrect = false; break; }
                }
                if (!isCorrect) { return string.Empty; }
            }

            byte tokenPaletteSize = br.ReadByte();
            int tokenCount = br.ReadInt32();
            int tokenLookups = br.ReadInt32();

            WordToken[] tokenLookup = new WordToken[tokenLookups];
            byte[] uTokenSizes = new byte[tokenLookups];

            while (true)
            {
                if(Encoding.ASCII.GetString(br.ReadBytes(3)) != TOKEN_STR) { br.BaseStream.Position -= 3; break; }

                byte size = br.ReadByte();
                int start = br.ReadInt32();
                int end = br.ReadInt32();
                for (int i = start; i < end; i++)
                {
                    uTokenSizes[i] = size;
                }
            }

            for (int i = 0; i < tokenLookups; i++)
            {
                tokenLookup[i] = WordToken.ReadBytes(br, uTokenSizes[i]);
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tokenCount; i++)
            {
                Word token = Word.ReadBytes(br, tokenPaletteSize);
                sb.Append(tokenLookup[token.index].word);
            }
            return sb.ToString();
        }

        private struct WordToken : IEquatable<WordToken>, IComparable<WordToken>
        {
            public bool IsWord { get; private set; }

            public byte size;
            public string word;

            public WordToken(string word, byte size)
            {
                this.word = word;
                this.size = size;

                IsWord = false;
                IsWord = ValidateWord();
            }

            public WordToken(string word, bool full = true)
            {
                size = 0;
                this.word = word;

                if (!full) { IsWord = false; return; }
                int largest = 0;
                for (int i = 0; i < word.Length; i++)
                {
                    int c = word[i];
                    largest = largest < c ? c : largest;
                }

                IsWord = false;
                IsWord = ValidateWord();

                if (largest > byte.MaxValue)
                {
                    size = 2;
                    return;
                }
                size = 1;
            }

            public void WriteBytes(BinaryWriter bw)
            {
                bw.Write(word.Length);
                switch (size)
                {
                    default:
                        for (int i = 0; i < word.Length; i++)
                        {
                            bw.Write((byte)word[i]);
                        }
                        break;
                    case 2:
                        for (int i = 0; i < word.Length; i++)
                        {
                            bw.Write((ushort)word[i]);
                        }
                        break;
                    case 4:
                        for (int i = 0; i < word.Length; i++)
                        {
                            bw.Write((int)word[i]);
                        }
                        break;
                }
            }

            public static WordToken ReadBytes(BinaryReader br, byte size)
            {
                int len = br.ReadInt32();

                StringBuilder str = new StringBuilder(len);
                switch (size)
                {
                    default:
                        for (int i = 0; i < len; i++)
                        {
                            str.Append((char)br.ReadByte());
                        }
                        break;
                    case 2:
                        for (int i = 0; i < len; i++)
                        {
                            str.Append((char)br.ReadUInt16());
                        }
                        break;
                }
                return new WordToken(str.ToString(), size);
            }

            public override bool Equals(object obj) => obj is WordToken word && word == this;

            public bool Equals(WordToken other) => word == other.word;

            public override int GetHashCode() => word.GetHashCode();

            private bool ValidateWord()
            {
                for (int i = 0; i < word.Length; i++)
                {
                    char c = word[i];
                    if (char.IsSymbol(c) || char.IsLetterOrDigit(c)) { return true; }
                }
                return false;
            }

            public int CompareTo(WordToken other) => other.size.CompareTo(size);

            public static bool operator ==(WordToken token1, WordToken token2) => token1.Equals(token2);
            public static bool operator !=(WordToken token1, WordToken token2) => !(token1 == token2);
        }

        private struct Word
        {
            public int startIndex;
            public int endIndex;

            public int index;

            public int Length { get => endIndex - startIndex; }

            public Word(int startIndex, int endIndex, int index)
            {
                this.startIndex = startIndex;
                this.endIndex = endIndex;

                this.index = index;
            }

            public Word(int index)
            {
                this.index = index;

                startIndex = -1;
                endIndex = -1;
            }

            public string GetWordString(string input) => input.Substring(startIndex, Length);

            public void WriteBytes(BinaryWriter bw, byte sizeType)
            {
                switch (sizeType)
                {
                    default:
                        bw.Write((byte)index);
                        break;
                    case 2:
                        bw.Write((ushort)index);
                        break;
                    case 4:
                        bw.Write(index);
                        break;
                }
            }

            public static Word ReadBytes(BinaryReader br, byte indexType)
            {
                int index = 0;
                switch (indexType)
                {
                    default:
                        index = br.ReadByte();
                        break;
                    case 2:
                        index = br.ReadUInt16();
                        break;
                    case 4:
                        index = br.ReadInt32();
                        break;
                }
                return new Word(index);
            }

            public Word SetIndex(int id)
            {
                index = id;
                return this;
            }
        }
    }
}
