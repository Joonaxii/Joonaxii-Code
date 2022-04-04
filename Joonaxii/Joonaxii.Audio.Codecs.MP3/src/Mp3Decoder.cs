using Joonaxii.Collections;
using Joonaxii.Data.Coding;
using Joonaxii.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Audio.Codecs.MP3
{
    public class Mp3Decoder : AudioDecoderBase
    {
        public override uint SampleRate => _sampleRate;
        public override uint Channels => _channels;
        public override byte BitDepth => 32;
        public override uint BitRate => _bitRate;

        private Mp3Info _headerInfo = Mp3Info.Zero;

        private uint _sampleRate;
        private uint _channels;
        private uint _bitRate;

        public Mp3Decoder(Stream stream) : this(stream, false)
        {
        }

        public Mp3Decoder(Stream stream, bool leaveOpen) : this(stream, leaveOpen, Mp3Info.Zero) { }
        public Mp3Decoder(Stream stream, Mp3Info header) : this(stream, false, header) { }
        public Mp3Decoder(Stream stream, bool leaveOpen, Mp3Info header) : base(stream, leaveOpen)
        {
            _sampleRate = 0;
            _channels = 0;
            _bitRate = 0;
            _headerInfo = header;
        }

        public override AudioDecodeResult Decode()
        {
            if(!IsFileMp3(_stream, out _headerInfo)) { return AudioDecodeResult.InvalidFormat; }
            return AudioDecodeResult.Success;
        }

        public override void LoadGeneralInformation(long pos)
        {
            if(_headerInfo != Mp3Info.Zero) { return; }
            long posSt = _stream.Position;

            _stream.Seek(pos, SeekOrigin.Begin);
            IsFileMp3(_stream, out _headerInfo);
            _stream.Seek(posSt, SeekOrigin.Begin);
        }

        public override int GetDataCRC(long pos)
        {
            if (_headerInfo != Mp3Info.Zero) { return (int)_headerInfo.crc; }
            long posSt = _stream.Position;

            _stream.Seek(pos, SeekOrigin.Begin);
            IsFileMp3(_stream, out _headerInfo);
            _stream.Seek(posSt, SeekOrigin.Begin);

            return (int)_headerInfo.crc;
        }

        public static bool IsFileMp3(Stream stream, out Mp3Info info)
        {
            bool isMp3 = false;
            info = Mp3Info.Zero;
            long pos = stream.Position;

            PinnableList<uint> crcs = new PinnableList<uint>(64);
            using (BinaryReader br = new BinaryReader(stream, Encoding.UTF8, true))
            {
                int hdr = br.ReadInt32BigEndian();
                if(((hdr >> 8) & 0xFFFFFF) == 0x49_44_33)
                {
                    var rev = br.ReadUInt16();
                    bool hasExtended = (rev & 0b01000000) != 0;

                    byte[] sizeD = br.ReadBytes(4);

                    uint size = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        size += (uint)sizeD[3 - i] << (i * 7);
                    }
                    isMp3 = true;

                    stream.Seek(size, SeekOrigin.Current);
                }
                else
                {
                    stream.Seek(-4, SeekOrigin.Current);
                }

                long total = 0;
                while (stream.Position < stream.Length)
                {
                    var valA = br.ReadByte();
                    if(valA != 0xFF) { continue; }
                    valA = br.ReadByte();

                    if((valA & 0xF0) != 0xF0) { continue; }

                    uint val = (uint)valA;

                    isMp3 = true;
                    bool hasCRC = (val & 0x1) == 0;
                    val = br.ReadByte();

                    var bitR = (val & 0xF0U) >> 4;
                    if(bitR == 0xF) { continue; }

                    uint bitRate = GetBitRate(bitR);
          
                    uint sampleRate = (val >> 2) & 0x3;
                    switch (sampleRate)
                    {
                        case 0x00:
                            sampleRate = 44100;
                            break;
                        case 0x01:
                            sampleRate = 48000;
                            break;
                        case 0x02:
                            sampleRate = 32000;
                            break;
                        case 0x03:
                            continue;
                    }

                    info.bitRate = info.bitRate < bitRate ? bitRate : info.bitRate;
                    info.sampleRate = info.sampleRate < sampleRate ? sampleRate : info.sampleRate;

                    byte padding = (byte)((val >> 1) & 0x1);
                    val = br.ReadByte();

                    bool isStereo = ((val & 0xC0) >> 6) != 0x3;
                    info.stereo |= isStereo;

                    if (hasCRC)
                    {
                        stream.Seek(2, SeekOrigin.Current);
                    }

                    stream.Seek(-4, SeekOrigin.Current);
                    int frameLen = (int)((144 * bitRate / sampleRate) + padding);
                    crcs.Add(CRC.Calculate(stream, frameLen));
                    total += frameLen;
                }

                if(crcs.Count > 0)
                {
                    unsafe
                    {
                        byte* data = (byte*)crcs.Pin();
                        info.crc = CRC.Calculate(data, 0, crcs.Count * sizeof(uint));
                        crcs.UnPin();
                    }
                }
            }
            stream.Seek(pos, SeekOrigin.Begin);
            return isMp3;
        }

        private static uint GetBitRate(uint bitRate)
        {
            switch (bitRate)
            {
                default: return 0;
                case 0x1: return 32_000;
                case 0x2: return 40_000;
                case 0x3: return 48_000;
                case 0x4: return 56_000;
                case 0x5: return 64_000;
                case 0x6: return 80_000;
                case 0x7: return 96_000;
                case 0x8: return 112_000;
                case 0x9: return 128_000;
                case 0xA: return 160_000;
                case 0xB: return 192_000;
                case 0xC: return 224_000;
                case 0xD: return 256_000;
                case 0xE: return 320_000;
                case 0xF: return 0x0;
            }
        }

        public static uint ParseSyncSafeUInt32(uint value)
        {
            return (((value & 0x7FU) << 28) | 
                (((value >> 8) & 0x7FU) << 14) | 
                (((value >> 16) & 0x7FU) << 7) | 
                (((value >> 24) & 0x7FU)));
        }

    }
}
