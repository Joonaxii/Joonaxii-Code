
using Joonaxii.Collections;
using Joonaxii.Data.Coding;
using System.IO;

namespace Joonaxii.Audio
{
    public abstract class AudioDecoderBase : CodecBase
    {
        public virtual int SampleCount { get => _samples != null ? _samples.Count : 0; }
        public abstract uint SampleRate { get; }
        public abstract uint Channels { get; }
        public abstract uint BitRate { get; }
        public abstract byte BitDepth { get; }

        protected BufferList _samples;
        protected bool _leaveOpen;
        protected Stream _stream;

        public AudioDecoderBase(Stream stream) : this(stream, false) { }

        public AudioDecoderBase(Stream stream, bool leaveOpen)
        {
            _samples = null;
            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        public abstract AudioDecodeResult Decode();

        public ulong GetSample(int index) => _samples == null ? 0 :  _samples[index];

        public override void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposed) 
        {
            if (!_leaveOpen)
            {
                _stream.Dispose();
            }

            if(_samples != null)
            {
                _samples.UnPin();
                _samples.Clear();
            }
        }
    }
}
