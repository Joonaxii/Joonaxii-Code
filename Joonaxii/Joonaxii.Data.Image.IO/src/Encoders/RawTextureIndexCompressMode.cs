namespace Joonaxii.Data.Image.IO
{
    public enum RawTextureIndexCompressMode : short
    {
        Auto = -1,
        None = 0,

        Huffman = 1,
        RLE     = 2,

        RLEHuffman = 3,
    }
}