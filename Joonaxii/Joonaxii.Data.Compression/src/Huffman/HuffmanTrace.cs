using System.Collections.Generic;

namespace Joonaxii.Data.Compression.Huffman
{
    internal struct HuffmanTrace
    {
        public long value;
        public Queue<bool> bits;

        public HuffmanTrace(long value, Stack<bool> trace)
        {
            this.value = value;
            this.bits = new Queue<bool>();

            while(trace.Count > 0)
            {
                this.bits.Enqueue(trace.Pop());
            }
        }

        public HuffmanTrace(long value, Queue<bool> trace)
        {
            this.value = value;
            this.bits = new Queue<bool>(trace);
        }
    }
}