using Joonaxii.Collections;
using Joonaxii.Debugging;
using Joonaxii.IO;
using Joonaxii.Pooling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Joonaxii.Text.Compression
{
    public static class Huffman
    {
        public static HuffmanComparer HUFFMAN_COMPARER { get; private set; } = new HuffmanComparer(false);
        public static HuffmanComparer HUFFMAN_INDEX_COMPARER { get; private set; } = new HuffmanComparer(true);
        public static GenericObjectPool<Stack<bool>> BIT_POOL = new GenericObjectPool<Stack<bool>>(32, () => { return new Stack<bool>(32); });

        public const string HEADER_STR = "HUFF";

        public static void CompressToStream(List<int> codes, byte size, BinaryWriter bw, TimeStamper stamper = null, FileDebugger debugger = null)
        {
            byte buffer = 0;
            List<bool> bits = new List<bool>();

            Compress(codes, bits, out int branches, out int leaves, out var root, stamper);

            byte connSize = CompressionHelpers.ToSize(branches);
            long diff = 0;

            List<HuffmanNode> leafList = new List<HuffmanNode>();
            List<HuffmanNode> branchList = new List<HuffmanNode>();
            root.GetLeaves(leafList);
            root.GetBranches(branchList);

            {
                HuffmanNodeTrace[] leafData = new HuffmanNodeTrace[leaves];
                int maxSteps = 0;
                for (int i = 0; i < leaves; i++)
                {
                    leafData[i] = new HuffmanNodeTrace(leafList[i], out int steps);
                    maxSteps = maxSteps < steps ? steps : maxSteps;
                }

                byte traceSize = (byte)IOExtensions.BitsNeeded(maxSteps);

                //HEADER
                //==================================================================Huffman Tree Tracing Header==========================================================
                //============================================================================14 BYTES===================================================================
                //               HEADER STRING              |                 BIT COUNT               |  VAL SIZE |  BIT SIZE |                LEAF COUNT               |
                //[ 0100-1000 0101-0101 0100-0110 0100-0110 | 0000-0000 0000-0000 0000-0000 1111-1111 | 0000-0010 | 0000-0010 | 0000-0000 0000-0000 0000-0000 1111-1111 |
                //------H---------U---------F---------F-----|-------------------255-------------------|-----2-----|-----2-----|-------------------255-------------------|
                //=======================================================================================================================================================

                debugger?.Start("Huffman Header");
                bw.Write(Encoding.ASCII.GetBytes(HEADER_STR)); //Header String 'HUFF'
                bw.Write(bits.Count);                          //Bit count

                bw.Write(size);                                //Leaf node value byte size
                bw.Write(traceSize);                           //Trace bit size

                bw.Write(leafData.Length);                     //Leaf traces
                System.Diagnostics.Debug.Print($"Huffman Header (COMPRESS): {bits.Count}, {size}, {traceSize}, {leafData.Length}");
                debugger?.Stamp();
          
                using (MemoryStream streamIn = new MemoryStream())
                using (BitWriter btW = new BitWriter(streamIn))
                {
                    for (int i = 0; i < leafData.Length; i++)
                    {
                        leafData[i].WriteTo(btW, size, traceSize);
                    }

                    btW.Flush();

                    debugger?.Start("Huffman Binary Tree");
                    bw.Write(streamIn.ToArray());
                    debugger?.Stamp();
                }
            }

            stamper?.Start($"Huffman Encoding: Writing '{bits.Count}' bits or '{IOExtensions.NextPowerOf(bits.Count, 8) / 8}' bytes tree is '{diff}' bytes");
            debugger?.Start("Huffman Binary");
            int byteIndex = 0;

            for (int i = 0; i < bits.Count; i++)
            {
                bool bit = bits[i];
                buffer = buffer.SetBit(byteIndex, bit);

                byteIndex++;
                if (byteIndex >= 8)
                {
                    bw.Write(buffer);
                    byteIndex = 0;
                    buffer = 0;
                }
            }
            if (byteIndex > 0) { bw.Write(buffer); }

            stamper?.Stamp();
            debugger?.Stamp();
        }

        public static void DecompressFromStream(List<int> codes, BinaryReader br, TimeStamper stamper = null)
        {
            string temp = Encoding.ASCII.GetString(br.ReadBytes(HEADER_STR.Length));
            if (temp != HEADER_STR)
            {
                br.BaseStream.Seek(-HEADER_STR.Length, SeekOrigin.Current);
                return;
            }

            HuffmanNode root = null;
            byte valSize = 0;
            int bitCount = 0;
            int leaves = 0;

            {

                //HEADER
                //==================================================================Huffman Tree Tracing Header==========================================================
                //============================================================================14 BYTES===================================================================
                //               HEADER STRING              |                 BIT COUNT               |  VAL SIZE |  BIT SIZE |                LEAF COUNT               |
                //[ 0100-1000 0101-0101 0100-0110 0100-0110 | 0000-0000 0000-0000 0000-0000 1111-1111 | 0000-0010 | 0000-0010 | 0000-0000 0000-0000 0000-0000 1111-1111 |
                //------H---------U---------F---------F-----|-------------------255-------------------|-----2-----|-----2-----|-------------------255-------------------|
                //=======================================================================================================================================================

                bitCount = br.ReadInt32();
                valSize = br.ReadByte();
                byte traceSize = br.ReadByte();
                leaves = br.ReadInt32();

                HuffmanNodeTrace[] leafData = new HuffmanNodeTrace[leaves];
                System.Diagnostics.Debug.Print($"Huffman Header: {bitCount}, {valSize}, {traceSize}, {leaves}");
                long posStart = br.BaseStream.Position;
        
                using (MemoryStream streamIn = new MemoryStream((br.BaseStream as MemoryStream).ToArray()))
                using (BitReader btR = new BitReader(streamIn))
                {
                    streamIn.Position = posStart;
                    for (int i = 0; i < leafData.Length; i++)
                    {
                        leafData[i].ReadFrom(btR, valSize, traceSize);
                    }
                    posStart = streamIn.Position;
                }
                
                br.BaseStream.Seek(posStart, SeekOrigin.Begin);
                root = new HuffmanNode(leafData);
            }

            Queue<bool> bits = new Queue<bool>(8192);
            int bytesToRead = IOExtensions.NextPowerOf(bitCount, 8) / 8;

            int bitsTotal = 0;
            for (int i = 0; i < bytesToRead; i++)
            {
                byte buffer = br.ReadByte();
                for (int j = 0; j < 8; j++)
                {
                    bits.Enqueue(buffer.IsBitSet(j));
                    bitsTotal++;
                    if (bitsTotal >= bitCount) { break; }
                }
                if (bitsTotal >= bitCount) { break; }
            }
            Huffman.Decompress(codes, bits, root);
        }

        public static void Compress(List<int> codes, List<bool> bits, out int branchCount, out int leafCount, out HuffmanNode root, TimeStamper stamper = null)
        {
            branchCount = 0;
            leafCount = 0;
            stamper?.Start($"Huffman Encoding: counting frequenices on '{codes.Count}' codes.");
            Dictionary<int, int> countLut = new Dictionary<int, int>();
            for (int i = 0; i < codes.Count; i++)
            {
                int c = codes[i];
                if (countLut.ContainsKey(c)) { countLut[c]++; continue; }
                countLut.Add(c, 1);
            }
            stamper?.Stamp();

            stamper?.Start($"Huffman Encoding: Creating Nodes");
            List<HuffmanNode> counts = new List<HuffmanNode>();
            foreach (var item in countLut)
            {
                var nod = new HuffmanNode(item.Value, item.Key);
                counts.Add(nod);
            }
            stamper?.Stamp();

            stamper?.Start($"Huffman Encoding: Connecting nodes and creating leaves");
            PriorityQueue<HuffmanNode> nodes = new PriorityQueue<HuffmanNode>(counts, HUFFMAN_COMPARER);
            List<HuffmanNode> nodesTemp = new List<HuffmanNode>(4096);
            while (nodes.Count > 1)
            {
                (var left, var right) = (nodes.Dequeue(), nodes.Dequeue());
                int newFreq = left.frequency + right.frequency;

                var nod = new HuffmanNode(newFreq, left, right);

                left.parent = nod;
                right.parent = nod;

                nodesTemp.Add(right);
                nodesTemp.Add(left);

                nodes.Enqueue(nod);
            }
            stamper?.Stamp();

            stamper?.Start($"Huffman Encoding: Traversing tree and writing bits");
            root = nodes.Dequeue();

            nodesTemp.Add(root);

            int index = 0;
            for (int i = nodesTemp.Count - 1; i >= 0; i--)
            {
                var nod = nodesTemp[i];
                if (nod.IsLeaf) { leafCount++; continue; }
                nod.index = index++;
                branchCount++;
            }

            for (int i = 0; i < codes.Count; i++)
            {
                root.Traverse(bits, codes[i]);
            }
            stamper?.Stamp();
        }

        public static void Decompress(List<int> codes, Queue<bool> bits, HuffmanNode root, TimeStamper stamper = null)
        {
            HuffmanNode nodeSt = root;
            while (bits.Count > 0)
            {
                bool bit = bits.Dequeue();

                nodeSt = nodeSt.GetNode(bit);
                if (nodeSt.IsLeaf)
                {
                    codes.Add(nodeSt.value);
                    nodeSt = root;
                    continue;
                }
            }
        }

        public class HuffmanComparer : IComparer<HuffmanNode>
        {
            private bool _compareIndices;

            public HuffmanComparer(bool indexComparer) => _compareIndices = indexComparer;
            public int Compare(HuffmanNode x, HuffmanNode y) => _compareIndices ? x.index.CompareTo(y.index) : y.CompareTo(x);
        }

        private struct HuffmanIndexPair : IComparable<HuffmanIndexPair>
        {
            public int index;
            public bool is1;
            public HuffmanNode node;

            public HuffmanIndexPair(int index, bool is1, HuffmanNode node)
            {
                this.index = index;
                this.is1 = is1;
                this.node = node;
            }

            public int CompareTo(HuffmanIndexPair other) => index.CompareTo(other.index);
        }

        public struct HuffmanNodeTrace
        {
            public int value;
            public bool[] bits;

            public HuffmanNodeTrace(HuffmanNode node, out int steps)
            {
                value = node.value;

                List<bool> bitsL = new List<bool>();
                HuffmanNode par = node.parent;

                bitsL.Add(node.IsNode1);
                while (par != null)
                {
                    node = par;
                    par = node.parent;

                    if (par == null) { break; }
                    bitsL.Add(node.IsNode1);
                }

                bits = bitsL.ToArray();
                steps = bits.Length;
            }

            public void WriteTo(BitWriter bw, byte size, byte bitSize)
            {
                bw.Write(value, size);

                bw.Write((byte)bits.Length, bitSize);
                for (int i = bits.Length - 1; i >= 0; i--)
                {
                    bw.Write(bits[i]);
                }
            }

            public HuffmanNodeTrace ReadFrom(BitReader br, byte size, byte bitSize)
            {
                value = br.ReadValue(size);

                bits = new bool[br.ReadValue(bitSize)];
                for (int i = 0; i < bits.Length; i++)
                {
                    bits[i] = br.ReadBoolean();
                }
                return this;
            }

            private string ToBinary(bool[] bits)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bits.Length; i++)
                {
                    sb.Append(bits[i] ? 1 : 0);
                }
                return sb.ToString();
            }
        }
    }
}
