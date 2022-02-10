using Joonaxii.Collections;
using Joonaxii.Data.Coding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Audio
{
    public abstract class AudioEncoderBase : CodecBase
    {
        public int NumChannels
        {
            get => _numChannels;
            set
            {
                _numChannels = (ushort)(value < 1 ? 1 : value > ushort.MaxValue ? ushort.MaxValue : value);
            }
        } 
        protected ushort _numChannels;

        public int SampleDataWritten { get => _totalData; }

        public BufferList Samples { get => _samples; }
        protected BufferList _samples;

        public int SampleRate { get => _sampleRate; }
        public short BitsPerSample { get => _bitsPerSample; }

        protected int _sampleRate;
        protected short _bitsPerSample;

        protected BinaryWriter _bw;
        protected Stream _stream;
        protected bool _leaveOpen;

        protected int _totalData;

        public AudioEncoderBase(Stream outStream) : this(outStream, false) 
        { _samples = new BufferList(); }
        public AudioEncoderBase(Stream outStream, bool leaveOpen)
        {
            _totalData = 0;

            _numChannels = 1;
            _stream = outStream;
            _leaveOpen = leaveOpen;
            _bw = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen);

            _samples = new BufferList();
        }

        public virtual void Setup(int numChannels, int sampleRate, short bitsPerSample)
        {
            NumChannels = numChannels;
            _sampleRate = sampleRate;
            _bitsPerSample = bitsPerSample;

            _samples.SetValueSize((byte)(_bitsPerSample >> 3));
        }

        public virtual void SetSamples(byte[] sampleData)
          => _samples = new BufferList(sampleData, (byte)(_bitsPerSample >> 3));

        public void SetSamples(short[] sampleData)
            => _samples = new BufferList(sampleData, (byte)(_bitsPerSample >> 3));

        public void SetSamples(int[] sampleData)
           => _samples = new BufferList(sampleData, (byte)(_bitsPerSample >> 3));

        public void SetSamples(BufferList samples) => _samples = new BufferList(samples);

        public abstract void WriteStaticData();
        protected abstract void WriteData();

        public override void Dispose()
        {
            FlushData();
            WritePostData();

            DisposeData();
            _bw.Dispose();
            if (!_leaveOpen)
            {
                _stream.Dispose();
                _stream.Close();
            }
        }

        public virtual void FlushData()
        {
            if (_samples.Count > 0)
            {
                _totalData += _samples.ByteSize;
                _samples.WriteToStream(_stream, true);
            }
        }

        public virtual void WritePostData() { }

        protected virtual void DisposeData()
        {
            _samples.Clear();
        }
    }
}
