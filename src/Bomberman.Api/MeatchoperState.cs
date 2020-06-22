namespace Bomberman.Api
{
    public class MeatchoperState
    {
        public Point Location { get; private set; }
        public Move LastDirection { get; private set; }

        public MeatchoperState(Point location)
        {
            Location = location;
            LastDirection = Move.Stop;
        }

        public void SetNewPosition(Point newLocation)
        {
            LastDirection = Location.GetShiftDirectionTo(newLocation);
            Location = newLocation;
        }

        public override string ToString()
        {
            return $"{Location} {LastDirection.ToString()}";
        }
    }
}