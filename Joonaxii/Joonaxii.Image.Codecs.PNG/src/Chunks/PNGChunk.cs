using System;
using System.Collections.Generic;
using System.IO;
using Joonaxii.IO;

namespace Joonaxii.Image.Codecs.PNG
{
    public abstract class PNGChunk
    {
        public bool IsValid { get; private set; }
        public int length;
        public PNGChunkType chunkType;

        public byte[] data;
        public uint crc;

        public PNGChunk() { }
        public PNGChunk(int len, PNGChunkType type, byte[] data, uint crc)
        {
            length = len;
            chunkType = type;
            this.data = data;
            this.crc = crc;
        }

        public static PNGChunk Read(BinaryReader br)
        {
            int length = br.ReadInt32BigEndian();
            PNGChunkType chunkType = (PNGChunkType)br.ReadInt32BigEndian();
            var data = br.ReadBytes(length);
            uint crc = br.ReadUInt32BigEndian();

            var chnk = GetSpecificChunk(length, chunkType, data, crc);
            chnk.IsValid = crc == chnk.GetCrc();
            return chnk;
        }

        public PNGChunk Write(BinaryWriter bw)
        {
            bw.WriteBigEndian(length);
            bw.WriteBigEndian((int)chunkType);
            bw.Write(data, 0, length);
            bw.WriteBigEndian(crc);
            return this;
        }

        public static PNGChunk GetSpecificChunk(int length, PNGChunkType type, byte[] data, uint crc)
        {
            switch (type)
            {
                default: return new RawChunk(length, type, data, crc);

                case PNGChunkType.IHDR: return new IHDRChunk(length, data, crc);
                case PNGChunkType.pHYs: return new PHYSChunk(length, data, crc);
                case PNGChunkType.IDAT: return new IDATChunk(length, data, crc);
                case PNGChunkType.iCCP: return new ICCPChunk(length, data, crc);
                case PNGChunkType.sPLT: return new SPLTChunk(length, data, crc);
                case PNGChunkType.PLTE: return new PLTEChunk(length, data, crc);
                case PNGChunkType.gAMA: return new gAMAChunk(length, data, crc);
                case PNGChunkType.tRNS: return new tRNSChunk(length, data, crc);
            }
        }

        public MemoryStream GetStream() => new MemoryStream(data);

        public virtual string ToMinString() => $"[{chunkType}, {length} bytes, {crc}]";

        public override string ToString() => $"Type: {chunkType}, Data: {length} bytes, CRC: {crc}\n{GetSpedcificInfoString()}";
        public abstract string GetSpedcificInfoString();

        private static uint[] _crcTable;
        public static uint CalcualteCrc(byte[] bytes, int start, int len)
        {
            uint c;
            if (_crcTable == null)
            {
                _crcTable = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    c = i;
                    for (int k = 0; k < 8; k++)
                    {
                        if ((c & 1) == 1)
                        {
                            c = 0xEDB88320 ^ (c >> 1);
                            continue;
                        }
                        c >>= 1;
                    }
                    _crcTable[i] = c;
                }
            }

            c = 0xFFFFFFFF;
            for (int i = start; i < start + len; i++)
            {
                c = _crcTable[(c ^ bytes[i]) & 0xFF] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFF;
        }

        public uint GetCrc()
        {
            byte[] local = new byte[length + 4];
            uint typ = (uint)chunkType;

            for (int i = 0; i < 4; i++)
            {
                local[3 - i] = (byte)((typ >> (i << 3)) & 0xFF);
            }
   
            Buffer.BlockCopy(data, 0, local, 4, length);
            return CalcualteCrc(local, 0, length + 4);
        }
    }
}
