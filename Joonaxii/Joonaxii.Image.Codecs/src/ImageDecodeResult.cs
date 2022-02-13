namespace Joonaxii.Image.Codecs
{
    public enum ImageDecodeResult
    {
        Success,

        InvalidImageFormat,
        WebpDecoderMissing,

        NotSupported,
        DataCorrupted,

        DuplicateChunkFound,
        DataMisalignment,

        HashMismatch,

        DecodeFailed,
    }
}