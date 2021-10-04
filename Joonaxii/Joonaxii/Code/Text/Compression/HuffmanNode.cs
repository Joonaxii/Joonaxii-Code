using System;
using System.Collections.Generic;

namespace Joonaxii.Text.Compression
{
    public class HuffmanNode : IComparable<HuffmanNode>
    {
        public bool IsLeaf { get => value >= 0; }
        public bool IsNode1 { get => parent.node1 == this; }

        public int frequency;
        public int value;

        public int index;
        public HuffmanNode parent;

        public HuffmanNode node0;
        public HuffmanNode node1;

        public HuffmanNode(int frequency, int value)
        {
            this.frequency = frequency;
            this.value = value;

            node0 = null;
            node1 = null;
        }

        public HuffmanNode(int frequency, HuffmanNode l, HuffmanNode r)
        {
            this.frequency = frequency;
            this.value = -1;

            node0 = l;
            node1 = r;
        }

        public HuffmanNode(Huffman.HuffmanNodeTrace[] traceNodes)
        {
            this.frequency = -1;
            this.value = -1;

            node0 = null;
            node1 = null;

            HuffmanNode cur = this;
            for (int i = 0; i < traceNodes.Length; i++)
            {
                var nod = traceNodes[i];

                for (int j = 0; j < nod.bits.Length; j++)
                {
                    bool is1 = nod.bits[j];
                    if (is1)
                    {
                        cur.node1 = cur.node1 == null ? new HuffmanNode(-1, -1) : cur.node1;
                        cur = cur.node1;
                        continue;
                    }

                    cur.node0 = cur.node0 == null ? new HuffmanNode(-1, -1) : cur.node0;
                    cur = cur.node0;
                }

                cur.value = nod.value;
                cur = this;
            }
        }

        public HuffmanNode((int, bool)[] branches, (int, int, bool)[] leaves)
        {
            HuffmanNode[] nodes = new HuffmanNode[branches.Length + 1];
            nodes[0] = this;
            value = -1;
            frequency = 0;

            for (int i = 1; i < nodes.Length; i++)
            {
                nodes[i] = new HuffmanNode(-1, -1);
            }

            for (int i = 0; i < branches.Length; i++)
            {
                var br = branches[i];
                var nod = nodes[br.Item1];

                if (br.Item2) { nod.node1 = nodes[i + 1]; }
                else { nod.node0 = nodes[i + 1]; }
            }

            for (int i = 0; i < leaves.Length; i++)
            {
                var leaf = leaves[i];
                var nodeL = nodes[leaf.Item2];

                HuffmanNode nodeOut = new HuffmanNode(0, leaf.Item1);
                if (leaf.Item3) { nodeL.node1 = nodeOut; }
                else { nodeL.node0 = nodeOut; }
            }
        }

        public int GetLeafCount()
        {
            int leaves = 0;

            leaves += node0.IsLeaf ? 1 : node0.GetLeafCount();
            leaves += node1.IsLeaf ? 1 : node1.GetLeafCount();

            return leaves;
        }

        public int GetBranchIndices(List<(bool, int)> indices, int index = -1, bool is1 = false, int offset = 0, int total = 0)
        {
            if (IsLeaf) { return index; }

            indices.Add((is1, index));

            index++;
            total++;

            if (index == 0)
            {
                total = node0.GetBranchIndices(indices, 0, false);
                total = node1.GetBranchIndices(indices, 0, true, total + 1, total);
                return total;
            }

            int off = offset > 1 ? 1 : 0;

            total = node0.GetBranchIndices(indices, index + offset, false, off, total);
            total = node1.GetBranchIndices(indices, index + offset, true, off, total);
            return total;
        }

        public int GetLeaves(List<HuffmanNode> nodes)
        {
            if (!IsLeaf)
            {
                node0.GetLeaves(nodes);
                node1.GetLeaves(nodes);
                return 0;
            }

            nodes.Add(this);
            return 0;
        }

        public void GetBranches(List<HuffmanNode> nodes)
        {
            if (IsLeaf) { return; }

            nodes.Add(this);

            node0.GetBranches(nodes);
            node1.GetBranches(nodes);
        }

        private void Retrace(HuffmanNode node, List<bool> bits)
        {
            char valInit = (char)node.value;
            Stack<bool> tracked = Huffman.BIT_POOL.Get();
            tracked.Clear();

            HuffmanNode par = node.parent;
            tracked.Push(par.node1 == node);

            while (par != null)
            {
                node = par;
                par = node.parent;
                if (par == null) { break; }

                tracked.Push(par.node1 == node);
            }

            while(tracked.Count > 0)
            {
                bits.Add(tracked.Pop());
            }

            Huffman.BIT_POOL.Return(tracked);
        }

        public void Traverse(List<bool> bits, int val)
        {
            HuffmanNode node = null;

            if (!node0.IsLeaf)
            {
                node = node0.FindOfValue(val);
                if (node != null)
                {
                    Retrace(node, bits);
                    return;
                }
            }
            else
            {
                if (node0.value == val)
                {
                    Retrace(node0, bits);
                    return;
                }
            }

            if (!node1.IsLeaf)
            {
                node = node1.FindOfValue(val);
                if (node != null)
                {
                    Retrace(node, bits);
                    return;
                }
            }
            else
            {
                if (node1.value == val)
                {
                    Retrace(node1, bits);
                }
            }
        }

        private HuffmanNode FindOfValue(int value)
        {
            HuffmanNode node;
            if (node0.IsLeaf) { if (node0.value == value) { return node0; } }
            else {
                node = node0.FindOfValue(value);
                if (node != null) { return node; }
            }

            if (node1.IsLeaf) { if (node1.value == value) { return node1; } }
            else {
                node = node1.FindOfValue(value);
                if (node != null) { return node; }
            }
            return null;
        }

        public int PrintConnections(int depth = 0, int total = 0)
        {
            System.Diagnostics.Debug.Print($"Node #{total++}'s 0 node is {(node0 == null ? "null" : node0.ToString())} and 1 node is {(node1 == null ? "null" : node1.ToString())}");
            if (node0 != null && !node0.IsLeaf)
            {
                total = node0.PrintConnections(depth + 1, total);
            }

            if (node1 != null && !node1.IsLeaf)
            {
                total = node1.PrintConnections(depth + 1, total);
            }
            return total;
        }

        public void PrintTree(List<bool> bits = null, bool root = true, int depth = 0)
        {
            List<bool> bitsT = root ? new List<bool>() : new List<bool>(bits);
            if (!node0.IsLeaf)
            {
                bitsT.Add(false);
                node0.PrintTree(bitsT, false, depth + 1);
            }
            else
            {
                bitsT.Add(false);
                System.Diagnostics.Debug.Print($"L: ({(node0.value).ToString().PadRight(3, ' ')}) ==> {BitsToString(bitsT)}");
            }

            bitsT = root ? new List<bool>() : new List<bool>(bits);
            if (!node1.IsLeaf)
            {
                bitsT.Add(true);
                node1.PrintTree(bitsT, false, depth + 1);
            }
            else
            {
                bitsT.Add(true);
                System.Diagnostics.Debug.Print($"R: ({(node1.value).ToString().PadRight(3, ' ')}) ==> {BitsToString(bitsT)}");
            }
        }

        public static string BitsToString(List<bool> bits)
        {
            string str = "";
            foreach (var item in bits)
            {
                str += item ? 1 : 0;
            }
            return str;
        }

        public HuffmanNode GetNode(bool bit) => bit ? node1 : node0;

        public int CompareTo(HuffmanNode other) => frequency.CompareTo(other.frequency);

        public override string ToString() => $"Val: {value}, Freq: {frequency}";
    }
}