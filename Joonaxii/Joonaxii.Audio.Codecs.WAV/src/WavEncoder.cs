using Joonaxii.Collections;
using Joonaxii.Data;
using System;
using System.IO;
using System.Text;

namespace Joonaxii.Audio.Codecs.WAV
{
    public class WavEncoder : AudioEncoderBase
    {
        private long _posTotalSize;
        private long _posDataSize;

        public WavEncoder(Stream outStream) : this(outStream, false)
        {
            _posTotalSize = -1;
            _posDataSize = -1;
        }
        public WavEncoder(Stream outStream, bool leaveOpen) : base(outStream, leaveOpen)
        {
            _posTotalSize = -1;
            _posDataSize = -1;
        }

        public override void WriteStaticData()
        {
            if (!HeaderManager.TryGetHeader(HeaderType.WAVE, out var hdr))
            {
                hdr = new MagicHeader(HeaderType.WAVE, new MagicByte[] {
                0x52, 0x49, 0x46, 0x46,      //RIFF
                0xF00, 0xF00, 0xF00, 0xF00,  //CHUNK_SIZE (0x??_??_??_??)
                0x57, 0x41, 0x56, 0x45,});   //WAVE
            }
            _posTotalSize = _stream.Position + 4;
            hdr.WriteHeader(_bw);

            _bw.Write(Encoding.ASCII.GetBytes("fmt "));
            _bw.Write(16);

            _bw.Write((short)1); //PCM ONLY
            _bw.Write(_numChannels);

            int bytConvers = _numChannels * (_bitsPerSample >> 3);
            _bw.Write(_sampleRate);
            _bw.Write(_sampleRate * bytConvers);
            _bw.Write((short)bytConvers);
            _bw.Write(_bitsPerSample);

            WriteData();
        }

        protected override void WriteData()
        {
            _bw.Write(Encoding.ASCII.GetBytes("data"));
            _posDataSize = _stream.Position;
            _bw.Write(0);
        }

        public override void WritePostData()
        {
            base.WritePostData();
            if(_posDataSize > - 1)
            {
                _stream.Seek(_posTotalSize, SeekOrigin.Begin);
                _bw.Write(_totalData + 36);

                _stream.Seek(_posDataSize, SeekOrigin.Begin);
                _bw.Write(_totalData);

                _posTotalSize = -1;
                _posDataSize = -1;
            }
        }

        protected override void DisposeData()
        {
            base.DisposeData();
        }
    }
}
