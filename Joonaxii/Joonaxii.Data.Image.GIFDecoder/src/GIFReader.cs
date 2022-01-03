using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Joonaxii.MathJX;

namespace Joonaxii.Data.Image.GIFDecoder
{
    public class GIFReader : IDisposable
    {
        public int FrameCount { get => _frames.Count; }

        public ushort Width { get => _width; }
        public ushort Height { get => _height; }

        public bool HasAlpha { get => _hasAlpha; }
        public bool IsValidIndexed { get => _palette.Length < 256; }

        private Stream _stream;
        private BinaryReader _br;

        private ushort _width;
        private ushort _height;

        private List<GIFFrame> _frames;
        private bool _hasAlpha;

        private FastColor[] _palette;
        private int _headerValid;

        public GIFReader(Stream stream)
        {
            _stream = stream;
            _br = new BinaryReader(_stream);

            _frames = new List<GIFFrame>();
            _headerValid = 0;
        }

        public GIFReader(BinaryReader br) : this(br, 0) { }
        public GIFReader(BinaryReader br, int markHeaderValid)
        {
            _stream = br.BaseStream;
            _br = br;

            _frames = new List<GIFFrame>();
            _headerValid = markHeaderValid;
        }

        public GIFReader(string path) : this(new FileStream(path, FileMode.Open)) { }

        public bool Decode(out string message)
        {
            try
            {
                FastColor[] globalPalette;
                _hasAlpha = false;
                bool is89 = false;

                #region Header
                if (_headerValid == 0)
                {
                    string hdr = Encoding.ASCII.GetString(_br.ReadBytes(3));
                    if (hdr != "GIF")
                    {
                        message = "Not a GIF Header!";
                        return false;
                    }
                    string ver = Encoding.ASCII.GetString(_br.ReadBytes(3));

                    switch (ver)
                    {
                        default:
                            message = $"Unsupported gif version '{ver}'";
                            return false;
                        case "87a":
                            break;
                        case "89a":
                            is89 = true;
                            break;
                    }
                }
                else { is89 = _headerValid == 2; }
                HashSet<FastColor> palette = new HashSet<FastColor>();

                _width = _br.ReadUInt16();
                _height = _br.ReadUInt16();

                byte mapInfo = _br.ReadByte();
                byte bgIndex = _br.ReadByte();
                byte aspect = _br.ReadByte();

                int numOfEntries = Maths.IsBitSet(mapInfo, 7) ? 1 << (Maths.GetRange(mapInfo, 0, 3) + 1) : 0;
                int aspectRatio = (aspect + 15) / 64;

                palette.Add(FastColor.clear);

                globalPalette = numOfEntries < 1 ? new FastColor[0] : new FastColor[numOfEntries];
                for (int i = 0; i < numOfEntries; i++)
                {
                    var pix = new FastColor(_br.ReadByte(), _br.ReadByte(), _br.ReadByte(), 255);
                    globalPalette[i] = pix;
                    palette.Add(pix);
                }
                #endregion

                GIFFrame first = null;
                GIFFrame prev = null;

                int mode = 0;
                while (_stream.Position < _stream.Length)
                {
                    var frame = GetFrame(_br, bgIndex, is89, globalPalette, ref mode, palette, prev, first);
                    if (frame == null) { break; }

                    prev = frame;
                    if (_frames.Count < 1) { first = prev; }
                    _frames.Add(frame);
                }

                if (!_hasAlpha) { palette.Remove(FastColor.clear); }
                _palette = new FastColor[palette.Count];
                palette.CopyTo(_palette);

                message = "All OK";
                return true;
            }
            catch (Exception e)
            {
                message = $"{e.Message}, {e.StackTrace}";
                return false;
            }
        }

        public int GetPaletteSize() => _palette == null ? 0 : _palette.Length;
        public FastColor[] GetPalette()
        {
            if (_palette == null) { return null; }

            FastColor[] pal = new FastColor[_palette.Length];
            Array.Copy(_palette, pal, _palette.Length);
            return pal;
        }

        public GIFFrame GetFrameAt(int i) => _frames[i];
        private GIFFrame GetFrame(BinaryReader br, byte bgColor, bool is89, FastColor[] table, ref int mode, HashSet<FastColor> palette, GIFFrame previous, GIFFrame first)
        {
            byte f = br.ReadByte();
            if (f == 0x3b || br.BaseStream.Position >= br.BaseStream.Length) { return null; }

            bool hasAlpha = false;
            int alphaInd = 0;
            byte dMet = 0;
            GIFFrame frame = null;
            {
                while (true)
                {
                    if (f == 0x21)
                    {
                        f = br.ReadByte();
                        switch (f)
                        {
                            case 0xFE: //Comment Block
                                string temp = "";
                                f = br.ReadByte();
                                int bytesRead = 3;

                                while (true)
                                {
                                    for (int i = 0; i < f; i++)
                                    {
                                        temp += (char)br.ReadByte(); ;
                                        bytesRead++;
                                    }

                                    f = br.ReadByte();
                                    bytesRead++;
                                    if (f == 0) { break; }
                                }
                                break;
                            case 0xFF: //Application Extensions
                                byte auth = br.ReadByte();
                                var bb = br.ReadBytes(auth);

                                byte bbA = br.ReadByte();
                                while (bbA != 0)
                                {
                                    for (int j = 0; j < bbA; j++)
                                    {
                                        br.ReadByte();
                                    }
                                    bbA = br.ReadByte();
                                }
                                break;
                            case 0xF9: //GFX Ctrl

                                br.ReadByte();
                                byte dipos = br.ReadByte();
                                hasAlpha = Maths.IsBitSet(dipos, 0);
                                bool userI = Maths.IsBitSet(dipos, 1);
                                dMet = Maths.GetRange(dipos, 2, 3);

                                ushort delayTime = br.ReadUInt16();
                                if (hasAlpha)
                                {
                                    _hasAlpha = true;
                                    alphaInd = br.ReadByte();
                                    palette.Add(FastColor.clear);
                                }

                                br.ReadByte();

                                frame = new GIFFrame(_width, _height, delayTime);
                                break;

                            default:
                                f = br.ReadByte();
                                continue;
                        }
                    }

                    f = br.ReadByte();
                    if (f == 0x2C || br.BaseStream.Position >= br.BaseStream.Length)
                    {
                        break;
                    }
                }
            }

            if (frame == null) { return null; }

            var bg = table.Length < 1 ? default : table[bgColor];
            bool skipAlpha = false;
            switch (mode)
            {
                case 0x00:
                    bg.a = hasAlpha ? (byte)0 : bg.a;
                    for (int i = 0; i < frame.Length; i++)
                    {
                        frame.SetPixel(i, bg);
                    }
                    break;

                case 0x01:
                    skipAlpha = true;
                    previous?.CopyTo(frame);
                    break;

                case 0x02:
                    bg.a = hasAlpha ? (byte)0 : bg.a;
                    for (int i = 0; i < frame.Length; i++)
                    {
                        frame.SetPixel(i, bg);
                    }
                    break;
                case 0x04:
                    skipAlpha = true;
                    first?.CopyTo(frame);
                    break;
            }

            ushort x = br.ReadUInt16();
            ushort y = br.ReadUInt16();

            ushort w = br.ReadUInt16();
            ushort h = br.ReadUInt16();

            byte imgInfo = br.ReadByte();

            byte sizeLocal = Maths.GetRange(imgInfo, 0, 3);
            bool hasLocal = Maths.IsBitSet(imgInfo, 7);

            bool sort = Maths.IsBitSet(imgInfo, 5);
            bool interlaced = Maths.IsBitSet(imgInfo, 6);

            if (hasLocal)
            {
                int l = 1 << (sizeLocal + 1);
                FastColor[] temp = new FastColor[Math.Max(table.Length, l)];
                if (table.Length > 0)
                {
                    Array.Copy(table, temp, table.Length);
                }

                for (int i = 0; i < l; i++)
                {
                    var pix = new FastColor(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
                    temp[i] = pix;
                    palette.Add(pix);
                }

                table = temp;
            }


            List<byte> lzw = new List<byte>();
            byte size = br.ReadByte();
            byte b = br.ReadByte();

            while (b != 0x00)
            {
                for (int i = 0; i < b; i++)
                {
                    lzw.Add(br.ReadByte());
                }
                b = br.ReadByte();

                if (b == 0) { break; }
            }

            int[] indices = new int[w * h];
            Unpack(size, lzw, indices);

            //Correct Interlacing
            if (interlaced)
            {
                int[] indicesIT = new int[indices.Length];

                int wW = w;
                int j = 0;
                for (int i = 0; i < h; i += 8, j++) //Pass 1
                {
                    int ij = j * w;
                    int ii = i * w;
                    Array.Copy(indices, ij, indicesIT, ii, wW);
                }

                for (int i = 4; i < h; i += 8, j++) //Pass 2
                {
                    int ij = j * w;
                    int ii = i * w;
                    Array.Copy(indices, ij, indicesIT, ii, wW);
                }

                for (int i = 2; i < h; i += 4, j++) //Pass 3
                {
                    int ij = j * w;
                    int ii = i * w;
                    Array.Copy(indices, ij, indicesIT, ii, wW);
                }

                for (int i = 1; i < h; i += 2, j++) //Pass 4
                {
                    int ij = j * w;
                    int ii = i * w;
                    Array.Copy(indices, ij, indicesIT, ii, wW);
                }
                Array.Copy(indicesIT, 0, indices, 0, indicesIT.Length);
            }

            for (int yY = 0; yY < h; yY++)
            {
                int yP = yY + y;
                for (int xX = 0; xX < w; xX++)
                {
                    int xP = xX + x;
                    int ii = yP * _width + xP;
                    int i = yY * w + xX;

                    int ind = indices[i];
                    var clr = table[ind];
                    if (ind == alphaInd && hasAlpha)
                    {
                        if (skipAlpha) { continue; }
                        clr.a = 0;
                    }

                    frame.SetPixel(ii, clr);
                }
            }

            mode = dMet;
            return frame;
        }

        private static void Unpack(int lzwSize, List<byte> bytes, int[] indices)
        {
            int CLEAR_CODE = 1 << lzwSize;
            int STOP_CODE = CLEAR_CODE + 1;

            int matchL;

            int codeLen = lzwSize;

            int dictI = 0;
            LZWEntry[] table = new LZWEntry[(1 << (codeLen + 1))];

            for (int i = 0; i < (1 << (codeLen)); i++)
            {
                LZWEntry ent = new LZWEntry();
                ent.data = (byte)dictI;
                ent.prev = -1;
                ent.len = 1;

                table[i] = ent;
                dictI++;
            }

            dictI++;
            dictI++;

            int code = 0;
            int prev = -1;

            int bit;

            int bI = 0;
            int mask = 0x01;
            int pos = 0;
            while (bI < bytes.Count)
            {
                code = 0x00;
                for (int i = 0; i < (codeLen + 1); i++)
                {
                    bit = ((bytes[bI] & mask) != 0) ? 1 : 0;
                    mask <<= 1;

                    if (mask == 0x100)
                    {
                        mask = 0x01;
                        bI++;
                        if (bI >= bytes.Count) { break; }
                    }
                    code |= (bit << i);
                }

                if (code == CLEAR_CODE)
                {
                    codeLen = lzwSize;

                    dictI = 0;
                    table = new LZWEntry[(1 << (codeLen + 1))];
                    for (int i = 0; i < (1 << (codeLen)); i++)
                    {
                        LZWEntry ent = new LZWEntry();
                        ent.data = (byte)dictI;
                        ent.prev = -1;
                        ent.len = 1;

                        table[i] = ent;
                        dictI++;
                    }
                    dictI++;
                    dictI++;

                    prev = -1;

                    continue;
                }
                else if (code == STOP_CODE) { break; }

                if (prev > -1 && codeLen < 12)
                {
                    if (code == dictI)
                    {
                        int ptr = prev;
                        ref var tbl = ref table[ptr];

                        while (tbl.prev != -1)
                        {
                            ptr = tbl.prev;
                            tbl = ref table[ptr];
                        }
                        table[dictI].data = tbl.data;
                    }
                    else
                    {
                        int ptr = code;
                        ref var tbl = ref table[ptr];

                        while (tbl.prev != -1)
                        {
                            ptr = tbl.prev;
                            tbl = ref table[ptr];
                        }

                        if (dictI >= table.Length) { return; }

                        table[dictI].data = tbl.data;
                    }

                    ref var dTB = ref table[dictI];
                    dTB.prev = prev;
                    dTB.len = table[prev].len + 1;
                    dictI++;

                    if (dictI >= table.Length && codeLen < 11)
                    {
                        codeLen++;
                        Array.Resize(ref table, (1 << (codeLen + 1)));
                    }
                }

                prev = code;
                matchL = table[code].len;

                int pr = -1;
                while (code != -1)
                {
                    pr = code;
                    int ii = pos + table[code].len - 1;

                    if (ii >= 0 && ii < indices.Length)
                    {
                        indices[ii] = table[code].data;
                    }
                    code = table[code].prev;

                    if (code == pr) { break; }
                }

                pos += matchL;
            }
        }

        private struct LZWEntry
        {
            public byte data;
            public int prev;
            public int len;
        }

        public void Dispose()
        {
            _palette = null;

            _frames.Clear();

            _stream.Dispose();
            _stream.Close();

            _br.Close();
        }
    }
}
