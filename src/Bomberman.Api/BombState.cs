using System;
using System.Collections.Generic;
using System.Linq;

namespace Bomberman.Api
{
    public class BombState
    {
        public Point Location { get; }
        public int CreationTime { get; private set; }
        public int Radius { get; private set; }
        public int? Timer { get; private set; }
        public int? BlastTurn => CreationTime + Timer;
        public bool IsRemoteControlled => !Timer.HasValue;
        public BomberState WhoPlaced { get; private set; }

        public BombState(Point location, Element state, BomberState whoPlaced, int settlementTime)
        {
            WhoPlaced = whoPlaced;
            Location = location;
            CreationTime = settlementTime;
            Radius = whoPlaced.BombRadius;

            var stateTimer = GetTimerFromState(state);
            Timer = stateTimer < Settings.BombTimer ? stateTimer :
                whoPlaced.DetonatorsCount > 0 ? (int?) null : Settings.BombTimer;
            
            whoPlaced.ProcessBombPlaced(location, settlementTime);
        }

        public BombState Clone(BomberState whoPlacedCloned)
        {
            return new BombState(Location, Element.BOMB_TIMER_5, whoPlacedCloned, CreationTime)
            {
                Radius = Radius,
                Timer = Timer
            };
        }

        public Element GetElement(bool actCmdSent, BomberState me)
        {
            if (IsRemoteControlled && actCmdSent && WhoPlaced == me)
            {
                return Element.BOOM;
            }

            if (IsRemoteControlled)
            {
                return Element.BOMB_TIMER_5;
            }

            return GetBombElementByTimer(Timer);
        }

        public static Element GetBombElementByTimer(int? timer)
        {
            if (!timer.HasValue)
            {
                return Element.BOMB_TIMER_5;
            }

            switch (timer.Value)
            {
                case 5:
                    return Element.BOMB_TIMER_5;
                case 4:
                    return Element.BOMB_TIMER_4;
                case 3:
                    return Element.BOMB_TIMER_3;
                case 2:
                    return Element.BOMB_TIMER_2;
                case 1:
                    return Element.BOMB_TIMER_1;
                case 0:
                    return Element.BOOM;
            }

            throw new NotImplementedException();
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
                WhoPlaced.ProcessRemoteBombDetonated(Location);
            }
            else
            {
                WhoPlaced.ProcessTimerBombDetonated(Location);
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

            return GameState.CalcProbabilityAnyOf(Enumerable.Range(currentTime - CreationTime, t).Select(Config.RemoteBombEnemyDetonationChanceAtTurn));
        }

        public override string ToString()
        {
            return $"{Location} {(Timer.HasValue ? "T" + Timer : "RC")} *{Radius}";
        }
    }
}
