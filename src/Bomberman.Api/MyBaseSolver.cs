using System.Collections.Generic;
using System.Linq;

namespace Bomberman.Api
{
    public abstract class MyBaseSolver : AbstractSolver
    {
        protected Board PrevBoard { get; private set; }
        protected Board Board { get; private set; }

        protected BomberState Me { get; private set; }
        protected List<BomberState> Emenies { get; } = new List<BomberState>();
        protected List<MeatchoperState> Meatchopers { get; } = new List<MeatchoperState>();
        protected Bombs Bombs { get; } = new Bombs();
        protected List<PerkState> Perks { get; } = new List<PerkState>();

        protected MyBaseSolver(string serverUrl) : base(serverUrl)
        {
        }

        protected abstract IEnumerable<Move> GetMoves();

        protected internal override string Get(Board gameBoard)
        {
            PrevBoard = Board;
            Board = gameBoard;

            UpdateBombStates();

            return string.Join(",", GetMoves().Select(m => m.ToString()));
        }

        private void UpdateBombStates()
        {
            foreach (var oldKnownBomb in Bombs.All)
            {
                if (!Board.CanBombBeAt(oldKnownBomb.Location))
                {
                    Bombs.RemoveAt(oldKnownBomb.Location);
                }
                else
                {
                    oldKnownBomb.ProcessTurn(Board.GetAt(oldKnownBomb.Location));
                }
            }

            foreach (var l in Board.Locations)
            {
                if (Board.IsBombAt(l) && !PrevBoard.CanBombBeAt(l))
                {
                    //TODO: Find who placed it and add to list
                }
            }
        }
        
    }
}
