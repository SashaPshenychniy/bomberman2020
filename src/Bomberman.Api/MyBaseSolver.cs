using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace Bomberman.Api
{
    public abstract class MyBaseSolver : AbstractSolver
    {
        protected MyBaseSolver(string serverUrl) : base(serverUrl)
        {
        }

        protected abstract IEnumerable<Move> GetMoves(Board gameBoard);

        protected internal override string Get(Board gameBoard)
        {
#if !DEBUG
            try
            {
#endif
                return string.Join(",", GetMoves(gameBoard).Select(m => m.ToString()));
#if !DEBUG
            }
            catch (Exception e)
            {
                Log.Error("Unhandled exception", e);
//#if DEBUG
                throw;
//#else
                return "Stop";
            }
#endif
        }
    }
}