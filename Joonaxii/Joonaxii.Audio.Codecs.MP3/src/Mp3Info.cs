using System;

namespace Joonaxii.Audio.Codecs.MP3
{
    public struct Mp3Info : IEquatable<Mp3Info>
    {
        public static Mp3Info Zero { get; } = new Mp3Info(0, 0, false, 0);

        public uint sampleRate;
        public uint bitRate;
        public bool stereo;
        public uint crc;

        public Mp3Info(uint smapleRate, uint bitRate, bool stereo, uint crc)
        {
            this.sampleRate = smapleRate;
            this.bitRate = bitRate;
            this.stereo = stereo;
            this.crc = crc;
        }

        public override bool Equals(object obj)
        {
            return obj is Mp3Info info && Equals(info);
        }

        public bool Equals(Mp3Info other)
        {
            return sampleRate == other.sampleRate &&
                   bitRate == other.bitRate &&
                   stereo == other.stereo &&
                   crc == other.crc;
        }

        public override int GetHashCode()
        {
            int hashCode = -268915139;
            hashCode = hashCode * -1521134295 + sampleRate.GetHashCode();
            hashCode = hashCode * -1521134295 + bitRate.GetHashCode();
            hashCode = hashCode * -1521134295 + stereo.GetHashCode();
            hashCode = hashCode * -1521134295 + crc.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Mp3Info left, Mp3Info right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Mp3Info left, Mp3Info right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"Mp3 Info: \n -Sample Rate: {sampleRate}\n -Bit Rate: {bitRate}\n -Stereo: {stereo}\n -CRC: 0x{Convert.ToString(crc, 16).PadLeft(8, '0')}";
        }
    }
}