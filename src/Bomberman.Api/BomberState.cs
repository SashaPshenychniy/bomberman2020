namespace Bomberman.Api
{
    public class BomberState
    {
        public const int INF_TIME = 1000;

        public Point Location { get; set; }

        public int BombRadius { get; set; }
        public int BombRadiusExpirationTime { get; set; } = INF_TIME;
        public int BombCount { get; set; }
        public int BombCountPerksExpirationTime { get; set; } = INF_TIME;
        public bool HasBombImmunePerk { get; set; }
        public int BombImmunePerkExpirationTime { get; set; } = INF_TIME;
        public int RemoteControlBombsCount { get; set; }
        public int BombRemoteControlPerksExpirationTime { get; set; } = INF_TIME;

        public void ProcessNewLocation(Point newLocation, Element newLocationElement, int currentTime)
        {
            Location = newLocation;

            TakePerks(newLocationElement, currentTime);
            ExpirePerks(currentTime);
        }

        private void ExpirePerks(int currentTime)
        {
            if (currentTime >= BombRadiusExpirationTime)
            {
                BombRadius -= Settings.BombRadiusPerkEffect;
                BombRadiusExpirationTime = INF_TIME;
            }

            if (currentTime >= BombCountPerksExpirationTime)
            {
                BombCount -= Settings.BombCountPerkEffect;
                BombCountPerksExpirationTime = INF_TIME;
            }

            if (currentTime >= BombImmunePerkExpirationTime)
            {
                HasBombImmunePerk = false;
                BombImmunePerkExpirationTime = INF_TIME;
            }

            if (currentTime >= BombRemoteControlPerksExpirationTime)
            {
                RemoteControlBombsCount -= Settings.BombRemoteControlPerkEffect;
                BombRemoteControlPerksExpirationTime = INF_TIME;
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
                    BombCount += Settings.BombCountPerkEffect;
                    BombCountPerksExpirationTime =
                        (BombCountPerksExpirationTime == INF_TIME ? currentTime : BombCountPerksExpirationTime) +
                        Settings.BombCountPerkDuration;
                    break;
                case Element.BOMB_IMMUNE:
                    HasBombImmunePerk = true;
                    BombImmunePerkExpirationTime =
                        (BombImmunePerkExpirationTime == INF_TIME ? currentTime : BombImmunePerkExpirationTime) +
                        Settings.BombImmunePerkDuration;
                    break;
                case Element.BOMB_REMOTE_CONTROL:
                    RemoteControlBombsCount += Settings.BombRemoteControlPerkEffect;
                    BombRemoteControlPerksExpirationTime =
                        (BombRemoteControlPerksExpirationTime == INF_TIME
                            ? currentTime
                            : BombRemoteControlPerksExpirationTime) + Settings.BombRemoteControlPerkDuration;
                    break;
            }
        }
    }
}
