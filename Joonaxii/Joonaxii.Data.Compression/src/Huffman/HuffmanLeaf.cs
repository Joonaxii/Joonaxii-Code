namespace Joonaxii.Data.Compression.Huffman
{
    internal class HuffmanLeaf : HuffmanNode
    {
        public override bool IsLeaf => true;

        public override long Value { get => _value; set => _value = value; }
        public override HuffmanNode Left { get => null; set { } }
        public override HuffmanNode Right { get => null; set { } }

        private long _value;

        public HuffmanLeaf(long value, uint frequency) : base(frequency)
        {
            _value = value;
        }
    }
}