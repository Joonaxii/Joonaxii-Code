namespace Joonaxii.Data.Image.Conversion
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