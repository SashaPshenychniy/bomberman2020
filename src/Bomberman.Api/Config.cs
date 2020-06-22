using System;

namespace Bomberman.Api
{
    public class Config
    {
        public const double PerkDiscoveringAvgValue = 2.0;
        public const double BombCountFutureExpectedValue = Settings.DestroyWallAward * Settings.BombCountPerkEffect;
        public const double BombRadiusFutureExpectedValue = 6.0;
        public const double BombRemoteControlFutureExpectedValue = Settings.BombRemoteControlDetonatorsCount * 1.5;
        public const double BombImmuneFutureExpectedValue = 5.0;

        public const double DeathExpectedValueLoss = -Settings.DeathPenalty + SkipTurnExpectedLoss * 5;
        public const double PerkDestroyFutureExpectedValueLoss = DeathExpectedValueLoss * ZombieAppearingFromPerkDestroyChance * DeathFromZombieChance;
        public const double ZombieAppearingFromPerkDestroyChance = 1.0;
        private const double DeathFromZombieChance = 0.7; // TODO: Reduce when implement fight with zombie

        public const double SkipTurnExpectedLoss = 0.5;
        public const double LongStandingTurnsThreshold = 3;

        public const int PredictMovesCount =
#if DEBUG
        5;
#else
        20;
#endif

        public const double Eplison = 1e-6;

        public static MoveProbability ChopperMoveProbability = new MoveProbability
        {
            KeepDirection = 0.9,
            Stop = 0,
            Reverse = 0.03,
            TurnLeft = 0.04,
            TurnRight = 0.04
        };

        public static MoveProbability EnemyMoveProbability = new MoveProbability
        {
            KeepDirection = 0.35,
            Stop = 0.05,
            Reverse = 0.1,
            TurnLeft = 0.25,
            TurnRight = 0.25
        };

        public static double RemoteBombEnemyDetonationChanceAtTurn(int t)
        {
            if (t <= 0)
            {
                return 0;
            }

            if (t == 1)
            {
                return 0.2;
            }

            if (t == 2)
            {
                return 0.8;
            }

            if (t >= 3 && t <= 5)
            {
                return 0.9;
            }

            return 0.9 * Math.Pow(0.8, t - 5);
        }
    }
}