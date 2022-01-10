using Joonaxii.IO;
using System.Collections.Generic;
using System.IO;

namespace Joonaxii.Data.Compression.RLE
{
    public static class RLE
    {
        public static void CompressValues(IEnumerable<int> values, List<RLEChunk> chunks)
        {
            int c = -1;
            int val = 0;
            foreach (var item in values)
            {
                if(c < 0)
                {
                    c++;
                    val = item;
                    continue;
                }

                if(val != item)
                {
                    chunks.Add(new RLEChunk((byte)c, new RLEValue(val)));

                    c = 0;
                    val = item;
                    continue;
                }

                c++;
                if (c >= 15)
                {
                    chunks.Add(new RLEChunk((byte)c, new RLEValue(val)));
                    c = -1;
                    continue;
                }
            }

            if(c > -1)
            {
                chunks.Add(new RLEChunk((byte)c, new RLEValue(val)));
            }
        }

        public static void CompressToLUT(IEnumerable<int> values, Dictionary<RLEChunk, int> lut, List<RLEChunk> lutOrder, List<int> indicesOut, out byte lengthBits, out byte valueBits)
        {
            List<RLEChunk> chunks = new List<RLEChunk>();
            CompressValues(values, chunks);

            lengthBits = 0;
            valueBits = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                var chnk = chunks[i];
                if (lut.TryGetValue(chnk, out int val)) 
                {
                    indicesOut.Add(val);
                    continue; 
                }
                lut.Add(chnk, val = lutOrder.Count);
                indicesOut.Add(val);
                lutOrder.Add(chnk);

                var bt = IOExtensions.BitsNeeded(chnk.value.ToUInt32);
                valueBits = bt > valueBits ? bt : valueBits;

                bt = IOExtensions.BitsNeeded(chnk.count);
                lengthBits = bt > lengthBits ? bt : lengthBits;
            }
        }

        public static void CompressToStreamRaw(BinaryWriter bw, IEnumerable<int> values)
        {
            List<RLEChunk> chunks = new List<RLEChunk>();
            CompressValues(values, chunks);

            bw.Write(chunks.Count);
            foreach (var item in chunks)
            {
                item.Write(bw);
            }
        }

        public static void CompressToStream(BinaryWriter bw, IEnumerable<byte> values)
        {
            List<int> data = new List<int>();
            foreach (var item in values)
            {
                data.Add(item);
            }
            CompressToStream(bw, data);
        }
        public static void CompressToStream(BinaryWriter bw, IEnumerable<int> values)
        {
            Dictionary<RLEChunk, int> lut = new Dictionary<RLEChunk, int>();
            List<RLEChunk> lutOrder = new List<RLEChunk>();

            List<int> indices = new List<int>();
            CompressToLUT(values, lut, lutOrder, indices, out byte lengthBits, out byte valueBits);

            BitWriter bwI = bw as BitWriter;
            bool isBitWriter = bwI != null;
            MemoryStream stream = null;

            if (!isBitWriter)
            {
                bwI = new BitWriter(stream = new MemoryStream());
            }

            bwI.Write((lengthBits - 1), 3);
            bwI.Write((valueBits - 1), 5);

            byte bitsPerIndex = IOExtensions.BitsNeeded(lutOrder.Count);

            bwI.Write7BitInt(lutOrder.Count);
            foreach (var item in lutOrder)
            {
                item.Write(bwI, lengthBits, valueBits);
            }

            bwI.Write7BitInt(indices.Count);
            for (int i = 0; i < indices.Count; i++)
            {
                bwI.Write(indices[i], bitsPerIndex);
            }

            if (!isBitWriter)
            {
                bwI.Flush();
                bw.Write(stream.ToArray());

                stream.Dispose();
                bwI.Dispose();
            }
        }

        public static void DecompressFromLUT(IList<RLEChunk> lut, IEnumerable<int> indices, List<int> values)
        {
            foreach (var item in indices)
            {
                var chnk = lut[item];
                for (int i = 0; i < chnk.count + 1; i++)
                {
                    values.Add(chnk.value.ToInt32);
                }
            }
        }

        public static void DecompressFromChunks(IEnumerable<RLEChunk> chunks, List<int> values)
        {
            foreach (var item in chunks)
            {
                for (int i = 0; i < item.count+1; i++)
                {
                    values.Add(item.value.ToInt32);
                }
            }
        }

        public static void DecompressFromStream(BinaryReader br, List<int> values)
        {
            BitReader brI = br as BitReader;
            bool isBitReader = brI != null;
            MemoryStream stream = null;

            if (!isBitReader)
            {
                stream = new MemoryStream();
                br.BaseStream.CopyToWithPos(stream);
                brI = new BitReader(stream);
            }

            byte lenBits = (byte)(brI.ReadByte(3) + 1);
            byte valBits = (byte)(brI.ReadByte(5) + 1);

            int lutSize = brI.Read7BitInt();
            byte bitsPerIndex = IOExtensions.BitsNeeded(lutSize);

            RLEChunk[] lut = new RLEChunk[lutSize];
            for (int i = 0; i < lut.Length; i++)
            {
                lut[i].Read(brI, lenBits, valBits);
            }

            int indices = brI.Read7BitInt();
            for (int i = 0; i < indices; i++)
            {
                var chnk = lut[brI.ReadInt32(bitsPerIndex)];
                for (int j = 0; j < (chnk.count + 1); j++)
                {
                    values.Add(chnk.value.ToInt32);
                }
            }

            if (!isBitReader)
            {
                br.BaseStream.Seek(stream.Position, SeekOrigin.Begin);

                stream.Dispose();
                brI.Dispose();
            }
        }
    }
}
