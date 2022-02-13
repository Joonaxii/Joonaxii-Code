namespace Joonaxii.Image.Codecs.PNG
{
    public enum PNGChunkType
    {
        IHDR = 0x49_48_44_52,

        PLTE = 0x50_4C_54_45,
        IDAT = 0x49_44_41_54,

        tRNS = 0x74_52_4E_53,
        gAMA = 0x67_41_4D_41,
        cHRM = 0x63_48_52_4D,
        sRGB = 0x73_52_47_42,
        iCCP = 0x69_43_43_50,

        iTXt = 0x69_54_58_74,
        tEXt = 0x74_45_58_74,
        zTXt = 0x7A_54_58_74,

        bKGD = 0x62_4B_47_44,
        pHYs = 0x70_48_59_73,
        sBIT = 0x73_42_49_54,
        sPLT = 0x73_50_4C_54,
        hIST = 0x68_49_53_54,
        tIME = 0x74_49_4D_45,

        IEND = 0x49_45_4E_44,
    }
}