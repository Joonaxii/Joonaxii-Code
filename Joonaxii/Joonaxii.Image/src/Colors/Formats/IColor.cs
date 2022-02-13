
namespace Joonaxii.Image
{
    public interface IColor
    {
        void GetValues(out float v0, out float v1, out float v2, out float v3);
        IColor Lerp(IColor to, float t);

        //float InverseLerp(IColor to, IColor input);

        int GetHashCode();
    }
}