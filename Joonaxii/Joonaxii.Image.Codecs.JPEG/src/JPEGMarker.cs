namespace Joonaxii.Image.Codecs.JPEG
{
    public enum JPEGMarker
    {
        Padding = 0xFF,

        SOI    = 0xD8,

        App0     = 0xE0,
        App1     = 0xE1,
        App2     = 0xE2,
        App3     = 0xE3,
        App4     = 0xE4,
        App5     = 0xE5,
        App6     = 0xE6,
        App7     = 0xE7,
        App8     = 0xE8,
        App9     = 0xE9,
        App10    = 0xEA,
        App11    = 0xEB,
        App12    = 0xEC,
        App13    = 0xED,
        App14    = 0xEE,
        App15    = 0xEF,

        SOF0  = 0xC0,
        SOF1  = 0xC1,
        SOF2  = 0xC2,
        SOF3  = 0xC3,
      
        SOF5  = 0xC5,
        SOF6  = 0xC6,
        SOF7  = 0xC7,
        SOF8  = 0xC8,
        SOF9  = 0xC9,
        SOF10 = 0xCA,
        SOF11 = 0xCB,

        SOF13 = 0xCD,
        SOF14 = 0xCE,
        SOF15 = 0xCF,

        DEF_HUFF = 0xC4,
        DEF_AR_COD = 0xCC,

        DEF_QUANT = 0xDB,
        DEF_NUM_OF_LINES = 0xDC,
        DEF_RESTART_INT = 0xDD,

        SOS = 0xDA,

        DEF_RESTART_0 = 0xD0,
        DEF_RESTART_1 = 0xD1,
        DEF_RESTART_2 = 0xD2,
        DEF_RESTART_3 = 0xD3,
        DEF_RESTART_4 = 0xD4,
        DEF_RESTART_5 = 0xD5,
        DEF_RESTART_6 = 0xD6,
        DEF_RESTART_7 = 0xD7,

        COMMENT = 0xFE,
        EOI = 0xD9,
    }
}