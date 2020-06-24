using System;

namespace Bomberman.Api
{
    public class BomberState
    {
        public const int INF_TIME = 999;

        public Point Location { get; set; }

        public Move LastDirection { get; private set; }

        public int KeepDirectionTurns { get; private set; }

        public bool IsLongStanding => KeepDirectionTurns >= Config.LongStandingTurnsThreshold;
        
        public int BombRadius { get; set; }
        public int BombRadiusExpirationTime { get; set; } = INF_TIME;
        public int BombCount { get; set; }
        public int BombCountPerksExpirationTime { get; set; } = INF_TIME;
        public bool HasBombImmunePerk { get; set; }
        public int BombImmunePerkExpirationTime { get; set; } = 0;
        public int WhenImmuneExpires(int currentTime) => Math.Max(0, BombImmunePerkExpirationTime - currentTime);
        public int DetonatorsCount { get; set; }

        public BomberState(Point initialLocation)
        {
            Location = initialLocation;

            LastDirection = Move.Stop;
            KeepDirectionTurns = 1;

            BombRadius = Settings.BombRadiusDefault;
            BombCount = Settings.BombsCountDefault;
            DetonatorsCount = 0;
            HasBombImmunePerk = false;
        }

        public void ProcessNewLocation(Point newLocation, Element newLocationPerk, int currentTime)
        {
            var direction = Location.GetShiftDirectionTo(newLocation);
            if (direction == LastDirection)
            {
                KeepDirectionTurns++;
            }
            else
            {
                KeepDirectionTurns = 1;
            }

            LastDirection = direction;
            Location = newLocation;

            TakePerks(newLocationPerk, currentTime);
            ExpirePerks(currentTime);
        }

        public void ProcessBombPlaced(Point bombLocation, int currentTime)
        {
            //if (AvailableRemoteBombsCount > 0)
            //{
            //    AvailableRemoteBombsCount--;
            //}
            //else
            //{
            //    if (AvailableTimerBombsCount > 0)
            //    {
            //        AvailableTimerBombsCount--;
            //    }
            //}
        }

        public void ProcessTimerBombDetonated(Point bombLocation)
        {
            //AvailableTimerBombsCount++;
        }

        public void ProcessRemoteBombDetonated(Point bombLocation)
        {
            //AvailableRemoteBombsCount++;
        }

        private void ExpirePerks(int currentTime)
        {
            if (currentTime > BombRadiusExpirationTime)
            {
                BombRadius = Settings.BombRadiusDefault;
                BombRadiusExpirationTime = INF_TIME;
            }

            if (currentTime > BombCountPerksExpirationTime)
            {
                BombCount = Settings.BombsCountDefault;
                BombCountPerksExpirationTime = INF_TIME;
            }

            if (currentTime > BombImmunePerkExpirationTime)
            {
                HasBombImmunePerk = false;
            }
        }

        private void TakePerks(Element newLocationElement, int currentTime)
        {
            switch (newLocationElement)
            {
                case Element.BOMB_BLAST_RADIUS_INCREASE:
                    BombRadius += Settings.BombRadiusPerkEffect;
                    BombRadiusExpirationTime =
                        (BombRadiusExpirationTime == INF_TIME ? currentTime : BombRadiusExpirationTime) +
                        Settings.BombRadiusPerkDuration;
                    break;
                case Element.BOMB_COUNT_INCREASE:
                    BombCount = Settings.BombsCountDefault + Settings.BombCountPerkEffect;
                    BombCountPerksExpirationTime = currentTime + Settings.BombCountPerkDuration;
                    break;
                case Element.BOMB_IMMUNE:
                    HasBombImmunePerk = true;
                    BombImmunePerkExpirationTime = currentTime + Settings.BombImmunePerkDuration;
                    break;
                case Element.BOMB_REMOTE_CONTROL:
                    DetonatorsCount = Settings.BombRemoteControlDetonatorsCount;
                    break;
            }
        }

        public override string ToString()
        {
            //return $"Loc at {Location} {LastDirection}; Available Bombs: T={AvailableTimerBombsCount}, RC={AvailableRemoteBombsCount}; Perks: BombCount={BombCount}|{BombCountPerksExpirationTime}, BlastRadius={BombRadius}|{BombRadiusExpirationTime}, RC={DetonatorsCount}|{BombRemoteControlPerksExpirationTime}, Immune={HasBombImmunePerk}|{BombImmunePerkExpirationTime}";
            return $"Loc at {Location} {LastDirection}; BombCount={BombCount}|{BombCountPerksExpirationTime}, BlastRadius={BombRadius}|{BombRadiusExpirationTime}, RC={DetonatorsCount}, Immune={HasBombImmunePerk}|{BombImmunePerkExpirationTime}";
        }
    }
}