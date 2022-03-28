
using Joonaxii.Collections;
using Joonaxii.Data.Coding;

namespace Joonaxii.Audio
{
    public abstract class AudioDecoderBase : CodecBase
    {
        public virtual int SampleCount { get => _samples != null ? _samples.Count : 0; }
        private BufferList _samples;

        private int _numChannels;

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
