using Joonaxii.Collections;
using Joonaxii.Data.Coding;
using Joonaxii.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Audio.Codecs.WAV
{
    public class WavDecoder : AudioDecoderBase
    {
        private const uint DATA_HEX = 0x61_74_61_64;

        public override uint SampleRate => _sampleRate;
        public override uint Channels => _channels;
        public override byte BitDepth => throw new NotImplementedException();

        private uint _sampleRate;
        private uint _channels;
        private byte _bitDepth;
        private bool _decoded;
        private long _dataPos = -1;

        public WavDecoder(Stream stream) : this(stream, false)
        {
        }

        public WavDecoder(Stream stream, bool leaveOpen) : base(stream, leaveOpen)
        {
            _decoded = false;
            _dataPos = -1;
        }

        public override AudioDecodeResult Decode()
        {
            _decoded = false;
            using (BinaryReader br = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                var res = ReadHeader(br, false);
                if (res != AudioDecodeResult.Success) { return res; }

                if (_dataPos < 0)
                {

                }

                _stream.Seek(_dataPos, SeekOrigin.Begin);
                int length = br.ReadInt32();

                byte bytesPerSample = (byte)(_bitDepth >> 3);
                int count = length / bytesPerSample / (int)_channels;
                _samples = new BufferList(_stream, bytesPerSample, count);
            }
            _decoded = true;
            return AudioDecodeResult.Success;
        }

        public override void LoadGeneralInformation(long pos)
        {
            if (_decoded) { return; }
            long posSt = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);
            using (BinaryReader br = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                ReadHeader(br, true);
            }
            _stream.Seek(posSt, SeekOrigin.Begin);
        }

        public override int GetDataCRC(long pos)
        {
            int crc = 0;
            long posSt = _stream.Position;
            _stream.Seek(pos, SeekOrigin.Begin);
            using (BinaryReader br = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                if (_dataPos < 0)
                {
                    ReadHeader(br, true);
                }

                if(_dataPos >= 0)
                {
                    _stream.Seek(_dataPos, SeekOrigin.Begin);
                    int len = br.ReadInt32();
                    crc = (int)CRC.Calculate(_stream, len);
                }
            }
            _stream.Seek(posSt, SeekOrigin.Begin);
            return crc;
        }

        private bool CheckHeader(BinaryReader br, out int chunkSize)
        {
            uint hdrA = br.ReadUInt32();
            chunkSize = br.ReadInt32();
            uint hdrB = br.ReadUInt32();

            return hdrA == 0x46_46_49_52 & hdrB == 0x45_56_41_57;
        }

        private AudioDecodeResult ReadHeader(BinaryReader br, bool skipUnsupported)
        {
            if (!CheckHeader(br, out var chnk)) { return AudioDecodeResult.InvalidFormat; }
            _stream.Seek(20, SeekOrigin.Current);
            if (!skipUnsupported & br.ReadByte() != 1) { return AudioDecodeResult.Unsupported; }

            _channels = br.ReadUInt16();
            _sampleRate = br.ReadUInt32();

            _stream.Seek(6, SeekOrigin.Current);
            _bitDepth = (byte)br.ReadUInt16();

            long index = _stream.IndexOf(DATA_HEX);
            _dataPos = index < 0 ? -1 : index + 4;
            return index < 0 ? AudioDecodeResult.AudioCorruption : AudioDecodeResult.Success;
        }
    }
}
