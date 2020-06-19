using System.Collections.Generic;
using System.Linq;

namespace Bomberman.Api
{
    public class Bombs
    {
        public List<BombState> My { get; } = new List<BombState>();
        public List<BombState> Other { get; } = new List<BombState>();
        public IEnumerable<BombState> All => My.Concat(Other).ToArray();

        public void TrackMy(BombState b)
        {
            My.Add(b);
        }

        public void TrackOther(BombState b)
        {
            Other.Add(b);
        }

        public void RemoveAt(Point location)
        {
            My.RemoveAll(b => b.Location == location);
            Other.RemoveAll(b => b.Location == location);
        }
    }
}