namespace Bomberman.Api
{
    public class PerkState
    {
        public Point Location { get; }
        public Element Perk { get; }
        public int ExpiresIn { get; private set; }
        public bool IsExpired => ExpiresIn <= 0;

        public PerkState(Point location, Element perk)
        {
            Location = location;
            Perk = perk;
            ExpiresIn = Settings.PerkExpirationTime;
        }

        public void ProcessTurn()
        {
            ExpiresIn--;
        }
    }
}
