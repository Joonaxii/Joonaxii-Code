using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Joonaxii.Collections;
using Joonaxii.Data.Coding;
using Joonaxii.MathJX;

namespace Joonaxii.Image.Codecs.GIF
{
    public class GIFDecoder : ImageDecoderBase
    {
        public int FrameCount { get => _frames.Count; }

        public bool HasAlpha { get => _hasAlpha; }
        public bool IsValidIndexed { get => _palette.Length < 256; }

        private List<GIFFrame> _frames = new List<GIFFrame>();
        private bool _hasAlpha;

        private FastColor[] _palette;
        private int _header;

        private int _activeFrame;

        private byte _bgIndex;
        private int _numOfEntries;

        public GIFDecoder(Stream stream) : this(stream, false, false) { }
        public GIFDecoder(Stream stream, bool readHeader, bool is89) : base(stream)
        {
            _header = readHeader ? is89 ? 2 : 1 : 0;
            _activeFrame = 0;
        }

        public GIFDecoder(BinaryReader br, bool dispose) : this(br, false, false, dispose) { }
        public GIFDecoder(BinaryReader br, bool readHeader, bool is89, bool dispose) : base(br, dispose)
        {
            _header = readHeader ? is89 ? 2 : 1 : 0;
            _activeFrame = 0;
        }

        public GIFFrame GetActiveFrame() => _frames[_activeFrame];

        public void SetActiveFrame(int frame)
        {
            if(frame < 0 | frame >= _frames.Count | !IsDecoded) { return; }
            _activeFrame = frame;
            _texture = _frames[frame].GetTexture();
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
        private GIFFrame GetFrame(BinaryReader br, byte bgColor, int width, int height, FastColor[] table, ref int mode, HashSet<FastColor> palette, GIFFrame previous, GIFFrame first)
        {
            byte f = br.ReadByte();
            if (f == 0x3b || br.BaseStream.Position >= br.BaseStream.Length) { return null; }

            bool hasAlpha = false;
            byte alphaInd = 0;
            byte dMet = 0;
            GIFFrame frame = null;
            ReadFrameInfo(f, br, palette, (GIFFrame fR) => frame = fR, out hasAlpha, out dMet, out alphaInd, out ushort delay);

            if (frame == null) { return null; }
            var tex = frame.GetTexture();

            var bg = table.Length < 1 ? default : table[bgColor];
            bool skipAlpha = false;
            switch (mode)
            {
                case 0x00:
                    bg.a = hasAlpha ? (byte)0 : bg.a;
                    tex.SetPixels(bg);
                    break;

                case 0x01:
                    skipAlpha = true;
                    previous?.CopyTo(frame);
                    break;

                case 0x02:
                    bg.a = hasAlpha ? (byte)0 : bg.a;
                    tex.SetPixels(bg);
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
                int yPP = yP * width;
                for (int xX = 0; xX < w; xX++)
                {
                    int xP = xX + x;
                    int ii = yPP + xP;
                    int i = yY * w + xX;

                    int ind = indices[i];
                    var clr = table[ind];
                    if (ind == alphaInd && hasAlpha)
                    {
                        if (skipAlpha) { continue; }
                        clr.a = 0;
                    }
                    tex.SetPixel(ii, clr);
                }
            }

            mode = dMet;
            return frame;
        }

        private void ReadFrameInfo(byte f, BinaryReader br, HashSet<FastColor> palette, Action<GIFFrame> getFrame, out bool hasAlpha, out byte dMet, out byte alphaInd, out ushort delayTime)
        {
            hasAlpha = false;
            dMet = 0;
            alphaInd = 0;
            delayTime = 0;

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

                            delayTime = br.ReadUInt16();
                            if (hasAlpha)
                            {
                                _hasAlpha = true;
                                alphaInd = br.ReadByte();
                                palette?.Add(FastColor.clear);
                            }
                            br.ReadByte();

                            if(getFrame != null)
                            {
                                getFrame.Invoke(new GIFFrame((ushort)_general.width, (ushort)_general.height, delayTime));
                            }
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

        public override void Dispose()
        {
            _palette = null;
            for (int i = 0; i < _frames.Count; i++)
            {
                _frames[i].Dispose();
            }
            _frames.Clear();
            base.Dispose();
        }

        public override ImageDecodeResult Decode(bool skipHeader)
        {
            FastColor[] globalPalette;
            _hasAlpha = false;
    
            #region Header
            if (_header == 0 & !skipHeader)
            {
                string hdr = Encoding.ASCII.GetString(_br.ReadBytes(3));
                if (hdr != "GIF") { return ImageDecodeResult.InvalidImageFormat; }
                string ver = Encoding.ASCII.GetString(_br.ReadBytes(3));

                switch (ver)
                {
                    default:
                        return ImageDecodeResult.InvalidImageFormat;
                    case "87a":
                        _header = 1;
                        break;
                    case "89a":
                        _header = 2;
                        break;
                }
            }
            HashSet<FastColor> palette = new HashSet<FastColor>();
            LoadGeneralTextureInfo(_br);

            palette.Add(FastColor.clear);

            globalPalette = _numOfEntries < 1 ? new FastColor[0] : new FastColor[_numOfEntries];
            for (int i = 0; i < _numOfEntries; i++)
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
                var frame = GetFrame(_br, _bgIndex, _general.width, _general.height, globalPalette, ref mode, palette, prev, first);
                if (frame == null) { break; }

                prev = frame;
                if (_frames.Count < 1) { first = prev; }
                _frames.Add(frame);
            }

            if (!_hasAlpha) { palette.Remove(FastColor.clear); }
            _palette = new FastColor[palette.Count];
            palette.CopyTo(_palette);

            SetActiveFrame(_activeFrame);
            return ImageDecodeResult.Success;
        }

        public override void LoadGeneralInformation(long pos)
        {
            long posSt = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);
            LoadGeneralTextureInfo(_br);
            _stream.Seek(posSt, SeekOrigin.Begin);
        }

        public override int GetDataCRC(long pos)
        {
            long posSt = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);
            LoadGeneralTextureInfo(_br);

            uint crc = CRC.CRC_START_VALUE;
            crc = CRC.ProgAdd(crc, _stream, _numOfEntries * 3);
         
            while(_stream.Position < _stream.Length)
            {
                byte bt = _br.ReadByte();
                if(bt == 0x3b) { break; }

                ReadFrameInfo(bt, _br, null, null, out bool hasAlpha, out byte dMet, out byte alphaInd, out ushort delay);
         
                ushort x = _br.ReadUInt16();
                ushort y = _br.ReadUInt16();

                ushort w = _br.ReadUInt16();
                ushort h = _br.ReadUInt16();

                byte imgInfo = _br.ReadByte();

                byte sizeLocal = Maths.GetRange(imgInfo, 0, 3);
                bool hasLocal = Maths.IsBitSet(imgInfo, 7);

                crc = CRC.ProgAdd(crc, x);
                crc = CRC.ProgAdd(crc, y);

                crc = CRC.ProgAdd(crc, w);
                crc = CRC.ProgAdd(crc, h);
                crc = CRC.ProgAdd(crc, imgInfo);

                if (hasLocal)
                {
                    int l = 1 << (sizeLocal + 1);
                    crc = CRC.ProgAdd(crc, _stream, l * 3);
                }

                byte size = _br.ReadByte();
                byte b = _br.ReadByte();

                crc = CRC.ProgAdd(crc, size);
                crc = CRC.ProgAdd(crc, b);
                while (b != 0x00)
                {
                    for (int i = 0; i < b; i++)
                    {
                        crc = CRC.ProgAdd(crc, _br.ReadByte());
                    }
                    b = _br.ReadByte();
                    crc = CRC.ProgAdd(crc, b);
                    if (b == 0) { break; }
                }
            }
            _stream.Seek(posSt, SeekOrigin.Begin);
            return (int)CRC.ProgEnd(crc);
        }

        protected override ImageDecodeResult LoadGeneralTextureInfo(BinaryReader br)
        {
            _general.width = br.ReadUInt16();
            _general.height = br.ReadUInt16();
            _general.bitsPerPixel = 8;

            byte mapInfo = br.ReadByte();
            _bgIndex = br.ReadByte();
            byte aspect = br.ReadByte();

            _numOfEntries = Maths.IsBitSet(mapInfo, 7) ? 1 << (Maths.GetRange(mapInfo, 0, 3) + 1) : 0;
            //int aspectRatio = (aspect + 15) / 64;
            return ImageDecodeResult.Success;
        }
    }
}
