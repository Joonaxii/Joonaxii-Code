namespace Joonaxii.Image.Codecs.PNG
{
    [System.Flags]
    public enum PNGFlags
    {
        None                     = 0,
 
        OverrideFilter          = 1,
        ForceFilter             = 2,

        UseBrokenSubFilter      = 4,
    }
}