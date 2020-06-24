using System.Collections.Generic;
using System.Linq;

namespace Bomberman.Api
{
    public class BombState
    {
        public Point Location { get; }
        public int CreationTime { get; }
        public int Radius { get; }
        public int? Timer { get; private set; }
        public int? BlastTurn => CreationTime + Timer;
        public bool IsRemoteControlled => !Timer.HasValue;

        private readonly BomberState _whoPlaced;

        public BombState(Point location, Element state, BomberState whoPlaced, int currentTime)
        {
            _whoPlaced = whoPlaced;
            Location = location;
            CreationTime = currentTime;
            Radius = whoPlaced.BombRadius;

            var stateTimer = GetTimerFromState(state);
            Timer = stateTimer < Settings.BombTimer ? stateTimer :
                whoPlaced.DetonatorsCount > 0 ? (int?) null : Settings.BombTimer;
            
            whoPlaced.ProcessBombPlaced(location, currentTime);
        }

        public void ProcessTurn(Element newState)
        {
            var newTimer = GetTimerFromState(newState);
            if (Timer.HasValue)
            {
                if (newTimer.HasValue)
                {
                    if (newTimer >= Timer)
                    {
                        Timer = null;
                    }
                    else
                    {
                        Timer = newTimer;
                    }
                }
                else
                {
                    Timer = Timer - 1;
                }
            }
            else
            {
                if (newTimer != Settings.BombTimer)
                {
                    Timer = newTimer;
                }
            }
        }

        public void NotifyDetonated()
        {
            if (IsRemoteControlled)
            {
                _whoPlaced.ProcessRemoteBombDetonated(Location);
            }
            else
            {
                _whoPlaced.ProcessTimerBombDetonated(Location);
            }
        }

        private int? GetTimerFromState(Element newState)
        {
            switch (newState)
            {
                case Element.BOMB_TIMER_5:
                    return 5;
                case Element.BOMB_TIMER_4:
                    return 4;
                case Element.BOMB_TIMER_3:
                    return 3;
                case Element.BOMB_TIMER_2:
                    return 2;
                case Element.BOMB_TIMER_1:
                    return 1;
                default: 
                    return null;
            }
        }

        public double ProbabilityExplodesBefore(int t, int currentTime)
        {
            if (!IsRemoteControlled)
            {
                if (t > Timer)
                {
                    return 1;
                }

                return 0;
            }

            return MySolver.CalcProbabilityAnyOf(Enumerable.Range(currentTime - CreationTime, t).Select(Config.RemoteBombEnemyDetonationChanceAtTurn));
        }

        public override string ToString()
        {
            return $"{Location} {(Timer.HasValue ? "T" + Timer : "RC")} *{Radius}";
        }
    }
}
