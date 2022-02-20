using Joonaxii.MathJX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Joonaxii.IO
{
    public static class IOExtensions
    {
        public static void CopyToLongList(List<long> longs, IEnumerable<byte> values)
        {
            foreach (var item in values)
            {
                longs.Add(item);
            }
        }

        public static void CopyToLongList(List<long> longs, IEnumerable<int> values)
        {
            foreach (var item in values)
            {
                longs.Add(item);
            }
        }

        public static void CopyToIntList(List<int> ints, IEnumerable<long> values)
        {
            foreach (var item in values)
            {
                ints.Add((int)item);
            }
        }

        public static byte ConvertToLongList(List<long> longs, IEnumerable<byte> values)
        {
            byte padding = 0;
            List<byte> bytes = new List<byte>(values);
            int ii = 0;
            while (ii < bytes.Count)
            {
                long val = 0;
                for (int i = 0; i < 8; i++)
                {
                    val += ((long)bytes[ii++] << i * 8);
                    if (ii >= bytes.Count)
                    {
                        padding = (byte)(8 - i);
                        break;
                    }
                }
                longs.Add(val);
            }
            return padding;
        }

        public static byte ConvertToLongList(List<long> longs, IEnumerable<int> values)
        {
            byte padding = 0;
            List<int> ints = new List<int>(values);
            int ii = 0;
            while (ii < ints.Count)
            {
                long val = 0;
                for (int i = 0; i < 2; i++)
                {
                    val += ((long)ints[ii++] << i * 32);
                    if (ii >= ints.Count)
                    {
                        padding = (byte)(2 - i);
                        break;
                    }
                }
                longs.Add(val);
            }
            return padding;
        }

        public static int GetCharSize(this string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] > byte.MaxValue) { return 2; }
            }
            return 1;
        }

        public static int NextDivBy(int value, int power)
        {
            while (value % power != 0)
            {
                value++;
            }
            return value;
        }


        public static long NextDivBy(long value, int power)
        {
            while (value % power != 0)
            {
                value++;
            }
            return value;
        }

        public static void WriteToByteArray(byte[] buf, int start, long value, int bytes, bool bigEndian)
        {
            if (bigEndian)
            {
                for (int i = 0; i < bytes; i++)
                {
                    int bI = start + i;
                    buf[bytes - 1 - bI] = (byte)((value >> (i << 3)) & 0xFF);
                }
                return;
            }
            for (int i = 0; i < bytes; i++)
            {
                int bI = start + i;
                buf[bI] = (byte)((value >> (i << 3)) & 0xFF);
            }
        }

        public static void CopyToWithPos(this Stream stream, Stream other)
        {
            long pos = stream.Position;
            stream.Position = 0;

            stream.CopyTo(other);
            other.Position = pos;
            stream.Position = pos;
        }

        public static byte[] GetData(this Stream stream)
        {
            if (stream is MemoryStream ms)
            {
                return ms.ToArray();
            }

            using (ms = new MemoryStream())
            {
                long pos = stream.Position;
                stream.Position = 0;
                stream.CopyTo(ms);
                stream.Position = pos;
                return ms.ToArray();
            }
        }

        public static uint ReverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        public static short ReadInt16BigEndian(this BinaryReader br) => (short)br.ReadIntBigEndian(2);
        public static ushort ReadUInt16BigEndian(this BinaryReader br) => (ushort)br.ReadUIntBigEndian(2);
        public static char ReadCharBigEndian(this BinaryReader br) => (char)br.ReadUIntBigEndian(2);

        public static int ReadInt32BigEndian(this BinaryReader br) => (int)br.ReadIntBigEndian(4);
        public static uint ReadUInt32BigEndian(this BinaryReader br) => (uint)br.ReadUIntBigEndian(4);

        public static long ReadInt64BigEndian(this BinaryReader br) => br.ReadIntBigEndian(8);
        public static ulong ReadUInt64BigEndian(this BinaryReader br) => br.ReadUIntBigEndian(8);

        public static long ReadIntBigEndian(this BinaryReader br, int count)
        {
            count = count > 8 ? 8 : count < 1 ? 1 : count;
            long val = 0;
            byte[] bytes = br.ReadBytes(count);
            count = bytes.Length < count ? bytes.Length : count;
            for (int i = 0; i < count; i++)
            {
                val += (bytes[i] << ((count - 1 - i) << 3));
            }
            return val;
        }

        public static ulong ReadUIntBigEndian(this BinaryReader br, int count)
        {
            count = count > 8 ? 8 : count < 1 ? 1 : count;
            ulong val = 0;
            byte[] bytes = br.ReadBytes(count);
            for (int i = 0; i < count; i++)
            {
                val += (ulong)(bytes[i] << ((count - 1 - i) << 3));
            }
            return val;
        }

        public static void WriteBigEndian(this BinaryWriter bw, short value) => bw.WriteBigEndian(value, 2);
        public static void WriteBigEndian(this BinaryWriter bw, ushort value) => bw.WriteBigEndian(value, 2);
        public static void WriteBigEndian(this BinaryWriter bw, char value) => bw.WriteBigEndian(value, 2);

        public static void WriteBigEndian(this BinaryWriter bw, int value) => bw.WriteBigEndian(value, 4);
        public static void WriteBigEndian(this BinaryWriter bw, uint value) => bw.WriteBigEndian(value, 4);

        public static void WriteBigEndian(this BinaryWriter bw, long value) => bw.WriteBigEndian(value, 8);
        public static void WriteBigEndian(this BinaryWriter bw, ulong value) => bw.WriteBigEndian(value, 8);

        public static void WriteBigEndian(this BinaryWriter bw, long value, int count) => WriteBigEndian(bw, (ulong)value, count);

        public static void WriteBigEndian(this BinaryWriter bw, ulong value, int count)
        {
            count = count > 8 ? 8 : count < 1 ? 1 : count;
            for (int i = 0; i < count; i++)
            {
                bw.Write((byte)((value >> ((count - 1 - i) << 3)) & 0xFF));
            }
        }

        public static byte GetRequired7BitBytes(int value)
        {
            byte b = 0;
            uint v = (uint)value;
            while (v >= 0x80)
            {
                b++;
                v >>= 7;
            }
            b++;
            return b;
        }

        public static void Encode7BitInt(this BinaryWriter br, int value)
        {
            uint v = (uint)value;
            while (v >= 0x80)
            {
                br.Write((byte)(v | 0x80));
                v >>= 7;
            }
            br.Write((byte)v);
        }

        public static int Decode7BitInt(this BinaryReader br)
        {
            int count = 0;
            int shift = 0;

            byte b = br.ReadByte();
            count |= (b & 0x7F) << shift;
            shift += 7;

            while ((b & 0x80) != 0)
            {
                if (shift >= 5 * 7) { break; }

                b = br.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            }
            return count;
        }

        public static byte BitsNeeded(sbyte value) { unsafe { return BitsNeeded(*(byte*)&value); }; }
        public static byte BitsNeeded(byte value) => value < 1 ? (byte)1 : (byte)((Math.Log(value) / Math.Log(2)) + 1.0);

        public static byte BitsNeeded(short value) { unsafe { return BitsNeeded(*(ushort*)&value); }; }
        public static byte BitsNeeded(ushort value) => value < 1 ? (byte)1 : (byte)((Math.Log(value) / Math.Log(2)) + 1.0);

        public static byte BitsNeeded(int value) { unsafe { return BitsNeeded(*(uint*)&value); }; }
        public static byte BitsNeeded(uint value) => value < 1 ? (byte)1 : (byte)((Math.Log(value) / Math.Log(2)) + 1.0);

        public static byte BitsNeeded(long value) { unsafe { return BitsNeeded(*(ulong*)&value); }; }
        public static byte BitsNeeded(ulong value) => value < 1 ? (byte)1 : (byte)((Math.Log(value) / Math.Log(2)) + 1.0);

        public static string ToHexString(this string str, string separator = "")
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char chr = str[i];
                sb.Append($"{Convert.ToString(chr, 16)}{separator}");
            }
            return sb.ToString();
        }

        public static int ToInt(this byte[] data, bool bigEndian = false) => ToInt(data, 0, bigEndian);
        public static int ToInt(this byte[] data, int start, bool bigEndian = false)
        {
            int val = 0;
            if (bigEndian)
            {
                for (int i = 3; i >= 0; i--)
                {
                    val += (ushort)((data[i + start] << ((3 - i) * 8)));
                }
                return val;
            }
            for (int i = 0; i < 4; i++)
            {
                val += (data[i + start] << (i * 8));
            }
            return val;
        }

        public static uint ToUInt(this byte[] data, bool bigEndian = false) => ToUInt(data, 0, bigEndian);
        public static uint ToUInt(this byte[] data, int start, bool bigEndian = false)
        {
            uint val = 0;
            if (bigEndian)
            {
                for (int i = 3; i >= 0; i--)
                {
                    val += (uint)((data[i + start] << ((3 - i) * 8)));
                }
                return val;
            }
            for (int i = 0; i < 4; i++)
            {
                val += (uint)(data[i + start] << (i * 8));
            }
            return val;
        }

        public static short ToShort(this byte[] data, bool bigEndian = false) => ToShort(data, 0, bigEndian);
        public static short ToShort(this byte[] data, int start, bool bigEndian = false)
        {
            short val = 0;

            if (bigEndian)
            {
                for (int i = 1; i >= 0; i--)
                {
                    val += (short)((data[i + start] << ((1 - i) * 8)));
                }
                return val;
            }

            for (int i = 0; i < 2; i++)
            {
                val += (short)((data[i + start] << (i * 8)));
            }
            return val;
        }

        public static void ReadBits(this BinaryReader br, long bitCount, List<bool> bits)
        {
            byte bI = 0;
            byte val = br.ReadByte();
            for (long i = 0; i < bitCount; i++)
            {
                if(bI >= 8)
                {
                    val = br.ReadByte();
                    bI = 0;
                }
                bits.Add(val.IsBitSet(bI++));
            }
        }

        public static void WriteBits(this BinaryWriter bw, Stack<bool> bits)
        {
            byte val = 0;
            byte bI = 0;

            while(bits.Count > 0)
            {
                val = val.SetBit(bI, bits.Pop());

                bI++;
                if(bI >= 8)
                {
                    bI = 0;
                    bw.Write(val);
                    val = 0;
                }
            }

            if (bI > 0)
            {
                bw.Write(val);
            }
        }

        public static ushort ToUShort(this byte[] data, bool bigEndian = false) => ToUShort(data, 0, bigEndian);
        public static ushort ToUShort(this byte[] data, int start, bool bigEndian = false)
        {
            ushort val = 0;

            if (bigEndian)
            {
                for (int i = 1; i >= 0; i--)
                {
                    val += (ushort)((data[i + start] << ((1 - i) * 8)));
                }
                return val;
            }

            for (int i = 0; i < 2; i++)
            {
                val += (ushort)((data[i + start] << (i * 8)));
            }
            return val;
        }


    }
}
