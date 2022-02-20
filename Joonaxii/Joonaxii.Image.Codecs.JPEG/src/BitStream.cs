using System;
using System.IO;

namespace Joonaxii.Image.Codecs.JPEG
{
    public class BitStream : IDisposable
    {
        public bool CanRead { get => _stream.Position < _stream.Length | _bitPos < 8; }

        private Stream _stream;
        private bool _leaveOpen;

        private byte _buffer;
        private byte _bitPos;

        public BitStream(Stream stream) : this (stream, false){ }
        public BitStream(Stream stream, bool leaveOpen)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;
            _bitPos = 8;
        }

        public BitStream(byte[] stream) : this(new MemoryStream(stream), false) { }

        public void Reset()
        {
            _bitPos = 8;
            _buffer = 0;
        }

        public byte GetBit()
        {
            if(_bitPos > 7)
            {
                _buffer = (byte)_stream.ReadByte();
                _bitPos = 0;
            }
            return (byte)((_buffer >> (_bitPos++)) & 0x1);
        }

        public int GetBitN(int l)
        {
            int v = 0;
            for (int i = 0; i < l; i++)
            {
                v = v * 2 + GetBit();
            }
            return v;
        }

        public void Dispose()
        {
            if (_leaveOpen) { return; }
            _stream.Dispose();
        }
    }
}