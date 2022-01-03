using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Joonaxii.Data.Compression.Huffman
{
    internal class HuffmanBranch : HuffmanNode
    {
        public override bool IsLeaf => false;

        public override long Value { get => frequency; set => frequency = (uint)value; }
        public override HuffmanNode Left { get => _left; set => _left = value; }
        public override HuffmanNode Right { get => _right; set => _right = value; }

        private HuffmanNode _left;
        private HuffmanNode _right;

        public HuffmanBranch(uint frequency) : base(frequency) { }
        public HuffmanBranch(IEnumerable<HuffmanTrace> leaves) : base(leaves) { }
        public HuffmanBranch(uint frequency, HuffmanNode l, HuffmanNode r) : base(frequency, l, r) { }
    }
}