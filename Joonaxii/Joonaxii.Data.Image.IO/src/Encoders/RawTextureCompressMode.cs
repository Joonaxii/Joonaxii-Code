namespace Joonaxii.Data.Image.Conversion
{
    public enum RawTextureCompressMode : short
    {
        Auto = -1,
        None = 0,

        IdxHuffman    = 1,
        IdxRLE        = 2,

        IdxRLEHuffman = 3,

        aRLE          = 4,
        IdxaRLE       = 5,
        RGBA_RLE      = 6,
    }
}