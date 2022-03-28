namespace Joonaxii.Audio.Codecs.OGG
{
    [System.Flags]
    public enum OggHeaderType : byte
    {
        None = 0,

        Continuation = 1,
        BOS = 2,
        EOS = 4,
    }
}