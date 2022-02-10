namespace Joonaxii.Audio
{
    [System.Flags]
    public enum SampleConversionMode
    {
        None        = 0,     
        Convert     = 1,
        Destructive = 2,
    }
}