using System;
using System.Collections.Generic;

namespace Bomberman.Api
{
    public class MySolver : MyBaseSolver
    {
        private Random _rand = new Random();

        public MySolver(string serverUrl) : base(serverUrl)
        {
        }

        protected override IEnumerable<Move> GetMoves()
        {
            var myPosition = Board.GetBomberman();
            var direction = (Move) _rand.Next(5);
            var newPosition = myPosition.Shift(direction);
            if (Board.IsBarrierAt(newPosition))
            {
                yield return Move.Stop;
            }
            else yield return direction;
        }
    }
}
