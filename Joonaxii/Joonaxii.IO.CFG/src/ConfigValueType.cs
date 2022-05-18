namespace Joonaxii.IO.CFG
{
    public enum ConfigValueType
    {
        Unknown = 0xFF,

        Int = 0x0,
        Float = 0x1,
        Bool = 0x2,
        Char = 0x3,
        String = 0x4,
        Enum = 0x5,
    }
}