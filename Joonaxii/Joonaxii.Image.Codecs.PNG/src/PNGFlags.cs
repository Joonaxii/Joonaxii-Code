namespace Joonaxii.Image.Codecs.PNG
{
    [System.Flags]
    public enum PNGFlags
    {
        None                     = 0,
                                 
        ForcePalette             = 1,
        ForceAlpha               = 2,
        ForceNoAlpha             = 4,
        ForceRGB                 = 4,

        AllowBigIndices         = 16,

        OverrideFilter          = 32,
        ForceFilter             = 64,
    }
}