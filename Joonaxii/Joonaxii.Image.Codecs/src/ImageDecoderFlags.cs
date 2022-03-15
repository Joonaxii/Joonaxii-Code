namespace Joonaxii.Image.Codecs
{
    [System.Flags]
    public enum ImageDecoderFlags
    {
        None = 0,

        ForcePalette = 1,
        ForceNoPalette = 2,

        ForceAlpha = 4,
        ForceNoAlpha = 8,

        ForceRGB = 16,

        AllowBigIndices = 32,
    }
}