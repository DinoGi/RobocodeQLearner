using System;
using Robocode;
using Robocode.Util;

namespace FDLearnAim
{
    public class AbstractRobot
    {
        private readonly AdvancedRobot _robot;
        private readonly Random _dodgeGenerator;

        public double MovingDirection { get; set; }

        public AbstractRobot(AdvancedRobot robot)
        {
            _dodgeGenerator = new Random();
            MovingDirection = 1d;
            _robot = robot;
        }

        internal void SetMoveToWallAndBack(double robotPositionX, double robotPositionY, bool shouldRam)
        {
            if (!SetMoveAhead(robotPositionX, robotPositionY, MovingDirection, shouldRam))
            {
                SetMoveAhead(robotPositionX, robotPositionY, -MovingDirection, shouldRam);
                MovingDirection = -MovingDirection;
            }
        }

        /// <summary>
        /// Moves the robot ahead if there are no walls in the immediate vicinity
        /// </summary>
        /// <param name="robotPositionX"></param>
        /// <param name="robotPositionY"></param>
        /// <param name="direction"> should be 1 or -1</param>
        /// <param name="shouldRam"> if true robot will ram lower health enemies</param>
        /// <returns></returns>
        internal bool SetMoveAhead(double robotPositionX, double robotPositionY, double direction, bool shouldRam)
        {
            var width = _robot.BattleFieldWidth - (_robot.Width / 2);
            var height = _robot.BattleFieldHeight - (_robot.Height / 2);

            var velocity = AddBrakeVelocity(Math.Abs(_robot.Velocity));

            var x = Math.Round(robotPositionX + Math.Sin(_robot.HeadingRadians) * (velocity * direction + direction));
            var y = Math.Round(robotPositionY + Math.Cos(_robot.HeadingRadians) * (velocity * direction + direction));

            if (x >= width || x <= (_robot.Width/2) || y >= height || y <= (_robot.Height/2))
            {
                return false;
            }

            //TODO ram enemies only if their health is lower
            _robot.SetAhead(direction * 30);
            return true;
        }

        /// <summary>
        /// Adds the speed of reversing into current velocity
        /// </summary>
        /// <param name="velocity"></param>
        /// <returns></returns>
        internal static double AddBrakeVelocity(double velocity)
        {
            if (Math.Abs(velocity - 0.0d) < 0.0001)
            {
                return 0;
            }
            var vel = Math.Abs(velocity - Rules.DECELERATION);
            if (vel > 0.0d && vel < Rules.DECELERATION)
            {
                return AddBrakeVelocity(0) + velocity;
            }

            return AddBrakeVelocity(vel) + velocity;

        }

        internal void RandomDodge()
        {
            var randomTurn = Utilities.GetRandomNumber(_dodgeGenerator, -17d, 17d);
            if (Math.Abs(randomTurn) < 5d)
            {
                randomTurn *= 2.5d;
            }
            _robot.SetTurnRight(randomTurn);

            //Should the robot reverse?
            var revProb = _dodgeGenerator.NextDouble();
            if (revProb < .1d)
            {
                MovingDirection = -MovingDirection;
            }
        }

        /// <summary>
        /// Only fire the gun if the gun has stopped turning
        /// </summary>
        /// <param name="firepower"></param>
        internal bool CheckFire(double firepower)
        {
            if (Math.Abs(_robot.GunTurnRemainingRadians) < 0.01d && _robot.GunHeat <= 0.0d)
            {
                _robot.SetFire(firepower);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Move the gun to the desired bearing
        /// </summary>
        /// <param name="desiredGunBearingRadians"></param>
        internal void SetMoveGunToDesiredBearing(double desiredGunBearingRadians)
        {
            var turnAmount = Utils.NormalRelativeAngle(desiredGunBearingRadians - _robot.GunHeadingRadians);
            _robot.SetTurnGunRightRadians(turnAmount);
        }

        /// <summary>
        /// Point the gun at the target robot. 
        /// Returns the desired gun bearing, so it is possible to check when the gun is at the position we want
        /// </summary>
        /// <param name="targetBearingRadians"></param>
        /// <returns></returns>
        internal double SetTurnGunToRobot(double targetBearingRadians)
        {
            // Calculate exact location of the robot
            var desiredGunBearing = _robot.HeadingRadians + targetBearingRadians;
            var bearingFromGun = Utils.NormalRelativeAngle(desiredGunBearing - _robot.GunHeadingRadians);
            _robot.SetTurnGunRightRadians(bearingFromGun);

            return desiredGunBearing;
        }

        /// <summary>
        /// Keeps radar locked on a target. If you wish to move the radar independently from the robot make sure 
        /// "IsAdjustRadarForGunTurn" is set to true.
        /// Since robots are 36*36 pixels and max velocity is 8px it is possible
        /// to always keep the radar on a target. Usually you'd call this method on the OnScannedRobot event
        /// </summary>
        /// <param name="targetBearingRadians"></param>
        internal void SetTurnMultiplierRadarLock(double targetBearingRadians)
        {
            var radarTurn =
                // Absolute bearing to target
                _robot.HeadingRadians + targetBearingRadians
                // Subtract current radar heading to get turn required
                - _robot.RadarHeadingRadians;

            _robot.SetTurnRadarRightRadians(2.0 * Utils.NormalRelativeAngle(radarTurn));
        }
    }
}
