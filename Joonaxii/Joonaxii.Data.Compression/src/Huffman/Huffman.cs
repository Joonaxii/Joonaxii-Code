using Joonaxii.Collections;
using Joonaxii.Collections.PriorityQueue;
using Joonaxii.IO;
using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Joonaxii.Data.Compression.Huffman
{
    public static class Huffman
    {
        private const string HUFFMAN_STR = "HUFF";
        private const int INT_MIN_NODES_FOR_CHUNKS = 16;
        private const int MAX_CHUNKS = 4096;

        public static byte[] Compress(IEnumerable<byte> values, bool copy, out byte padded)
        {
            List<long> longs = new List<long>();
            if (copy)
            {
                padded = 0;
                IOExtensions.CopyToLongList(longs, values);
            }
            else
            {
                padded = IOExtensions.ConvertToLongList(longs, values);
            }
            return Compress(longs);
        }
        public static byte[] Compress(IEnumerable<int> values, bool copy, out byte padded)
        {
            List<long> longs = new List<long>();
            if (copy)
            {
                padded = 0;
                IOExtensions.CopyToLongList(longs, values);
            }
            else
            {
                padded = IOExtensions.ConvertToLongList(longs, values);
            }
            return Compress(longs);
        }
        public static byte[] Compress(IEnumerable<long> values)
        {
            byte[] data = null;
            using (MemoryStream stream = new MemoryStream())
            using (BitWriter bw = new BitWriter(stream))
            {
                CompressToStream(bw, values);
                bw.Flush();
                data = stream.ToArray();
            }
            return data;
        }

        public static void CompressToStream(BinaryWriter bw, IEnumerable<byte> values, bool copy, out byte padded)
        {
            List<long> longs = new List<long>();
            if (copy)
            {
                padded = 0;
                IOExtensions.CopyToLongList(longs, values);
            }
            else
            {
                padded = IOExtensions.ConvertToLongList(longs, values);
            }
            CompressToStream(bw, longs);
        }
        public static void CompressToStream(BinaryWriter bw, IEnumerable<int> values, bool copy, out byte padded)
        {
            List<long> longs = new List<long>();
            if (copy)
            {
                padded = 0;
                IOExtensions.CopyToLongList(longs, values);
            }
            else
            {
                padded = IOExtensions.ConvertToLongList(longs, values);
            }
            CompressToStream(bw, longs);
        }
        public static void CompressToStream(BinaryWriter bw, IEnumerable<long> values)
        {
            Dictionary<long, uint> freqLut = new Dictionary<long, uint>();
            foreach (var val in values)
            {
                if (freqLut.ContainsKey(val))
                {
                    freqLut[val]++;
                    continue;
                }
                freqLut.Add(val, 1);
            }

            List<HuffmanNode> nodes = new List<HuffmanNode>(2048);
            foreach (var item in freqLut)
            {
                nodes.Add(new HuffmanLeaf(item.Key, item.Value));
            }
            freqLut.Clear();

            PriorityQueue<HuffmanNode> nodeQueue = new PriorityQueue<HuffmanNode>(true, nodes);
            nodes.Clear();
            while (nodeQueue.Count > 1)
            {
                (var left, var right) = (nodeQueue.Dequeue(), nodeQueue.Dequeue());
                uint newFreq = left.frequency + right.frequency;

                var nod = new HuffmanBranch(newFreq, left, right);

                left.parent = nod;
                right.parent = nod;
                nodeQueue.Enqueue(nod);
            }
            var root = nodeQueue.Dequeue();

            Dictionary<long, HuffmanNode> idToNode = new Dictionary<long, HuffmanNode>();
            root.GetAllLeaves(idToNode, out ulong largestVal, out int maxDepth);

            List<HuffmanInstance> nodesToSave = new List<HuffmanInstance>();
            foreach (var n in idToNode)
            {
                int depth = n.Value.GetDepth();
                nodesToSave.Add(new HuffmanInstance(n.Value, depth, IOExtensions.BitsNeeded(n.Value.Value), IOExtensions.BitsNeeded(depth)));
            }

            BitWriter bwI = bw as BitWriter;
            MemoryStream stream = null;
            bool isBitWriter = bwI != null;

            //Make sure we're byte aligned
            if (isBitWriter) { bwI.ByteAlign(); }
            else
            {
                stream = new MemoryStream();
                bwI = new BitWriter(stream);
            }

            byte valueBits = (IOExtensions.BitsNeeded(largestVal));
            byte traceBits = (IOExtensions.BitsNeeded(maxDepth));

            //HEADER
            //===========================================================================================Huffman Tree Tracing Header=============================================================================
            //=================================================================================================14 to 18 BYTES====================================================================================
            //               HEADER STRING              | CHUNK | VAL SIZ + BIT SIZ |                LEAF COUNT               |                                     BIT COUNT                                   |
            //[ 0100-1000 0101-0101 0100-0110 0100-0110 | 00-01 | 000-010-+-000-010 | 0000-0000 0000-0000 0000-0000 1111-1111 | 0000-0000 0000-0000 0000-0000 0000-0000 0000-0000 0000-0000 0000-0000 1111-1111 |
            //------H---------U---------F---------F-----|---1---|----2----+----2----|-------------------255-------------------|---------------------------------------255---------------------------------------|
            //===================================================================================================================================================================================================

            var bytes = Encoding.ASCII.GetBytes(HUFFMAN_STR);
            bwI.Write(bytes);

            long bitCount = 0;
    
            ulong estimated = IOExtensions.GetRequired7BitBytes(nodesToSave.Count - 1) * 8ul;
            for (int i = 0; i < nodesToSave.Count; i++)
            {
                var node = nodesToSave[i];
                estimated += (ulong)(valueBits + traceBits + node.depth);
            }

            byte mode = EvaluateMode(nodesToSave, estimated, out var chunks);

            bwI.Write(mode, 4);
            if(mode == 0)
            {
                bwI.Write(valueBits - 1, 6);
                bwI.Write(traceBits - 1, 6);

                bwI.Write7BitInt(nodesToSave.Count - 1);
            }
            else
            {
                bwI.Write((chunks.Count - 1), 12);
            }
            bwI.ByteAlign();

            //Write down the start position of 'bitCount' 
            long pos = bwI.BaseStream.Position;
            bwI.Write(bitCount);

            if (mode == 0)
            {
                foreach (var n in nodesToSave)
                {
                    var nd = n.node;

                    bwI.Write(nd.Value, valueBits);
                    bwI.Write(n.depth, traceBits);
                    HuffmanNode.TraceNode(nd, (bool bit) => { bwI.Write(bit); });
                }
            }
            else
            {
                foreach (var chunk in chunks)
                {
                    chunk.Write(bwI, nodesToSave);
                }
            }

            long bitsWritten = 0;
            Stack<bool> stack = new Stack<bool>(2048);
            foreach (var val in values)
            {
                HuffmanNode.TraceNode(idToNode[val], stack);
                while (stack.Count > 0) { bitsWritten++; bwI.Write(stack.Pop()); }
            }

            bwI.ByteAlign();
            long posEnd = bwI.BaseStream.Position;

            bwI.BaseStream.Seek(pos, SeekOrigin.Begin);
            bwI.Write(bitsWritten);
            bwI.BaseStream.Seek(posEnd, SeekOrigin.Begin);

            if (!isBitWriter)
            {
                bwI.Flush();
                bw.Write(stream.ToArray());

                bwI.Dispose();
                stream.Dispose();
            }
        }

        private static byte EvaluateMode(List<HuffmanInstance> nodes, ulong estimatedSize, out List<HuffmanChunk> chunks)
        {
            if(nodes.Count < INT_MIN_NODES_FOR_CHUNKS) { chunks = null; return 0; }

            List<HuffmanInstance> nodesVal = new List<HuffmanInstance>(nodes);
            nodesVal.Sort(CompareByValueBits);

            List<HuffmanInstance> nodesDepth = new List<HuffmanInstance>(nodes);
            nodesDepth.Sort(CompareByDepthBits);

            List<HuffmanChunk> chunksVal = new List<HuffmanChunk>();
            List<HuffmanChunk> chunksDepth = new List<HuffmanChunk>();

            ChunkByValue(nodesVal, chunksVal, out ulong bitCountVal);
            ChunkByDepth(nodesDepth, chunksDepth, out ulong bitCountDepth);

            int min = Math.Min(chunksDepth.Count, chunksVal.Count);
            if(min > MAX_CHUNKS || estimatedSize <= Math.Min(bitCountDepth, bitCountVal)) 
            {
                nodesDepth.Clear();
                nodesVal.Clear();

                chunksVal.Clear();
                chunksDepth.Clear();

                chunks = null; 
                return 0; 
            }

            System.Diagnostics.Debug.Print($"Estimated Size: {estimatedSize}, Depth Chunks: {bitCountDepth} bits, Value Chunks: {bitCountVal} bits");

            bool preferDepth = bitCountDepth < bitCountVal;
            if (preferDepth)
            {
                if (chunksDepth.Count > MAX_CHUNKS)
                {
                    nodesDepth.Clear();
                    nodesVal.Clear();

                    chunksVal.Clear();
                    chunksDepth.Clear();

                    chunks = null;
                    return 0;
                }

                nodes.Sort(CompareByDepthBits);
                chunks = chunksDepth;

                nodesDepth.Clear();
                nodesVal.Clear();

                chunksVal.Clear();
                return 2;
            }

            if(chunksVal.Count > MAX_CHUNKS)
            {
                nodesDepth.Clear();
                nodesVal.Clear();

                chunksVal.Clear();
                chunksDepth.Clear();

                chunks = null;
                return 0;
            }

            nodes.Sort(CompareByValueBits);
            chunks = chunksVal;

            nodesDepth.Clear();
            nodesVal.Clear();

            chunksDepth.Clear();
            return 1;
        }

        private static void ChunkByValue(List<HuffmanInstance> instances, List<HuffmanChunk> chunks, out ulong bitsOut)
        {
            bitsOut = 0;

            HuffmanInstance current = instances[0];
            int start = 0;
            byte valueBits = current.bitsValue;
            byte maxDepth = current.bitsDepth;
            HuffmanChunk chnk;

            for (int i = 1; i < instances.Count; i++)
            {
                current = instances[i];
                if(current.bitsValue != valueBits)
                {
                    chnk = new HuffmanChunk(start, i - start, valueBits, maxDepth);
                    chunks.Add(chnk);
                    bitsOut += chnk.GetBitsRequired(instances);

                    start = i;
                    valueBits = current.bitsValue;
                    maxDepth = current.bitsDepth;
                    continue;
                }
                maxDepth = maxDepth < current.bitsDepth ? current.bitsDepth : maxDepth;
            }
            chunks.Add(chnk = new HuffmanChunk(start, instances.Count - start, valueBits, maxDepth));
            bitsOut += chnk.GetBitsRequired(instances);
        }

        private static void ChunkByDepth(List<HuffmanInstance> instances, List<HuffmanChunk> chunks, out ulong bitsOut)
        {
            bitsOut = 0;

            HuffmanInstance current = instances[0];
            int start = 0;
            byte maxValue = current.bitsValue;
            byte depthBits = current.bitsDepth;
            HuffmanChunk chnk;

            for (int i = 1; i < instances.Count; i++)
            {
                current = instances[i];
                if (current.bitsDepth != depthBits)
                {
                    chnk = new HuffmanChunk(start, i - start, maxValue, depthBits);
                    chunks.Add(chnk);
                    bitsOut += chnk.GetBitsRequired(instances);

                    start = i;
                    depthBits = current.bitsDepth;
                    maxValue = current.bitsValue;
                    continue;
                }
                maxValue = maxValue < current.bitsValue ? current.bitsValue : maxValue;
            }
            chunks.Add(chnk = new HuffmanChunk(start, instances.Count - start, maxValue, depthBits));
            bitsOut += chnk.GetBitsRequired(instances);
        }

        public static bool DecompressFromStream(BinaryReader br, List<int> values)
        {
            List<long> longs = new List<long>();
            DecompressFromStream(br, longs);
            IOExtensions.CopyToIntList(values, longs);
            return true;
        }

        public static bool DecompressFromStream(BinaryReader br, List<long> values)
        {
            BitReader brI = br as BitReader;
            bool isBitReader = brI != null;
            MemoryStream stream = null;

            //Make sure we're byte aligned
            if (isBitReader) { brI.ByteAlign(); }
            else
            {
                stream = new MemoryStream();
                br.BaseStream.CopyToWithPos(stream);
                brI = new BitReader(stream);
            }
            if (Encoding.ASCII.GetString(brI.ReadBytes(HUFFMAN_STR.Length)) != HUFFMAN_STR) { return false; }

            List<HuffmanTrace> traces = null;
            byte mode = brI.ReadByte(4);
            byte valBits = 0;
            byte traceBits = 0;
            ushort chunks = 0;
            int leafCount = 0;

            if (mode == 0)
            {
                valBits = (byte)(brI.ReadByte(6) + 1);
                traceBits = (byte)(brI.ReadByte(6) + 1);
                leafCount = brI.Read7BitInt() + 1;

                traces = new List<HuffmanTrace>(new HuffmanTrace[leafCount]);
            }
            else
            {
                chunks = (ushort)(brI.ReadUInt16(12) + 1);
                traces = new List<HuffmanTrace>();
            }

            long bitCount = brI.ReadInt64();

            Stack<bool> bitVals = new Stack<bool>();
            if (mode == 0)
            {
                for (int i = 0; i < leafCount; i++)
                {
                    long value = brI.ReadInt64(valBits);
                    int bits = brI.ReadInt32(traceBits);
                    for (int j = 0; j < bits; j++)
                    {
                        bitVals.Push(brI.ReadBoolean());
                    }
                    traces[i] = new HuffmanTrace(value, bitVals);
                    bitVals.Clear();
                }
            }
            else
            {
                for (int i = 0; i < chunks; i++)
                {
                    HuffmanChunk.Read(brI, traces);
                }
            }
     
            HuffmanNode root = new HuffmanBranch(traces);

            HuffmanNode nodeTmp = root;
            for (int i = 0; i < bitCount; i++)
            {
                bool bit = brI.ReadBoolean();
                nodeTmp = nodeTmp.SelectNode(bit);

                if (nodeTmp.IsLeaf)
                {
                    values.Add(nodeTmp.Value);
                    nodeTmp = root;
                }
            }
            brI.ByteAlign();

            if (!isBitReader)
            {
                br.BaseStream.Seek(brI.BaseStream.Position, SeekOrigin.Begin);

                brI.Dispose();
                stream.Dispose();
            }
            return true;
        }

        private static int CompareByDepthBits(HuffmanInstance a, HuffmanInstance b) => a.bitsDepth.CompareTo(b.bitsDepth);
        private static int CompareByValueBits(HuffmanInstance a, HuffmanInstance b) => a.bitsValue.CompareTo(b.bitsValue);

        private struct HuffmanChunk
        {
            private int _start;
            private int _len;

            private byte _bitsValue;
            private byte _bitsDepth;

            public HuffmanChunk( int start, int count, byte bitsValue, byte bitsDepth)
            {
                _start = start;
                _len = count;

                _bitsValue = bitsValue;
                _bitsDepth = bitsDepth;
            }

            public ulong GetBitsRequired(List<HuffmanInstance> leaves)
            {
                ulong baseBits = 12ul + IOExtensions.GetRequired7BitBytes((_len - 1)) * 8ul;
                for (int i = 0; i < _len; i++)
                {
                    var inst = leaves[i];
                    baseBits += (ulong)(_bitsDepth + _bitsValue + inst.depth);
                }
                return baseBits;
            }

            public void Write(BitWriter bw, List<HuffmanInstance> leaves)
            {
                bw.Write(_bitsValue - 1, 6);
                bw.Write(_bitsDepth - 1, 6);

                bw.Write7BitInt((_len - 1));

                for (int i = _start; i < _start + _len; i++)
                {
                    var inst = leaves[i];
                    bw.Write(inst.node.Value, _bitsValue);
                    bw.Write(inst.depth, _bitsDepth);

                    HuffmanNode.TraceNode(inst.node, (bool bit) => 
                    { 
                        bw.Write(bit); 
                    });
                }
            }

            public static void Read(BitReader br, List<HuffmanTrace> leaves)
            {
                byte bitsValue = (byte)(br.ReadByte(6) + 1);
                byte bitsDepth = (byte)(br.ReadByte(6) + 1);

                int len = br.Read7BitInt() + 1;

                Stack<bool> bitVals = new Stack<bool>();
                for (int i = 0; i < len; i++)
                {
                    long value = br.ReadInt64(bitsValue);
                    int bits = br.ReadInt32(bitsDepth);
                    for (int j = 0; j < bits; j++)
                    {
                        bitVals.Push(br.ReadBoolean());
                    }
                    leaves.Add(new HuffmanTrace(value, bitVals));
                    bitVals.Clear();
                }
            }
        }

        private class HuffmanInstance
        {
            public HuffmanNode node;
            public int depth;

            public byte bitsValue;
            public byte bitsDepth;

            public HuffmanInstance(HuffmanNode node, int depth, byte bitsValue, byte bitsDepth)
            {
                this.node = node;
                this.depth = depth;
                this.bitsValue = bitsValue;
                this.bitsDepth = bitsDepth;
            }
        }
    
    }
}
