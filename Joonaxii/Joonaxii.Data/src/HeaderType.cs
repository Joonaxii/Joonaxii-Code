namespace Joonaxii.Data
{
    [System.Flags]
    public enum HeaderType : byte
    {
        NONE                    = 0,
        UNKNOWN                 = 1,

        JPEG                    = 2,
        PNG                     = 4,
        GIF87                   = 8,
        GIF89                   = 16,
        WEBP                    = 32,
        BMP                     = 64,

        RAW_TEXTURE             = 128,

        IMAGE_FORMAT            = JPEG | PNG | GIF87 | GIF89 | WEBP | BMP,
    }
}