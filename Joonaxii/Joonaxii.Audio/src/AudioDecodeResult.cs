namespace Joonaxii.Audio
{
    public enum AudioDecodeResult
    {
        Success,

        Unsupported,
        UnsupportedVersion,
        InvalidFormat,
        AudioDecodeFailed,
        AudioCorruption,
    }
}