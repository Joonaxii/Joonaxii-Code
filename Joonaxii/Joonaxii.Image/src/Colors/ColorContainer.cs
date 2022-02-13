namespace Joonaxii.Image
{
    public class ColorContainer
    {
        public FastColor color;
        public int count;

        public ColorContainer(FastColor color, int count)
        {
            this.color = color;
            this.count = count < 0 ? 0 : count;
        }
    }

    public struct ColorContainerValue
    {
        public FastColor color;
        public int count;

        public ColorContainerValue(FastColor color, int count)
        {
            this.color = color;
            this.count = count;
        }
    }
}
