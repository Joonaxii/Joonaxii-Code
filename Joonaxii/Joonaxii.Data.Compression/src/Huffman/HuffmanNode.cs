using System;
using System.Collections.Generic;

namespace Joonaxii.Data.Compression.Huffman
{
    internal abstract class HuffmanNode : IComparable<HuffmanNode>
    {
        public abstract bool IsLeaf { get; }
        public bool IsRight { get => parent?.Right == this; }

        public uint frequency;
        public HuffmanNode parent;

        public abstract long Value { get; set; }
        public abstract HuffmanNode Left { get; set; }
        public abstract HuffmanNode Right { get; set; }

        public HuffmanNode(uint frequency) : this(frequency, null, null) { }
        public HuffmanNode(uint frequency, HuffmanNode l, HuffmanNode r)
        {
            this.frequency = frequency;

            Left = l;
            Right = r;

            if(Left != null)
            {
                Left.parent = this;
            }

            if (Right != null)
            {
                Right.parent = this;
            }
        }

        public HuffmanNode(IEnumerable<HuffmanTrace> leaves)
        {
            HuffmanNode cur;

            foreach (var trace in leaves)
            {
                cur = this;
                if(trace.bits.Count < 1) { continue; }
                while (trace.bits.Count > 0)
                {
                    bool bit = trace.bits.Dequeue();
                    bool isLast = trace.bits.Count < 1;

                    if (bit)
                    {
                        cur.Right = cur.Right == null ? (isLast ? new HuffmanLeaf(0, 0) : new HuffmanBranch(0, null, null) as HuffmanNode) : cur.Right;
                        cur = cur.Right;
                        continue;
                    }

                    cur.Left = cur.Left == null ? (isLast ? new HuffmanLeaf(0, 0) : new HuffmanBranch(0, null, null) as HuffmanNode) : cur.Left;
                    cur = cur.Left;
                }
                cur.Value = trace.value;
            }
        }

        public void GetAllLeaves(Dictionary<long, HuffmanNode> nodes, out ulong largestValue, out int maxDepth)
        {
            Stack<HuffmanNode> stack = new Stack<HuffmanNode>();
            stack.Push(this);
            largestValue = 0;
            maxDepth = 0;

            while (stack.Count > 0)
            {
                HuffmanNode node = stack.Pop();

                if(node.Left != null) { stack.Push(node.Left); }
                if(node.Right != null) { stack.Push(node.Right); }

                if (node.IsLeaf)
                {
                    ulong va = (ulong)node.Value;
                    largestValue = largestValue < va ? va : largestValue;
                    nodes.Add(node.Value, node);

                    var depth = node.GetDepth();
                    maxDepth = maxDepth < depth ? depth : maxDepth;
                }
            }
        }

        public int GetDepth()
        {
            int depth = 0;
            var par = parent;
            while (par != null)
            {
                par = par.parent;
                depth++;
            }
            return depth;
        }

        public bool TraceByValue(long value, Stack<bool> bits) => TraceNode(FindNode(value), bits);
        public static bool TraceNode(HuffmanNode node, Stack<bool> bits)
        {
            if (node != null)
            {
                var par = node.parent;
                while (par != null)
                {
                    bits.Push(node.IsRight);
                    node = par;
                    par = par.parent;
                }
                return true;
            }
            return false;
        }

        public static bool TraceNode(HuffmanNode node, Action<bool> onWrite)
        {
            if (node != null)
            {
                var par = node.parent;
                while (par != null)
                {
                    onWrite.Invoke(node.IsRight);
                    node = par;
                    par = par.parent;
                }
                return true;
            }
            return false;
        }

        public HuffmanNode FindNode(long value)
        {
            Stack<HuffmanNode> stack = new Stack<HuffmanNode>();
            stack.Push(this);

            while(stack.Count > 0)
            {
                HuffmanNode node = stack.Pop();

                if (node.Left != null) { stack.Push(node.Left); }
                if (node.Right != null) { stack.Push(node.Right); }

                if(node.IsLeaf && node.Value == value) { return node; }
            }
            return null;
        }

        public HuffmanNode SelectNode(bool right) => IsLeaf ? this : right ? Right : Left;

        public long GetValue(Queue<bool> bits)
        {
            HuffmanNode cur = this;
            while(bits.Count > 0 && !cur.IsLeaf)
            {
                bool bit = bits.Dequeue();
                cur = bit ? cur.Right : cur.Left;
            }
            return cur.Value;
        }

        public override string ToString() => $"Value: {Value}, Frequency: {frequency}";

        public int CompareTo(HuffmanNode other) => frequency.CompareTo(other.frequency);
    }
}
