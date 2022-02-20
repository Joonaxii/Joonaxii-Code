namespace New.JPEG
{
    public struct JPEGSample
    {
        public byte id;

        public int XSize { get => scale & 0xF; }
        public int YSize { get => (scale >> 4) & 0xF; }

        public byte scale;
        public byte index;

        public JPEGSample(uint value)
        {
            id =    (byte)(value & 0xFF);
            scale = (byte)((value >> 8) & 0xFF);
            index = (byte)((value >> 16) & 0xFF);
        }

        public override string ToString() => $"{id}, {XSize}x{YSize}, {index}";
    }
}