using System;
using System.IO;
using System.Text;
using Joonaxii.Debugging;
using System.Collections.Generic;
using Joonaxii.IO;
using Joonaxii.MathJX;

namespace Joonaxii.Text.Compression
{
    /// <summary>
    /// A Token Based Text compression algorithm, also utilizes the LZW compression where it makes sense
    /// </summary>
    public static class TTC
    {
        public const int HEADER_SIZE = 12;
        public const int LZW_THRESHOLD = 4096;
        public const int MAX_AVERAGE_CHARS_PER_WORD = 10;

        public const string HEADER_STR = "TTC";
        public static readonly byte[] HEADER_STR_BYTES = Encoding.ASCII.GetBytes(HEADER_STR);

        public const string TOKEN_STR = "TOK";
        public static readonly byte[] TOKEN_STR_BYTES = Encoding.ASCII.GetBytes(TOKEN_STR);

        public static void Compress(byte[] data, BinaryWriter bw, IndexCompressionMode idxCompression, TimeStamper stamper = null)
        {
            StringBuilder sb = new StringBuilder();

            int l = data.Length;
            l = l % 2 != 0 ? l + 1 : l;

            char[] chars = new char[l / 2];

            int bit = 0;
            stamper?.Start($"TTC (Compress): Converting '{data.Length}' bytes to '{chars.Length}' chars");
            for (int i = 0; i < chars.Length; i++)
            {
                int a = data[bit++];
                int b = (bit <= data.Length - 1 ? data[bit++] : 0);
                sb.Append((char)(a + (b << 8)));
            }
            stamper?.Stamp();

            Compress(sb.ToString(), bw, idxCompression, stamper);
        }

        /// <summary>
        /// <para>Compresses given string with TTC, then writes that data to the given BinaryWriter </para> 
        /// <para>Might also use LZW compression if the average word length is more than the MAX_AVERAGE_CHARS_PER_WORD </para> 
        /// Can potentially use LZW on the TTC compression if it will result in a smaller file
        /// </summary>
        /// <param name="input">The string to be compressed</param>
        /// <param name="bw">The BinaryWriter where the compressed data will be written to</param>
        public static void Compress(string input, BinaryWriter bw, IndexCompressionMode idxCompression, TimeStamper timeStamper = null, FileDebugger fileDebugger = null)
        {
            long startPos = bw.BaseStream.Position;
            long startLen = bw.BaseStream.Length;
            Dictionary<WordToken, int> tokenLookup = new Dictionary<WordToken, int>(2048);
            List<WordToken> tokenIntLookup = new List<WordToken>(2048);

            List<int> tokenIndices = new List<int>(4096);

            int totalLength = 0;
            int addedTokens = 0;

            timeStamper?.Start("TTC (Compress): Initial Token evaluation");
            #region Initial Token Evaluation

            int start = 0;
            int l = input.Length - 1;

            Dictionary<byte, TokenRange> tokenRanges = new Dictionary<byte, TokenRange>();

            for (int i = 0; i < input.Length; i++)
            {
                char cur = input[i];

                bool reachedEnd = i >= l;
                char next = reachedEnd ? char.MinValue : input[i + 1];

                bool curIsBig = cur > byte.MaxValue;
                bool nextIsBig = next > byte.MaxValue;

                bool isNextDiff = curIsBig != nextIsBig;
                bool nextIsSpace = !isNextDiff && (next == ' ' & cur != ' ');

                bool isCurLN = char.IsLetterOrDigit(cur);
                bool isNextLN = char.IsLetterOrDigit(next);
                if ((reachedEnd | nextIsSpace | isNextDiff) ||
                    ((isCurLN & !isNextLN) && char.IsPunctuation(next)) ||
                    ((cur != '\'' & isNextLN & !isCurLN) && char.IsPunctuation(cur)) ||
                    (isCurLN != isNextLN & !char.IsControl(cur) && char.IsControl(next)))
                {
                    string tmp = input.Substring(start, (i + (nextIsSpace ? 2 : 1)) - start);

                    WordToken token = new WordToken(tmp);
                    if (!tokenLookup.TryGetValue(token, out int index))
                    {
                        token.Initialize(out byte sizeInbits, out byte lengthInbits);

                        if (!tokenRanges.TryGetValue(sizeInbits, out var val))
                        {
                            val = new TokenRange(sizeInbits);
                            tokenRanges.Add(sizeInbits, val);
                        }
                        val.Setup(lengthInbits);

                        index = tokenLookup.Count;
                        tokenLookup.Add(token, index);
                        tokenIntLookup.Add(token);

                        if (token.IsWord)
                        {
                            totalLength += token.word.Length;
                            addedTokens++;
                        }
                    }
                    tokenIndices.Add(index);

                    if (nextIsSpace) { i++; }
                    start = i + 1;
                }
            }

            #endregion
            timeStamper?.Stamp();

            int averageWordL = totalLength / (addedTokens < 1 ? 1 : addedTokens);
            if (averageWordL > MAX_AVERAGE_CHARS_PER_WORD)
            {
                System.Diagnostics.Debug.Print($"Average Token Length is too HIGH! ({averageWordL} // {MAX_AVERAGE_CHARS_PER_WORD}) Falling back to LZW!");
                LZW.Compress(input, idxCompression == IndexCompressionMode.LZWChunked, bw, timeStamper);
                return;
            }

            timeStamper?.Start("TTC (Compress): Token byte size sorting");
            #region Token Byte Size Sorting

            tokenIntLookup.Sort();
            int[] tempLookup = new int[tokenIntLookup.Count];
            for (int i = 0; i < tokenIntLookup.Count; i++)
            {
                var tok = tokenIntLookup[i];
                tempLookup[tokenLookup[tok]] = i;
                tokenLookup[tok] = i;
            }

            for (int i = 0; i < tokenIndices.Count; i++)
            {
                tokenIndices[i] = tempLookup[tokenIndices[i]];
            }

            tempLookup = null;

            #endregion
            timeStamper?.Stamp();

            byte tokenSize = (byte)(tokenIntLookup.Count > byte.MaxValue ? 2 : 1);

            fileDebugger?.Start($"TTC Header");

            fileDebugger?.StartSub("Common");
            bw.Write(HEADER_STR_BYTES);
            bw.Write((byte)idxCompression);
            bw.Write(tokenIntLookup.Count);
            fileDebugger?.Stamp();

            switch (idxCompression)
            {
                case IndexCompressionMode.None:

                    fileDebugger?.StartSub("No Index Compression");
                    bw.Write(tokenSize);
                    bw.Write(tokenIndices.Count);
                    fileDebugger?.Stamp(true);

                    WriteTokens(tokenIntLookup, tokenRanges, bw, timeStamper, fileDebugger);

                    timeStamper?.Start($"TTC (Compress): Token Index writing '{tokenIndices.Count}' tokens");
                    #region Token Index Writing

                    fileDebugger?.Start("Index Data");
                    for (int i = 0; i < tokenIndices.Count; i++)
                    {
                        var token = tokenIndices[i];
                        var wToken = tokenIntLookup[token];
                        WriteIndex(bw, tokenSize, token);
                    }
                    fileDebugger?.Stamp(true);

                    #endregion
                    timeStamper?.Stamp();
                    break;

                case IndexCompressionMode.LZW:
                case IndexCompressionMode.LZWChunked:
                    fileDebugger?.StartSub("LZW Index Compression");
                    bw.Write(tokenSize);
                    bw.Write(tokenIndices.Count);
                    fileDebugger?.Stamp(true);

                    WriteTokens(tokenIntLookup, tokenRanges, bw, timeStamper, fileDebugger);

                    LZW.CompressToStream(tokenIndices, tokenSize, idxCompression == IndexCompressionMode.LZWChunked, bw, timeStamper, fileDebugger);
                    break;

                case IndexCompressionMode.Huffman:

                    WriteTokens(tokenIntLookup, tokenRanges, bw, timeStamper, fileDebugger);

                    Huffman.CompressToStream(tokenIndices, (byte)IOExtensions.BitsNeeded(tokenIntLookup.Count), bw, timeStamper, fileDebugger);
                    break;
            }
        }

        public static byte[] DecompressAsData(BinaryReader br, TimeStamper timeStamper = null)
        {
            char[] dataStr = Decompress(br, timeStamper).ToCharArray();
            byte[] data = new byte[dataStr.Length * 2];

            timeStamper?.Start($"Converting '{dataStr.Length}' chars to '{data.Length}' bytes");
            int ii = 0;
            for (int i = 0; i < dataStr.Length; i++)
            {
                char c = dataStr[i];
                data[ii++] = (byte)c;
                data[ii++] = (byte)(c >> 8);
            }
            timeStamper?.Stamp();
            return data;
        }

        public static string Decompress(BinaryReader br, TimeStamper timeStamper = null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (br.ReadByte() != HEADER_STR_BYTES[i]) { br.BaseStream.Position -= (i + 1); return br.ReadString(); }
            }

            System.Diagnostics.Debug.Print($"Starting TTC Decompression");
            StringBuilder sb = new StringBuilder();
            IndexCompressionMode mode = (IndexCompressionMode)br.ReadByte();
            byte tokenPaletteSize;
            int tokenCount;
            int tokenLookups;

            tokenLookups = br.ReadInt32();

            List<(byte bits, byte size, RangeInt range)> ranges = new List<(byte bits, byte size, RangeInt range)>();
            WordToken[] tokenLookup = new WordToken[tokenLookups];

            System.Diagnostics.Debug.Print($"TTC Header: {mode}, {tokenLookups}");
            int totalRead = 0;
            switch (mode)
            {
                case IndexCompressionMode.None:
                    tokenPaletteSize = br.ReadByte();
                    tokenCount = br.ReadInt32();

                    ReadTokens(ranges, tokenLookup, br, timeStamper);
                    timeStamper?.Start($"TTC (Decompress): Reading '{tokenCount}' tokens");
                    for (int i = 0; i < tokenCount; i++)
                    {
                        sb.Append(tokenLookup[ReadIndex(br, tokenPaletteSize)].word);
                    }
                    timeStamper?.Stamp();
                    break;

                case IndexCompressionMode.LZWChunked:
                case IndexCompressionMode.LZW:
                    tokenPaletteSize = br.ReadByte();
                    tokenCount = br.ReadInt32();

                    ReadTokens(ranges, tokenLookup, br, timeStamper);
                    char[] output = LZW.Decompress(br, timeStamper);
                    switch (tokenPaletteSize)
                    {
                        case 1:
                        case 2:
                            for (int i = 0; i < output.Length; i++)
                            {
                                sb.Append(tokenLookup[output[i]].word);
                            }
                            break;
                        case 4:
                            int ll = output.Length;
                            for (int i = 0; i < ll; i += 2)
                            {
                                char a = output[i];
                                char b = output[i + 1];
                                sb.Append(tokenLookup[(a + (b << 16))].word);
                            }
                            break;
                    }
                    break;
                case IndexCompressionMode.Huffman:

                    ReadTokens(ranges, tokenLookup, br, timeStamper);
                    List<int> codes = new List<int>();
                    Huffman.DecompressFromStream(codes, br);

                    for (int i = 0; i < codes.Count; i++)
                    {
                        sb.Append(tokenLookup[codes[i]].word);
                    }

                    break;
            }

            return sb.ToString();
        }

        private static void ReadTokens(List<(byte bits, byte size, RangeInt range)> ranges, WordToken[] tokenLookup, BinaryReader br, TimeStamper timeStamper)
        {
            timeStamper?.Start("TTC (Decompress): Reading token size ranges");
            while (true)
            {
                if (Encoding.ASCII.GetString(br.ReadBytes(3)) != TOKEN_STR) { br.BaseStream.Position -= 3; break; }

                byte bits = br.ReadByte();
                byte size = br.ReadByte();
                int start = br.ReadInt32();
                int end = br.ReadInt32();

                ranges.Add((bits, size, new RangeInt(start, end, true)));
                System.Diagnostics.Debug.Print($"Read range: {bits}, {size}, {start} => {end}");
            }
            timeStamper?.Stamp();

            timeStamper?.Start($"TTC (Decompress): Reading '{tokenLookup.Length}' unique tokens");
            var curRange = ranges[0];
            int curRangeI = 0;

            long pos = br.BaseStream.Position;

            using (MemoryStream bStream = new MemoryStream(br.BaseStream.GetData()))
            using (BitReader btR = new BitReader(bStream))
            {
                bStream.Position = pos;
                for (int i = 0; i < tokenLookup.Length; i++)
                {
                    if (i >= curRange.range.end && curRangeI < ranges.Count - 1)
                    {
                        curRangeI++;
                        curRange = ranges[curRangeI];
                    }
                    tokenLookup[i].ReadBytes(btR, curRange.bits, curRange.size);
                }
                pos = bStream.Position;
            }

            br.BaseStream.Position = pos;
            br.BaseStream.Flush();
            timeStamper?.Stamp();
        }

        private static void WriteTokens(List<WordToken> tokenIntLookup, Dictionary<byte, TokenRange> tokenData, BinaryWriter bw, TimeStamper timeStamper, FileDebugger fileDebugger)
        {
            int startI = 0;
            byte sizeI = tokenIntLookup[0].size;

            fileDebugger?.Start("Token LUT");
            timeStamper?.Start("TTC (Compress): Token byte size range writing");

            TokenRange curR = tokenData[sizeI];

            #region Token Byte Size Ranges
            for (int i = 0; i < tokenIntLookup.Count; i++)
            {
                var tok = tokenIntLookup[i];

                if (tok.size != sizeI)
                {
                    bw.Write(TOKEN_STR_BYTES);

                    bw.Write(curR.bits);
                    bw.Write(sizeI);
                    bw.Write(startI);
                    bw.Write(i);

                    startI = i;
                    sizeI = tok.size;
                    curR = tokenData[sizeI];
                }
            }

            //APPEND THE LAST TOKEN SIZE RANGE
            {
                bw.Write(TOKEN_STR_BYTES);

                bw.Write(curR.bits);
                bw.Write(sizeI);
                bw.Write(startI);
                bw.Write(tokenIntLookup.Count);
            }

            using (MemoryStream bStream = new MemoryStream())
            using (BitWriter btW = new BitWriter(bStream))
            {
                for (int i = 0; i < tokenIntLookup.Count; i++)
                {
                    var tok = tokenIntLookup[i];
                    tok.WriteBytes(btW, tokenData[tok.size].bits);
                }

                btW.Flush();
                bw.Write(bStream.ToArray());
            }

            #endregion
            fileDebugger?.Stamp();
            timeStamper?.Stamp();
        }

        private static void WriteIndex(BinaryWriter bw, byte sizeType, int index)
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

        private static int ReadIndex(BinaryReader br, byte indexType)
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
            return index;
        }

        private struct WordToken : IEquatable<WordToken>, IComparable<WordToken>
        {
            public int MinValue { get => _min; }
            public bool IsWord { get; private set; }
            public byte size;

            public string word;

            private int _min;

            public WordToken(string word, byte size)
            {
                this.word = word;
                this.size = size;

                _min = int.MaxValue;

                IsWord = false;
                IsWord = ValidateWord();
            }

            public WordToken(string word)
            {
                _min = int.MaxValue;

                size = 0;
                this.word = word;
                IsWord = false;
            }

            public void Initialize(out byte sizeInBits, out byte lengthInBits)
            {
                _min = int.MaxValue;
                IsWord = ValidateWord();
                int largest = 0;
                for (int i = 0; i < word.Length; i++)
                {
                    int c = word[i];
                    largest = largest < c ? c : largest;

                    _min = _min > c ? c : _min;
                }

                lengthInBits = (byte)IOExtensions.BitsNeeded(word.Length);
                size = sizeInBits = (byte)IOExtensions.BitsNeeded(largest);
            }

            public void WriteBytes(BitWriter bw, byte bits)
            {
                bw.Write(word.Length, bits);
                for (int i = 0; i < word.Length; i++)
                {
                    bw.Write(word[i], size);
                }
            }

            public WordToken ReadBytes(BitReader br, byte bits, byte size)
            {
                int len = br.ReadValue(bits);

                StringBuilder str = new StringBuilder(len);
                for (int i = 0; i < len; i++)
                {
                    var cc = (char)br.ReadValue(size);
                    str.Append(cc);
                }

                this.size = size;
                word = str.ToString();
                IsWord = ValidateWord();
                return this;
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

            public int CompareTo(WordToken other) => size.CompareTo(other.size);

            public static bool operator ==(WordToken token1, WordToken token2) => token1.Equals(token2);
            public static bool operator !=(WordToken token1, WordToken token2) => !(token1 == token2);
        }
    }
}
