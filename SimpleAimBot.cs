using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Robocode;
using Robocode.Util;

namespace FDLearnAim
{
    public class SimpleAimBot : AdvancedRobot
    {
        //How long should the turret start calculating predicted position before gun is ready
        private const int AimPrepareTime = 1;
        private Random _randomGenOffset;
        private Random _randomGenFirePower;

        private bool _isAiming = false;
        private double? _desiredGunBearing = null;
        private BulletInfo _bulletToFire = null;

        private PointF _position
        {
            get
            {
                return new PointF((float)X, (float)Y);
            }
        }

        private EnemyInfo _enemy;
        private readonly AbstractRobot _robotController;

        private static QLearning _aimLearner;

        private readonly List<BulletInfo> _bulletsFired = new List<BulletInfo>();
        private static bool _hasCreatedScores;
        private float _scoreMultiplier = 2.5f;
        private const double MinBulletScore = 1d;
        private double _baseFirePower;
        private double _maxAllowableBaseFirePower;
        private const double FirePowerIncreaseAmount = 0.05d;
        private const double MinEnergyToFire = 20d;
        private const double AllowableFireRadius = 75d;

        private int _scanlessTime;

        private long _nrbulletsFired = 1L;
        private long _nrbulletsHit = 1L;

        private const bool AlwaysFireRandom = false;

        private double Accuracy
        {
            get
            {
                //If robot has not fired, it has perfect accuracy :)
                if (_nrbulletsFired == 0L)
                {
                    return 1d;
                }
                return _nrbulletsHit / (double)_nrbulletsFired;
            }
        }


        private void ResetFirePowerLevels()
        {
            if (Rules.MIN_BULLET_POWER > 1.1d)
            {
                _baseFirePower = Rules.MIN_BULLET_POWER;
            }
            else
            {
                _baseFirePower = 1.0d;
            }

            if (Rules.MAX_BULLET_POWER < _maxAllowableBaseFirePower)
            {
                _maxAllowableBaseFirePower = Rules.MAX_BULLET_POWER;
            }
            else
            {
                _maxAllowableBaseFirePower = 1.6d;
            }
        }
        public SimpleAimBot()
            : base()
        {
            var nrStatesQLearning = 9;
            ResetFirePowerLevels();

            _enemy = new EnemyInfo();
            _robotController = new AbstractRobot(this);

            if (!_hasCreatedScores)
            {
                _aimLearner = new QLearning(
                    Segmentation.None  
                    | Segmentation.DistanceClose | Segmentation.DistanceFar | 
                    Segmentation.VelocityFast | Segmentation.VelocitySlow
                    ,
                    nrStatesQLearning, 0.0d);

                //Make sure discount factor is 1, as we will use temperature as the exploration rate
                _aimLearner.DiscountFactor = 1.0f;
                _aimLearner.Temperature = .2f;
                _aimLearner.TemperatureDecraseAmount = _aimLearner.Temperature / 20f;
                _aimLearner.MinTemperature = .005f;
                _aimLearner.UseSoftmaxSelection = true;

                //Start with positive scores so if robot keeps missing the scores go down
                _aimLearner.UpdateAllLearningScores(1.0d);
                _aimLearner.ResetFavorableActions(1);

                _hasCreatedScores = true;
            }
            else
            {
                //Increase temperature slightly as opponent might switch dodging strategy
                _aimLearner.Temperature += (.2f/20f) * 5;
            }
        }

        public override void Run()
        {
            _randomGenOffset = new Random();
            _randomGenFirePower = new Random();

            IsAdjustRadarForGunTurn = true;
            IsAdjustGunForRobotTurn = true;
            SetTurnRadarRightRadians(double.PositiveInfinity);
            while (true)
            {
                _scanlessTime++;

                SetMoveGun();

                if (_enemy.Fired)
                {
                    _robotController.RandomDodge();
                }

                _robotController.SetMoveToWallAndBack(_position.X, _position.Y, true);

                if (_isAiming)
                {
                    //If we fire the gun, reset aiming state 
                    if (_robotController.CheckFire(_bulletToFire.Power))
                    {
                        _isAiming = false;
                        _desiredGunBearing = null;

                        _bulletToFire.UpdateInfo(GunHeadingRadians);
                        _bulletsFired.Add(_bulletToFire);
                        _bulletToFire = null;
                    }
                }
                //Fire at disabled enemy!
                if (_enemy.LastEnergy <= 0d)
                {
                    _robotController.CheckFire(Rules.MIN_BULLET_POWER);
                }
                if (_scanlessTime > 3)
                {
                    SetTurnRadarRightRadians(double.PositiveInfinity);
                }
                Execute();
            }
        }

        private void SetMoveGun()
        {
            if (!_desiredGunBearing.HasValue)
                return;

            //We have reached target
            if (GunTurnRemaining < 0.01d)
            {
                _desiredGunBearing = null;
                return;
            }

            //Keep rotating
            _robotController.SetMoveGunToDesiredBearing(_desiredGunBearing.Value);
        }

        private bool IsAllowedFire()
        {
            return Energy > MinEnergyToFire || _enemy.LastDistance < AllowableFireRadius;
        }

        private bool IsGunReady()
        {
            return GunHeat <= GunCoolingRate * AimPrepareTime;
        }

        private Segmentation GetApplicableSegments(double targetDistance, double targetHeadingRadians, double velocity)
        {
            Segmentation velocitySegment;

            if (Math.Abs(velocity) > (Rules.MAX_VELOCITY / 2))
            {
                velocitySegment = Segmentation.VelocityFast;
            }
            else
            {
                velocitySegment = Segmentation.VelocitySlow;
            }

            Segmentation distanceSegment;
            if (Math.Abs(targetDistance) > 150d)
            {
                distanceSegment = Segmentation.DistanceFar;
            }
            else
            {
                distanceSegment = Segmentation.DistanceClose;

            }

            return Segmentation.None |  velocitySegment | distanceSegment;
        }

        /// <summary>
        ///   onScannedRobot:  Stop and Fire!
        /// </summary>
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            _enemy.Fired = false;
            var energyDifference = Math.Abs(_enemy.LastEnergy - e.Energy);
            if (energyDifference >= Rules.MIN_BULLET_POWER && energyDifference <= Rules.MAX_BULLET_POWER)
            {
                _enemy.Fired = true;
            }
            //Keep our enemy information up to date
            UpdateEnemyInfo(_enemy, e);

            _scanlessTime = 0;
            //If we are close to firing, update Guess factors so gun has time to turn to desired place
            if (IsGunReady() && !_isAiming && e.Energy > 0 && IsAllowedFire())
            {
                //Fire bullets with random power so we can more easily keep track of them
                var firepower = Rules.MIN_BULLET_POWER;
                if (_enemy.LastDistance < AllowableFireRadius)
                {
                    firepower = Rules.MAX_BULLET_POWER;
                }
                else
                {
                    firepower = Utilities.GetRandomNumber(_randomGenFirePower, _baseFirePower, _baseFirePower + 0.3d);
                }

                //TODO update the enemy info for the last 1 ticks so we know how to update the maximum escape angle
                var maxEscapeAngle = Utilities.CalculateMaximumEscapeAngle(firepower);

                var applicableSegments = GetApplicableSegments(e.Distance, e.HeadingRadians, e.Velocity);

                var desiredState = _aimLearner.SelectQLearningState(applicableSegments);

                var rangeOfAnglesMin = Utilities.MapToNewScale(desiredState, 0, _aimLearner.NrStates,
                    -maxEscapeAngle, maxEscapeAngle);
                var rangeOfAnglesMax = Utilities.MapToNewScale(desiredState + 1, 0, _aimLearner.NrStates,
                    -maxEscapeAngle, maxEscapeAngle);

                //just fire randomly for baseline purposes
                var gunOffset = 0.0d;
                if (AlwaysFireRandom)
                {
                    gunOffset = Utilities.GetRandomNumber(_randomGenOffset, -maxEscapeAngle, maxEscapeAngle);
                }
                else
                {
                    //We want to rotate the gun a random amount between [-maxEscapeAngle, maxEscapeAngle]
                    gunOffset = Utilities.GetRandomNumber(_randomGenOffset, rangeOfAnglesMin, rangeOfAnglesMax);
                }

                //Normalize guessFactor from -1 to 1
                _bulletToFire = new BulletInfo(gunOffset / maxEscapeAngle, firepower, applicableSegments);

                _desiredGunBearing = HeadingRadians + e.BearingRadians + gunOffset;

                _robotController.SetMoveGunToDesiredBearing(_desiredGunBearing.Value);

                _isAiming = true;
            }

            if (!_isAiming)
            {
                //Keep tracking robot with gun
                _robotController.SetTurnGunToRobot(e.BearingRadians);
            }

            _robotController.SetTurnMultiplierRadarLock(e.BearingRadians);
        }

        private void UpdateEnemyInfo(EnemyInfo enemyInfoToUpdate, ScannedRobotEvent e)
        {
            var currentTargetPosition =
                Utilities.CalculateRobotPosition(_position, Utils.NormalAbsoluteAngle(this.HeadingRadians + e.BearingRadians), e.Distance);

            enemyInfoToUpdate.LastPosition = currentTargetPosition;
            enemyInfoToUpdate.RecordedTime = Time;
            enemyInfoToUpdate.LastDistance = e.Distance;
            enemyInfoToUpdate.LastEnergy = e.Energy;
        }

        public override void OnPaint(IGraphics graphics)
        {
            base.OnPaint(graphics);
            if (_desiredGunBearing.HasValue)
            {
                var point2 = new PointF(_position.X + (float)Math.Sin(_desiredGunBearing.Value) * 60, _position.Y + (float)Math.Cos(_desiredGunBearing.Value) * 60);

                graphics.DrawLine(Pens.Red, _position, point2);
            }

            //Draw allowable fire distance
            var transparentGreen = new SolidBrush(Color.FromArgb(30, 0, 0xFF, 0));
            graphics.FillEllipse(transparentGreen, (int)(X - AllowableFireRadius), (int)(Y - AllowableFireRadius),
                (float)AllowableFireRadius * 2f, (float)AllowableFireRadius * 2f);
        }

        public override void OnBulletHit(BulletHitEvent evnt)
        {
            var bullet = Utilities.FindBulletInfoInBulletList(_bulletsFired, evnt.Bullet);

            if (bullet == null)
                return;

            var state = (int)Math.Round(Utilities.MapToNewScale(bullet.GuessFactor, -1.0d, 1.0d, 0d,
                _aimLearner.NrStates - 1));

            _aimLearner.IncreaseRatio(bullet.ApplicableSegments, state);
            _aimLearner.DecreaseTemperature();

            _nrbulletsFired++;
            _nrbulletsHit++;

            _bulletsFired.Remove(bullet);

            IncreaseMinFirePower(FirePowerIncreaseAmount);
        }

        private void IncreaseMinFirePower(double amount)
        {
            if (_baseFirePower > _maxAllowableBaseFirePower)
            {
                return;
            }

            _baseFirePower += amount;
        }

        public override void OnBulletMissed(BulletMissedEvent evnt)
        {
            var bullet = Utilities.FindBulletInfoInBulletList(_bulletsFired, evnt.Bullet);

            if (bullet == null)
                return;

            var state = (int)Math.Round(Utilities.MapToNewScale(bullet.GuessFactor, -1.0d, 1.0d, 0d,
                _aimLearner.NrStates - 1));

            _aimLearner.DecreaseRatio(bullet.ApplicableSegments, state);

            _nrbulletsFired++;

            _bulletsFired.Remove(bullet);
        }


        public override void OnRoundEnded(RoundEndedEvent evnt)
        {
            base.OnRoundEnded(evnt);

            Console.WriteLine("------ My Learning Scores are: -------- ");
            Console.WriteLine(_aimLearner);

            Console.WriteLine("Accuracy: ");
            Console.WriteLine(Accuracy);

            using (var outFile = this.GetDataFile("Accuracy_" + this.Name + ".csv"))
            {
                outFile.Position = outFile.Length;

                var bytes = Encoding.Unicode.GetBytes(Math.Round(Accuracy,2) + Environment.NewLine);
                outFile.Write(bytes, 0, bytes.Length);
            }

        }
    }
}
