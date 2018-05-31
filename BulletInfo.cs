namespace FDLearnAim
{
    public class BulletInfo
    {
        public double Power { get; private set; }
        public double HeadingRadians { get; private set; }
        public double GuessFactor { get; private set; }
        public Segmentation ApplicableSegments { get; private set; }

        public BulletInfo(double guessFactor, double power, Segmentation segments)
        {
            GuessFactor = guessFactor;
            Power = power;
            ApplicableSegments = segments;
        }

        public void UpdateInfo(double headingRadians)
        {
            HeadingRadians = headingRadians;
        }
    }
}
