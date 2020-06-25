namespace Bomberman.Api
{
    public class PerkState
    {
        public Point Location { get; }
        public Element PerkType { get; }
        public int ExpiresIn { get; private set; }
        public bool IsExpired => ExpiresIn <= 0;

        public PerkState(Point location, Element perk)
        {
            Location = location;
            PerkType = perk;
            ExpiresIn = Settings.PerkExpirationTime;
        }

        public PerkState Clone()
        {
            return new PerkState(Location, PerkType)
            {
                ExpiresIn = ExpiresIn
            };
        }

        public void ProcessTurn()
        {
            ExpiresIn--;
        }

        public override string ToString()
        {
            return $"{Location} {(char) PerkType} exp={ExpiresIn}";
        }
    }
}
