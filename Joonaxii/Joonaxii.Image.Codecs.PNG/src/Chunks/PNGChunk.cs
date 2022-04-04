using System;
using System.IO;
using Joonaxii.Collections;
using Joonaxii.Data.Coding;
using Joonaxii.IO;
using Joonaxii.MathJX;

namespace Joonaxii.Image.Codecs.PNG
{
    public class PNGChunk
    {
        public bool IsValid { get; private set; }
  
        public int length;
        public PNGChunkType chunkType;

        public long dataStart;
        public uint crc;

        public PNGChunk() { }
        public PNGChunk(int len, PNGChunkType type, uint crc, long dataPos)
        {
            length = len;
            chunkType = type;
            dataStart = dataPos;
            this.crc = crc;
        }

        public static PNGChunk Read(BinaryReader br, Stream stream, Func<PNGChunkType, PNGChunk> skip = null)
        {
            int length = br.ReadInt32BigEndian();
            PNGChunkType chunkType = (PNGChunkType)br.ReadInt32BigEndian();

            var skipChnk = skip?.Invoke(chunkType);
            if (skipChnk != null)
            {
                stream.Seek(length + 4, SeekOrigin.Current);
                return skipChnk;
            }

            long dataStart = stream.Position;

            stream.Seek(-4, SeekOrigin.Current);
            var testCRC = CRC.Calculate(stream, length + 4);
            uint crc = br.ReadUInt32BigEndian();

            stream.Seek(dataStart, SeekOrigin.Begin);
            var chnk = GetSpecificChunk(br, length, chunkType, crc, dataStart);
            stream.Seek(dataStart + length + 4, SeekOrigin.Begin);
            chnk.IsValid = crc == testCRC;
            return chnk;
        }

        public static uint ReadCrc(BinaryReader br, uint crc, Stream stream, out PNGChunkType chunkType, Func<PNGChunkType, bool> skip = null)
        {
            int length = br.ReadInt32BigEndian();
            chunkType = (PNGChunkType)br.ReadInt32BigEndian();

            if (skip != null && skip.Invoke(chunkType))
            {
                stream.Seek(length + 4, SeekOrigin.Current);
                return crc;
            }

            stream.Seek(length, SeekOrigin.Current);
            return CRC.ProgAdd(crc, br.ReadUInt32BigEndian());
        }

        public static void Write(BinaryWriter bw, PNGChunkType type, byte[] data, int offset, int length)
        {
            unsafe
            {
                bw.WriteBigEndian(length);
                bw.WriteBigEndian((uint)type);
                for (int i = offset; i < offset + length; i++) { bw.Write(data[i]); }
                bw.WriteBigEndian(CRC.Calculate(IOExtensions.ReverseBytes((uint)type), 4, data, offset, length));
            }
        }

        internal static unsafe void Write(BinaryWriter bw, PNGChunkType type, byte* data, int length)
        {
            unsafe
            {
                bw.WriteBigEndian(length);
                bw.WriteBigEndian((uint)type);
                for (int i = 0; i < length; i++) { bw.Write(data[i]); }
                bw.WriteBigEndian(CRC.Calculate(IOExtensions.ReverseBytes((uint)type), 4, data, 0, length));
            }
        }

        internal static unsafe void Write(BinaryWriter bw, byte* data, int length)
        {
            bw.WriteBigEndian(length);
            length += 4;
            for (int i = 0; i < length; i++) { bw.Write(data[i]); }
            bw.WriteBigEndian(CRC.Calculate(data, 0, length));
        }

        public static PNGChunk GetSpecificChunk(BinaryReader br, int length, PNGChunkType type, uint crc, long dataPos)
        {
            switch (type)
            {
                default: return new PNGChunk(length, type, crc, dataPos);

                case PNGChunkType.IHDR: return new IHDRChunk(br, length, crc);
                case PNGChunkType.pHYs: return new PHYSChunk(br, length, crc);
                case PNGChunkType.iCCP: return new ICCPChunk(br, length, crc);
                case PNGChunkType.sPLT: return new SPLTChunk(br, length, crc);
                case PNGChunkType.PLTE: return new PLTEChunk(br, length, crc);
                case PNGChunkType.gAMA: return new gAMAChunk(br, length, crc);
                case PNGChunkType.tRNS: return new tRNSChunk(length, crc, dataPos);
            }
        }

        public virtual string ToMinString() => $"[{chunkType}, {length} bytes, {crc}]";

        public override string ToString() => $"Type: {chunkType}, Data: {length} bytes, CRC: {crc}\n{GetSpedcificInfoString()}";
        public virtual string GetSpedcificInfoString() => "";
    }
}
