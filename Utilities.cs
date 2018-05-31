using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode;

namespace FDLearnAim
{
    public class Utilities
    {
        public static double GetRandomNumber(Random generator, double minimum, double maximum)
        {
            return generator.NextDouble() * (maximum - minimum) + minimum;
        }

        /// <summary>
        /// When firing, the Maximum Escape Angle (MEA) is the largest angle offset from zero 
        /// (i.e., Head-On Targeting) that could possibly hit an enemy bot.
        /// Note that the actual maximum depends on the enemy's current heading, speend and Wall Distance
        /// </summary>
        /// <param name="firepower"></param>
        /// <returns></returns>
        public static double CalculateMaximumEscapeAngle(double firepower)
        {
            return Math.Asin(Rules.MAX_VELOCITY / Rules.GetBulletSpeed(firepower));
        }

        public static BulletInfo FindBulletInfoInBulletList(IList<BulletInfo> bullets, Bullet targetBullet)
        {
            if (bullets == null || bullets.Count == 0 )
            {
                return null;
            }

            foreach (var bullet in bullets)
            {
                if (Math.Abs(bullet.Power - targetBullet.Power) < 0.0001d &&
                    Math.Abs(bullet.HeadingRadians - targetBullet.HeadingRadians) < 0.0001d)
                {
                    return bullet;
                }
            }

            return null;
        }

        /// <summary>
        /// Scale a value from a range of [min, max] to [newMin, newMax]
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="newMin"></param>
        /// <param name="newMax"></param>
        /// <returns></returns>
        public static double MapToNewScale(double value, double min, double max, double newMin, double newMax)
        {
            var numerator = (newMax - newMin) * (value - min);
            return (numerator / (max - min)) + newMin;
        }

        /// <summary>
        /// Is current movement clockwise? 
        /// </summary>
        /// <param name="initialVector"></param>
        /// <param name="finalVector"></param>
        /// <returns></returns>
        public static bool IsMovementClockwise(PointF initialVector, PointF finalVector)
        {
            return (finalVector.X * initialVector.Y) - (finalVector.Y * initialVector.X) > 0;
        }

        /// <summary>
        /// Calculate a robot position based on my position and distance
        /// </summary>
        /// <param name="myPosition"></param>
        /// <param name="angle"> the absolute angle</param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static PointF CalculateRobotPosition(PointF myPosition, double angle, double distance)
        {
            var xAmount = (float) (Math.Sin(angle) * distance);
            var yAmount = (float) (Math.Cos(angle) * distance);

            return new PointF(myPosition.X + xAmount, myPosition.Y + yAmount);
        }

        public static double GetDistance(PointF point1, PointF point2)
        {
            //pythagorean theorem c^2 = a^2 + b^2
            //thus c = square root(a^2 + b^2)
            var a = (double)(point2.X - point1.X);
            var b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }
    }
}
