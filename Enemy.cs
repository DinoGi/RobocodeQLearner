using System.Drawing;

namespace FDLearnAim
{
    public class EnemyInfo
    {
        public double LastEnergy { get; set; }
        public PointF LastPosition { get; set; }
        public long RecordedTime { get; set; }
        public double LastDistance { get; set; }
        public bool Fired { get; set; }

    }
}
