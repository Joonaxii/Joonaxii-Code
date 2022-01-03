namespace Joonaxii.Data.Image.IO
{
    public enum ImageDecodeResult
    {
        Success,

        InvalidImageFormat,
        WebpDecoderMissing,

        NotSupported,

        DecodeFailed,
    }
}