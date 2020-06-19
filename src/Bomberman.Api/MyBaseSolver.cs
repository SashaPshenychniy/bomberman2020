using System.Collections.Generic;
using System.Linq;

namespace Bomberman.Api
{
    public abstract class MyBaseSolver : AbstractSolver
    {
        protected Board PrevBoard { get; private set; }
        protected Board Board { get; private set; }

        protected MyBaseSolver(string serverUrl) : base(serverUrl)
        {
        }

        protected internal override string Get(Board gameBoard)
        {
            PrevBoard = Board;
            Board = gameBoard;

            return string.Join(",", GetMoves().Select(m => m.ToString()));
        }

        protected abstract IEnumerable<Move> GetMoves();
    }
}
