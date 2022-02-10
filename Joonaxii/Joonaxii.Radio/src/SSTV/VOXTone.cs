namespace Joonaxii.Radio
{
    public struct VOXTone
    {
        public double frequency;
        public double duration;

        public double fadeIn;
        public double fadeOut;
   
        public VOXTone(double frequency, double duration, double fadeIn = 0.0, double fadeOut = 0.0)
        {
            this.frequency = frequency;
            this.duration = duration;
            this.fadeIn = fadeIn;
            this.fadeOut = fadeOut;
        }
    }
}