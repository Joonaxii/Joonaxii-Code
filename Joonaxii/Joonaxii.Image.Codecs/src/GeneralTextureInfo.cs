using System;

namespace Joonaxii.Image.Codecs
{
    public struct GeneralTextureInfo : IEquatable<GeneralTextureInfo>
    {
        public static GeneralTextureInfo Zero { get; } = new GeneralTextureInfo();

        public ushort width;
        public ushort height;

        public byte bitsPerPixel;

        public GeneralTextureInfo(int width, int height, int bitsPerPixel)
        {
            this.width = (ushort)width;
            this.height = (ushort)height;
            this.bitsPerPixel = (byte)bitsPerPixel;
        }

        public override bool Equals(object obj)
        {
            return obj is GeneralTextureInfo info && Equals(info);
        }

        public bool Equals(GeneralTextureInfo other)
        {
            return width == other.width &&
                   height == other.height &&
                   bitsPerPixel == other.bitsPerPixel;
        }

        public override int GetHashCode()
        {
            int hashCode = 122608113;
            hashCode = hashCode * -1521134295 + width.GetHashCode();
            hashCode = hashCode * -1521134295 + height.GetHashCode();
            hashCode = hashCode * -1521134295 + bitsPerPixel.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(GeneralTextureInfo left, GeneralTextureInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeneralTextureInfo left, GeneralTextureInfo right)
        {
            return !(left == right);
        }
    }
}