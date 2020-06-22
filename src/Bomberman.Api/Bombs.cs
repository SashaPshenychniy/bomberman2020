using System.Collections.Generic;
using System.Linq;

namespace Bomberman.Api
{
    public class Bombs
    {
        public List<BombState> My { get; } = new List<BombState>();
        public List<BombState> Enemy { get; } = new List<BombState>();
        public IEnumerable<BombState> All => My.Concat(Enemy).ToArray();

        public void TrackMy(BombState b)
        {
            My.Add(b);
        }

        public void TrackOther(BombState b)
        {
            Enemy.Add(b);
        }

        public void RemoveAt(Point location)
        {
            My.RemoveAll(b => b.Location == location);
            Enemy.RemoveAll(b => b.Location == location);
        }

        public void Clear()
        {
            My.Clear();
            Enemy.Clear();
        }
    }
}