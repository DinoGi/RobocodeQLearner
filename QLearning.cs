using System;
using System.Collections.Generic;

namespace FDLearnAim
{
    public class QLearning
    {
        public Dictionary<Segmentation, double[]> Scores { get; set; }

        /// <summary>
        /// Used if scores represent ratios of favorableActions / totalActions 
        /// </summary>
        private readonly Dictionary<Segmentation, long[]> _favorableActionCount;

        public float DiscountFactor { get; set; }
        public bool UseSoftmaxSelection { get; set; }
        public readonly int NrStates;

        /// <summary>
        /// Used when selecting state when using Fermi distribution
        /// </summary>
        public float Temperature { get; set; }
        public float MinTemperature { get; set; }
        public float TemperatureDecraseAmount { get; set; }

        private readonly Random _randomGenQLearningSelect;
        private readonly Random _randomStateGen;
        private double _minScore;

        public QLearning(Segmentation segmentsToUse, int nrStates, double minScore)
        {
            _randomStateGen = new Random();
            _randomGenQLearningSelect = new Random();
            DiscountFactor = 0.98f;
            NrStates = nrStates;

            var segments = GetApplicableSegments(segmentsToUse);

            _favorableActionCount = new Dictionary<Segmentation, long[]>();
            Scores = new Dictionary<Segmentation, double[]>();
            foreach (var segment in segments)
            {
                Scores.Add(segment, new double[nrStates]);
                _favorableActionCount.Add(segment, new long[nrStates]);
            }
            
            _minScore = minScore;

            Temperature = 10f;
            MinTemperature = 0.01f;
        }

        /// <summary>
        /// Selects a state based on the scores.
        /// </summary>
        /// <param name="applicableSegments"></param>
        /// <returns> A state ranging from 0 to Scores.Lenght</returns>
        public int SelectQLearningState(Segmentation applicableSegments)
        {
            if (UseSoftmaxSelection)
            {
                var scores = AverageApplicableScores(applicableSegments);
                return SoftmaxSelection(scores, Temperature);
            }
            else
            {
                var scores = AverageApplicableScores(applicableSegments);
                return SelectBySumProb(scores);
            }
        }

        /// <summary>
        /// Selects state using the sum of all probabilities. 
        /// This has a potential problem: if scores are [.25 | .25 | 1 | .25 | .25]
        /// The odds of selecting the middle states are .5, even though it has a much higher score than other states.
        /// This problem is made worse the more states there are. This is not a problem when using the Boltzmann distribution
        /// </summary>
        /// <param name="scores"></param>
        /// <returns></returns>
        private int SelectBySumProb(double[] scores)
        {
            //For now we attribute probabilities to each state based on their scores
            var prbToSelect = _randomGenQLearningSelect.NextDouble();

            var maxScore = SumScores(scores);

            var prbSum = 0.0d;

            for (var i = 0; i < scores.Length; i++)
            {
                //Keep adding the probability for this state until it reaches the prbToSelect
                prbSum += scores[i] / maxScore;

                if (prbToSelect < prbSum)
                {
                    return i;
                }
            }
            //Something went wrong, return random state
            return (int)Utilities.GetRandomNumber(_randomStateGen, 0, scores.Length - 1);
        }

        private static double SumScores(double[] scores)
        {
            var sum = 0d;
            for (var i = 0; i < scores.Length; i++)
            {
                sum += scores[i];
            }
            return sum;
        }

        /// <summary>
        /// Uses Boltzmann distribution to select state. 
        /// </summary>
        /// <param name="initialScores"></param>
        /// <param name="temperature"> 
        /// T = 0: if value less than 0 returns 0, if value bigger than 0 return 1;
        /// T > 0: probability is reduced i.e. it needs a bigger value to produce a bigger probability
        /// </param>
        /// <returns></returns>
        private int SoftmaxSelection(double[] initialScores, double temperature)
        {
            var boltzmannScores = new double[initialScores.Length];

            for (var i = 0; i < boltzmannScores.Length; i++)
            {
                boltzmannScores[i] = BoltzmanDistribution(initialScores[i], temperature);
            }

            return SelectBySumProb(boltzmannScores);
        }

        
        public void DecreaseTemperature()
        {
            Temperature -= TemperatureDecraseAmount;

            if (Temperature < MinTemperature)
            {
                Temperature = MinTemperature;
            }
        }
        
        #region Segmentation code

        private List<Segmentation> GetApplicableSegments(Segmentation applicableSegments)
        {
            var segments = Enum.GetValues(typeof(Segmentation));

            var retList = new List<Segmentation>();

            foreach (var segment in segments)
            {
                if (segment.GetType() != typeof(Segmentation)) 
                    continue;

                if ((applicableSegments & (Segmentation)segment) != 0)
                {
                    retList.Add((Segmentation)segment);
                }
            }

            return retList;
        }

        /// <summary>
        /// Returns a tuple with the favorableActionCount and Ratio for every applicable segment.
        /// Useful when scores are a ratio of favorableActions / TotalActions
        /// </summary>
        /// <param name="applicableSegments"></param>
        /// <returns>List of tuples where Item1 -> nrFavorableActions & Item2 -> Ratio </returns>
        private List<FavorableActionInfo> GetApplicableFavorableActionCountAndRatio(Segmentation applicableSegments)
        {
            var applicableFavorableActionCount = new List<FavorableActionInfo>();
            var segments = GetApplicableSegments(applicableSegments);

            //Get scores for each applicable segment
            foreach (var segment in segments)
            {
                if (!_favorableActionCount.ContainsKey(segment) || !Scores.ContainsKey(segment))
                    continue;

                applicableFavorableActionCount.Add(new FavorableActionInfo(
                    _favorableActionCount[segment], Scores[segment]));
            }

            return applicableFavorableActionCount;
        }

        private List<double[]> GetApplicableScores(Segmentation applicableSegments)
        {
            var applicableScores = new List<double[]>();
            var segments = GetApplicableSegments(applicableSegments);

            //Get scores for each applicable segment
            foreach (var segment in segments)
            {
                if (!Scores.ContainsKey(segment))
                    continue;

                applicableScores.Add(Scores[segment]);
            }

            return applicableScores;
        }

        private double[] AverageApplicableScores(Segmentation applicableSegments)
        {
            var applicableScores = GetApplicableScores(applicableSegments);

            if (applicableScores.Count == 0)
                return new double[NrStates];

            //Sum all scores
            var sumScores = new double[NrStates];
            foreach (var score in applicableScores)
            {
                for (var i = 0; i < NrStates; i++)
                {
                    sumScores[i] += score[i];
                }
            }

            //Average scores
            var avgScores = new double[NrStates];
            for (var i = 0; i < NrStates; i++)
            {
                avgScores[i] = sumScores[i] / applicableScores.Count;
            }

            return avgScores;
        }

        #endregion
        
        #region ratio methods

        /// <summary>
        /// Resets all favorableActionCounts to a certain value (usually either 1 or 0). 
        /// This is useful when scores represent ratios of favorableActions / totalActions
        /// </summary>
        /// <param name="value"></param>
        public void ResetFavorableActions(long value)
        {
            foreach (var favorableAction in _favorableActionCount.Values)
            {
                for (var i = 0; i < favorableAction.Length; i++)
                {
                    favorableAction[i] = value;
                }
            }
        }

        /// <summary>
        /// Assuming scores are a ratio of (favorableActions / totalActions), increases the nr of favorableActions by one
        /// </summary>
        /// <param name="applicableSegments"></param>
        /// <param name="state"></param>
        public void IncreaseRatio(Segmentation applicableSegments, int state)
        {
            var ratioDetails = GetApplicableFavorableActionCountAndRatio(applicableSegments);

            foreach (var ratioDetail in ratioDetails)
            {
                var favorableActions = ratioDetail.FavorableActionCounts;
                var nrTotalActions = GetNrOfTotalActions(favorableActions[state], ratioDetail.Ratios[state]);

                ratioDetail.Ratios[state] = ++favorableActions[state] / (float)++nrTotalActions;
            }
        }

        /// <summary>
        /// Assuming scores are a ratio of (favorableActions / totalActions), increases the nr of totalActions by one
        /// </summary>
        /// <param name="applicableSegments"></param>
        /// <param name="state"></param>
        public void DecreaseRatio(Segmentation applicableSegments, int state)
        {
            var ratioDetails = GetApplicableFavorableActionCountAndRatio(applicableSegments);

            foreach (var ratioDetail in ratioDetails)
            {
                var favorableActions = ratioDetail.FavorableActionCounts;
                var nrTotalActions = GetNrOfTotalActions(favorableActions[state], ratioDetail.Ratios[state]);

                ratioDetail.Ratios[state] = favorableActions[state] / (float)++nrTotalActions;
            }
        }

        /// <summary>
        /// Gets the number of total actions given the number of favorableActions and its ratio of favorable / total actions
        /// </summary>
        /// <param name="favorableActions"></param>
        /// <param name="ratio"> ratio of favorableActions / totalActions for a specific state</param>
        private static long GetNrOfTotalActions(long favorableActions, double ratio)
        {
            if (ratio == 0.0d)
            {
                return 0;
            }
            return (long)Math.Round(favorableActions / ratio);
        }
        
        #endregion

        #region Score updating region

        /// <summary>
        /// Increases the given state score the same amount for every applicable segment
        /// </summary>
        /// <param name="applicableSegments"></param>
        /// <param name="state"></param>
        /// <param name="amount"></param>
        public void IncreaseScore(Segmentation applicableSegments, int state, double amount)
        {
            ApplyDiscountFactor();

            var applicableScores = GetApplicableScores(applicableSegments);

            foreach (var applicableScore in applicableScores)
            {
                applicableScore[state] += amount;

                //Q-Learning does not fair well with negative values
                //Ensure there is always some value to allow for exploration
                if (applicableScore[state] < _minScore)
                {
                    applicableScore[state] = _minScore;
                }
            }
        }

        /// <summary>
        /// Updates the given state score the same amount for every applicable segment
        /// </summary>
        /// <param name="state"></param>
        /// <param name="score"></param>
        public void UpdateScore(Segmentation applicableSegments, int state, double score)
        {
            ApplyDiscountFactor();

            var applicableScores = GetApplicableScores(applicableSegments);

            foreach (var applicableScore in applicableScores)
            {

                applicableScore[state] = score;

                //Q-Learning does not fair well with negative values
                //Ensure there is always some value to allow for exploration
                if (applicableScore[state] < _minScore)
                {
                    applicableScore[state] = _minScore;
                }
            }
        }

        /// <summary>
        /// Apply a discount factor to the learning scores so the agent prefers more recent data
        /// </summary>
        public void ApplyDiscountFactor()
        {
            foreach (var score in Scores.Values)
            {
                for (var i = 0; i < score.Length; i++)
                {
                    score[i] *= DiscountFactor;
                }
            }
        }

        /// <summary>
        /// Apply a discount factor to the learning scores so the agent prefers more recent data.
        /// Only updates values in teh applicableSegments
        /// </summary>
        public void ApplyDiscountFactor(Segmentation aplicableSegments)
        {
            var applicableScores = GetApplicableScores(aplicableSegments);

            foreach (var score in applicableScores)
            {
                for (var i = 0; i < score.Length; i++)
                {
                    score[i] *= DiscountFactor;
                }
            }
        }

        public void IncreaseAllLearningScores(double amount)
        {
            foreach (var score in Scores.Values)
            {
                for (int i = 0; i < score.Length; i++)
                {
                    score[i] += amount;
                }
            }
        }

        public void UpdateAllLearningScores(double value)
        {
            foreach (var score in Scores.Values)
            {
                for (int i = 0; i < score.Length; i++)
                {
                    score[i] = value;
                }
            }
        }

        #endregion

        /// <summary>
        /// Return the probability of a certain value using Boltzmann distribution
        /// </summary>
        /// <param name="value"></param>
        /// <param name="temperature">
        /// T = 0: if value less than 0 returns 0, if value bigger than 0 return 1;
        /// T > 0: probability is reduced i.e. it needs a bigger value to produce a bigger probability
        /// </param>
        /// <returns></returns>
        public static double BoltzmanDistribution(double value, double temperature)
        {
            if (temperature - 0.0d < double.Epsilon)
            {
                return value;
            }
            return Math.Exp(value / temperature);
        }

        public override string ToString()
        {
            var str = string.Empty;

            foreach (var score in Scores)
            {
                str = str + "Segment " + score.Key.ToString() + ": ";
                str += System.Environment.NewLine;

                for (var i = 0; i < score.Value.Length; i++)
                {
                    if (UseSoftmaxSelection)
                    {
                        str += Math.Round(score.Value[i], 2) + " | ";
                    }
                    else
                    {
                        str += Math.Round(score.Value[i]) + " | ";
                    }
                }

                str += System.Environment.NewLine;
            }
            

            return str;
        }
    }

    public class FavorableActionInfo
    {
        public long[] FavorableActionCounts { get; set; }
        public double[] Ratios { get; set; }

        public FavorableActionInfo(long[] counts, double[] ratios)
        {
            FavorableActionCounts = counts;
            Ratios = ratios;
        }
    }

    [Flags]
    public enum Segmentation
    {
        None = 1,
        //Distance
        DistanceClose = 2,
        DistanceFar = 4,
        //Velocity
        VelocityFast = 8,
        VelocitySlow = 16
    }
}
