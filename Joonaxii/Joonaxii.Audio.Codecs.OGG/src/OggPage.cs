using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Joonaxii.Audio.Codecs.OGG
{
    [StructLayout(LayoutKind.Sequential, Size = OGG_PAGE_SIZE +  8)]
    public struct OggPage : IEquatable<OggPage>
    {
        public const int OGG_PAGE_SIZE = 27;

        public long startPos;

        public OggPattern syncPattern;
        public byte version;
        public OggHeaderType type;

        public ulong granulePos;
        public uint bitSerial;
        public uint pageSequenceNum;
        public uint checksum;
        public byte pageSegments;

        public OggPage(long startPos, OggPattern pattern, BinaryReader br)
        {
            this.startPos = startPos;
            syncPattern = pattern;
            version = br.ReadByte();
            type = (OggHeaderType)br.ReadByte();

            granulePos = br.ReadUInt64();
            bitSerial = br.ReadUInt32();
            pageSequenceNum = br.ReadUInt32();
            checksum = br.ReadUInt32();
            pageSegments = br.ReadByte();
        }

        public void SkipSegmentTable(Stream stream)
        {
            long pos = stream.Position;
            int bytes = 0;
            for (int i = 0; i < pageSegments; i++)
            {
                int val = stream.ReadByte();
                if(val < 0) { continue; }
                bytes += val;
            }
            stream.Seek(pos + pageSegments + bytes, SeekOrigin.Begin);
        }

        public override bool Equals(object obj) => obj is OggPage page && Equals(page);

        public bool Equals(OggPage other)
        {
            return syncPattern == other.syncPattern &&
                   version == other.version &&
                   type == other.type &&
                   granulePos == other.granulePos &&
                   bitSerial == other.bitSerial &&
                   pageSequenceNum == other.pageSequenceNum &&
                   checksum == other.checksum &&
                   pageSegments == other.pageSegments;
        }

        public override int GetHashCode()
        {
            int hashCode = 623684596;
            hashCode = hashCode * -1521134295 + syncPattern.GetHashCode();
            hashCode = hashCode * -1521134295 + version.GetHashCode();
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            hashCode = hashCode * -1521134295 + granulePos.GetHashCode();
            hashCode = hashCode * -1521134295 + bitSerial.GetHashCode();
            hashCode = hashCode * -1521134295 + pageSequenceNum.GetHashCode();
            hashCode = hashCode * -1521134295 + checksum.GetHashCode();
            hashCode = hashCode * -1521134295 + pageSegments.GetHashCode();
            return hashCode;
        }

        public string ToShortString() => $"{syncPattern}, {version}, {type}, {granulePos}, {bitSerial}, {pageSequenceNum}, {checksum}, {pageSegments}";

        public override string ToString() => ToString("");
        public string ToString(string indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{syncPattern}");
            sb.AppendLine($"{indent} -Version: {version}");
            sb.AppendLine($"{indent} -Type: {type}");
            sb.AppendLine($"{indent} -Granule Position: {granulePos}");
            sb.AppendLine($"{indent} -Bitstream Serial Number: {bitSerial}");
            sb.AppendLine($"{indent} -Page Sequence Number: {pageSequenceNum}");
            sb.AppendLine($"{indent} -Checksum: 0x{Convert.ToString(checksum, 16).PadLeft(8, '0')}");
            sb.AppendLine($"{indent} -Page Segments: {pageSegments}");
            return sb.ToString();
        }

        public static bool operator ==(OggPage left, OggPage right) => left.Equals(right);
        public static bool operator !=(OggPage left, OggPage right) => !(left == right);
    }
}