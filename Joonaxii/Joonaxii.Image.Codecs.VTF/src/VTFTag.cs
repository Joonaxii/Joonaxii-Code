namespace Joonaxii.Image.Codecs.VTF
{
    public enum VTFTag
    {
        VTF_HEADER    = 0x00465456,

        LOW_RES_THUMB = 0x00_00_01,
        HI_RES_IMAGE  = 0x00_00_30,
        ANIM_SHEET    = 0x00_00_10,

        CRC           = 0x435243,
        LOD           = 0x444F4C,
        TSD           = 0x445354,
        KVD           = 0x44564B,
    }
}