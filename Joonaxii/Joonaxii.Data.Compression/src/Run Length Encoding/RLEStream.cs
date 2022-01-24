using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Data.Compression.RLE
{
    public class RLEStream : Stream
    {
        public override bool CanRead  { get => _canSeek; }
        public override bool CanSeek  { get => _canRead; }
        public override bool CanWrite { get => _canWrite; }

        public override long Length => 0;

        public override long Position { get => _position; set => _position = value; }

        private long _position;
        private bool _canSeek;
        private bool _canRead;
        private bool _canWrite;

        public RLEStream(Stream inputStream, RLEMode mode)
        {

        }

        public override void Flush()
        {
           
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
            
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            
        }
    }
}
