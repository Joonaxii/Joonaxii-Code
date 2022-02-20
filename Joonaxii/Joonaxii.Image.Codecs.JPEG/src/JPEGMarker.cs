namespace Joonaxii.Image.Codecs.JPEG
{
    public enum JPEGMarker
    {
        Padding           = 0xFF00,

        SOI               = 0xFFD8,

        App0              = 0xFFE0,
        App1              = 0xFFE1,
        App2              = 0xFFE2,
        App3              = 0xFFE3,
        App4              = 0xFFE4,
        App5              = 0xFFE5,
        App6              = 0xFFE6,
        App7              = 0xFFE7,
        App8              = 0xFFE8,
        App9              = 0xFFE9,
        App10             = 0xFFEA,
        App11             = 0xFFEB,
        App12             = 0xFFEC,
        App13             = 0xFFED,
        App14             = 0xFFEE,
        App15             = 0xFFEF,

        SOF0              = 0xFFC0,
        SOF1              = 0xFFC1,
        SOF2              = 0xFFC2,
        SOF3              = 0xFFC3,
      
        SOF5              = 0xFFC5,
        SOF6              = 0xFFC6,
        SOF7              = 0xFFC7,
        SOF8              = 0xFFC8,
        SOF9              = 0xFFC9,
        SOF10             = 0xFFCA,
        SOF11             = 0xFFCB,

        SOF13             = 0xFFCD,
        SOF14             = 0xFFCE,
        SOF15             = 0xFFCF,

        DEF_HUFF          = 0xFFC4,
        DEF_AR_COD        = 0xFFCC,

        DEF_QUANT         = 0xFFDB,
        DEF_NUM_OF_LINES  = 0xFFDC,
        DEF_RESTART_INT   = 0xFFDD,

        SOS               = 0xFFDA,
                          
        DEF_RESTART_0     = 0xFFD0,
        DEF_RESTART_1     = 0xFFD1,
        DEF_RESTART_2     = 0xFFD2,
        DEF_RESTART_3     = 0xFFD3,
        DEF_RESTART_4     = 0xFFD4,
        DEF_RESTART_5     = 0xFFD5,
        DEF_RESTART_6     = 0xFFD6,
        DEF_RESTART_7     = 0xFFD7,

        COMMENT           = 0xFFFE,
        EOI               = 0xFFD9,
    }
}