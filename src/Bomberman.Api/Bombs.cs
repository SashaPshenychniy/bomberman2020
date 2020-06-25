using System.Collections.Generic;
using System.Linq;

namespace Bomberman.Api
{
    public class Bombs
    {
        public List<BombState> My { get; private set; } = new List<BombState>();
        public List<BombState> Enemy { get; private set; } = new List<BombState>();
        public IEnumerable<BombState> All => My.Concat(Enemy).ToArray();

        public Bombs Clone(BomberState meCloned, List<BomberState> enemiesCloned)
        {
            return new Bombs
            {
                My = My.Select(b => b.Clone(meCloned)).ToList(),
                Enemy = Enemy.Select(b => b.Clone(enemiesCloned.Single(e=>e.Location == b.WhoPlaced.Location))).ToList()
            };
        }

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