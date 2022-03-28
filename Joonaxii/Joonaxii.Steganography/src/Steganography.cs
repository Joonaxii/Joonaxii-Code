using Joonaxii.IO;
using Joonaxii.IO.BitStream;
using Joonaxii.MathJX;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Joonaxii.Stegaography
{
    public static class Steganography
    {
        public const int LENGTH_BITS_LONG = 64;
        public const int LENGTH_BITS = 32;
        public const int BIT_COUNT = 8;

        public static byte[] GetData(byte[] ogData, byte leastSigBits, string fileName, out ulong requiredBits, out ulong requiredBytes)
        {
            byte[] inputBytes = ogData;

            requiredBits = (ulong)(inputBytes.Length * 8L);

            requiredBits += (LENGTH_BITS_LONG + LENGTH_BITS);
            requiredBits += (ulong)(fileName.Length * 16);

            requiredBytes = (requiredBits / leastSigBits) + BIT_COUNT;
            requiredBits += BIT_COUNT;

            byte[] bitsToSave = new byte[13 + (fileName.Length * 2) + inputBytes.Length];
            bitsToSave[0] = leastSigBits;

            Buffer.BlockCopy(BitConverter.GetBytes((ulong)inputBytes.LongLength), 0, bitsToSave, 1, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(fileName.Length), 0, bitsToSave, 9, 4);

            int chars = fileName.Length * 2;
            Buffer.BlockCopy(fileName.ToCharArray(), 0, bitsToSave, 13, chars);
            Buffer.BlockCopy(inputBytes, 0, bitsToSave, 13 + chars, inputBytes.Length);
            return bitsToSave;
        }

        public static void ReadFromWav(string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (BinaryReader br = new BinaryReader(stream))
            {
                if (!ReadWav(br, out int size, out ushort channels, out int freq, out int bytesPSec, out ushort blockAling, out ushort bitsPerSample, out int[] data))
                {
                    System.Diagnostics.Debug.Print($"Failed to read wav file!");
                    return;
                }

                byte[] dataByts = new byte[data.Length * (bitsPerSample / 8)];
                Buffer.BlockCopy(data, 0, dataByts, 0, dataByts.Length);

                Decrypt(dataByts, (byte)(bitsPerSample / 8), out string nameOut, out byte[] dataOut);
                string pathOut = $"{Path.GetDirectoryName(file)}/{Path.GetFileNameWithoutExtension(nameOut)}_DECRYPTED{Path.GetExtension(nameOut)}";
                File.WriteAllBytes(pathOut, dataOut);
                System.Diagnostics.Debug.Print($"Decrypted file written to '{pathOut}'");
            }
        }

        public static void WriteWav(string fileIn, byte leastSigBits, byte[] bitsToSave, ulong requiredBits, ulong requiredBytes)
        {
            using (FileStream stream = new FileStream(fileIn, FileMode.Open))
            using (BinaryReader br = new BinaryReader(stream))
            {
                ReadWav(br, out int size, out ushort channels, out int freq, out int bytesPSec, out ushort blockAling, out ushort bitsPerSample, out int[] data);
                System.Diagnostics.Debug.Print($"Sample count: {data.Length}");

                bool isEnough = (ulong)data.Length >= requiredBytes;
                System.Diagnostics.Debug.Print(isEnough ? $"The wav has enough samples! [{data.Length}, {requiredBytes}]" : $"The wav doesn't have enough samples! [{data.Length}, {requiredBytes}]");

                if (isEnough)
                {
                    string outPath = $"{Path.GetDirectoryName(fileIn)}/{Path.GetFileNameWithoutExtension(fileIn)}_MSG.wav";
                    using (FileStream fStream = new FileStream(outPath, FileMode.OpenOrCreate))
                    using (BinaryWriter bwT = new BinaryWriter(fStream))
                    {
                        bwT.Write(Encoding.ASCII.GetBytes("RIFF"));
                        bwT.Write(size);
                        bwT.Write(Encoding.ASCII.GetBytes("WAVEfmt "));

                        bwT.Write(16);
                        bwT.Write((ushort)1);
                        bwT.Write(channels);
                        bwT.Write(freq);
                        bwT.Write(bytesPSec);
                        bwT.Write(blockAling);
                        bwT.Write(bitsPerSample);

                        int ss = bitsPerSample / 8;
                        bwT.Write(Encoding.ASCII.GetBytes("data"));
                        bwT.Write(data.Length * ss);

                        long byteI = 0;
                        byte bitI = 0;

                        byte buffer = bitsToSave[byteI++];
                        bool isHdr = true;
                        bool done = false;

                        System.Diagnostics.Debug.Print($"Data [{ss}, {bitsPerSample}]: {data.Length}, {bitsToSave.LongLength}");
                        for (int i = 0; i < data.Length; i++)
                        {
                            int value = data[i];
                            if (!done)
                            {
                                int c = isHdr ? 1 : leastSigBits;
                                int v = value;
                                for (int j = 0; j < c; j++)
                                {
                                    value = value.SetBit(j, buffer.IsBitSet(bitI++));
                                    if (bitI >= 8)
                                    {
                                        if (byteI >= bitsToSave.LongLength) { done = true; break; }
                                        bitI = 0;
                                        buffer = bitsToSave[byteI++];
                                        isHdr = false;
                                    }
                                }
                            }

                            switch (ss)
                            {
                                case 1:
                                    bwT.Write((byte)value);
                                    break;
                                case 2:
                                    bwT.Write((short)value);
                                    break;
                                case 3:
                                    for (int j = 0; j < 3; j++)
                                    {
                                        bwT.Write((byte)(value >> j * 8));
                                    }
                                    break;
                                case 4:
                                    bwT.Write(value);
                                    break;
                            }
                        }

                    }
                }
            }
        }

        public static void WriteImage(string fileIn, byte leastSigBits, byte[] bitsToSave, ulong requiredBits, ulong requiredBytes)
        {
            using (Bitmap bm = new Bitmap(fileIn))
            {
                ulong amountOfPixels = (ulong)(bm.Width * bm.Height);
                ulong bytesForPixels = amountOfPixels * 32ul;

                bool isEnough = bytesForPixels >= requiredBytes;
                System.Diagnostics.Debug.Print(isEnough ? $"The image has enough pixel bytes! [{amountOfPixels}, {bytesForPixels}, {requiredBytes}]" : $"The Image doesn't have enough pixel bytes! [{amountOfPixels}, {bytesForPixels}, {requiredBytes}]");

                if (isEnough)
                {
                    FastColor[] pixels = new FastColor[amountOfPixels];
                    for (int y = 0; y < bm.Height; y++)
                    {
                        for (int x = 0; x < bm.Width; x++)
                        {
                            int i = y * bm.Width + x;
                            pixels[i] = bm.GetPixel(x, y);
                        }
                    }

                    string outPath = $"{Path.GetDirectoryName(fileIn)}/{Path.GetFileNameWithoutExtension(fileIn)}_MSG.png";
                    using (FileStream fStream = new FileStream(outPath, FileMode.OpenOrCreate))
                    using (Bitmap bmOut = new Bitmap(bm.Width, bm.Height, PixelFormat.Format32bppArgb))
                    {
                        WriteBytes(bitsToSave, leastSigBits, pixels, bmOut);
                        bmOut.Save(fStream, ImageFormat.Png);
                    }
                }
            }
        }

        public static bool ReadWav(BinaryReader br, out int size, out ushort channels, out int frequency, out int bytePerSec, out ushort blockAling, out ushort bitsPerSample, out int[] data)
        {
            size = 0;
            channels = 0;
            frequency = 0;
            bytePerSec = 0;
            blockAling = 0;
            bitsPerSample = 0;

            data = null;


            string str = Encoding.ASCII.GetString(br.ReadBytes(4));

            if (str != "RIFF") { return false; }
            size = br.ReadInt32();

            str = Encoding.ASCII.GetString(br.ReadBytes(8));
            if (str != "WAVEfmt ") { return false; }

            int sizeOfChunk = br.ReadInt32();
            ushort format = br.ReadUInt16();

            if (sizeOfChunk != 16 | format != 1) { return false; }

            channels = br.ReadUInt16();
            frequency = br.ReadInt32();
            bytePerSec = br.ReadInt32();
            blockAling = br.ReadUInt16();
            bitsPerSample = br.ReadUInt16();

            br.ReadBytes(4);
            int sizeOfBytes = br.ReadInt32();

            byte bitSize = (byte)(bitsPerSample / 8);

            data = new int[sizeOfBytes / bitSize];
            for (int i = 0; i < data.Length; i++)
            {
                int val = 0;
                for (int j = 0; j < bitSize; j++)
                {
                    byte bb;
                    val += ((bb = br.ReadByte()) << j * 8);
                }
                data[i] = val;
            }
            return true;

        }

        public static void ReadFromImage(string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (Bitmap bm = new Bitmap(stream))
            {
                ReadPixelData(bm, out string nameOut, out byte[] dataOut);
                string pathOut = $"{Path.GetDirectoryName(file)}/{Path.GetFileNameWithoutExtension(nameOut)}_DECRYPTED{Path.GetExtension(nameOut)}";
                File.WriteAllBytes(pathOut, dataOut);
                System.Diagnostics.Debug.Print($"Decrypted file written to '{pathOut}'");
            }
        }

        private static void WriteBytes(byte[] bitsToWrite, byte bits, FastColor[] pixels, Bitmap bm)
        {
            ulong bitsWritten = 0;
            bool keepWriting = true;
            long byteIndex = 0;
            byte buffer = bitsToWrite[byteIndex++];
            byte bitIndex = 0;

            bool hdr = false;
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % bm.Width;
                int y = i / bm.Width;
                ref var pix = ref pixels[i];

                if (keepWriting)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        byte v = pix[j];
                        int write = (!hdr ? 1 : bits);
                        if (!hdr && bitsWritten >= 8) { hdr = true; }
                        for (int b = 0; b < write; b++)
                        {
                            bool bv;
                            v = v.SetBit(b, bv = buffer.IsBitSet(bitIndex++));
                            if (bitIndex >= 8)
                            {
                                if (byteIndex >= bitsToWrite.LongLength)
                                {
                                    pix[j] = v;
                                    keepWriting = false;
                                    bitsWritten++;
                                    break;
                                }

                                buffer = bitsToWrite[byteIndex++];
                                bitIndex = 0;
                            }
                            bitsWritten++;
                        }

                        if (!keepWriting) { break; }
                        pix[j] = v;
                    }
                }
                bm.SetPixel(x, y, pix);
            }
        }

        public static void Decrypt(byte[] data, byte bitJump, out string name, out byte[] dataOut)
        {
            dataOut = null;
            name = "";
            using (MemoryStream stream = new MemoryStream(data))
            using (BitReader br = new BitReader(stream))
            {
                //byte leastSigBits = br.ReadByte(1, bitJump);

                //ulong bitLen = br.ReadUInt64(leastSigBits, bitJump);
                //int nameL = br.ReadInt32(leastSigBits, bitJump);

                //StringBuilder sb = new StringBuilder(nameL);
                //for (int i = 0; i < nameL; i++)
                //{
                //    sb.Append((char)br.ReadUInt16(leastSigBits, bitJump));
                //}
                //name = sb.ToString();

                //System.Diagnostics.Debug.Print($"Read header with '{leastSigBits}' least-sig-bit, byte count of '{bitLen}' & name of {name}");
                //dataOut = new byte[bitLen];

                //for (ulong i = 0; i < bitLen; i++)
                //{
                //     dataOut[i] = br.ReadByte(leastSigBits, bitJump);
                //}
            }
        }

        private static void ReadPixelData(Bitmap bm, out string name, out byte[] dataOut)
        {
            byte[] pixels = new byte[bm.Width * bm.Height * 4];

            int byteI = 0;
            for (int y = 0; y < bm.Height; y++)
            {
                for (int x = 0; x < bm.Width; x++)
                {
                    var bmP = bm.GetPixel(x, y);

                    pixels[byteI++] = bmP.R;
                    pixels[byteI++] = bmP.G;
                    pixels[byteI++] = bmP.B;
                    pixels[byteI++] = bmP.A;
                }
            }
            Decrypt(pixels, 1, out name, out dataOut);
        }


        [StructLayout(LayoutKind.Explicit, Size = 4)]
        public struct FastColor
        {
            [FieldOffset(0)] public byte r;
            [FieldOffset(1)] public byte g;
            [FieldOffset(2)] public byte b;
            [FieldOffset(3)] public byte a;

            public byte this[int i]
            {
                get
                {
                    switch (i)
                    {
                        default:
                            return r;
                        case 1:
                            return g;
                        case 2:
                            return b;
                        case 3:
                            return a;
                    }
                }

                set
                {
                    switch (i)
                    {
                        default:
                            r = value;
                            break;
                        case 1:
                            g = value;
                            break;
                        case 2:
                            b = value;
                            break;
                        case 3:
                            a = value;
                            break;
                    }
                }
            }

            public FastColor(byte r, byte g, byte b, byte a)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;
            }

            public static implicit operator Color(FastColor c) => Color.FromArgb(c.a, c.r, c.g, c.b);
            public static implicit operator FastColor(Color c) => new FastColor(c.R, c.G, c.B, c.A);

            public override string ToString() => $"RGBA: ({r}, {g}, {b}, {a})";
        }
    }
}
