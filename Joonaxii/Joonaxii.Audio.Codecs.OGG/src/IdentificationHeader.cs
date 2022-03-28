using System.IO;

namespace Joonaxii.Audio.Codecs.OGG
{
    public struct IdentificationHeader
    {
        public uint vorbisVersion;
        public byte audioChannels;
        public uint sampleRate;

        public int maxBitRate;
        public int nominalBitRate;
        public int minBitRate;

        public ushort blockSize0;
        public ushort blockSize1;

        public byte framingFlag;

        public IdentificationHeader(BinaryReader br)
        {
            vorbisVersion = br.ReadUInt32();
            audioChannels = br.ReadByte();

            sampleRate = br.ReadUInt32();
            maxBitRate = br.ReadInt32();
            nominalBitRate = br.ReadInt32();
            minBitRate = br.ReadInt32();

            byte block = br.ReadByte();
            blockSize0 = (ushort)(1 << (block & 0xF));
            blockSize1 = (ushort)(1 << ((block & 0xF0) >> 4));
            framingFlag = br.ReadByte();
        }
    }
}