namespace Joonaxii.Image
{
    public class ColorContainer
    {
        public FastColor color;
        public int count;
        public int index;

        public ColorContainer(FastColor color, int count, int index)
        {
            this.color = color;
            this.count = count < 0 ? 0 : count;
            this.index = index;
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
