using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace Bomberman.Api
{
    public class MySolver : MyBaseSolver
    {
        public bool WaitingInput;
        public ConsoleKey LastKey;

        private static GameState currentSituation = new GameState();
        private Stopwatch _stopwatch = new Stopwatch();

        private List<Move[]> PossibleMoves = new List<Move[]>
        {
            new[] {Move.Stop},
            new[] {Move.Up},
            new[] {Move.Left},
            new[] {Move.Right},
            new[] {Move.Down},
            new[] {Move.Act, Move.Up},
            new[] {Move.Act, Move.Left},
            new[] {Move.Act, Move.Right},
            new[] {Move.Act, Move.Down},
            new[] {Move.Up, Move.Act},
            new[] {Move.Left, Move.Act},
            new[] {Move.Right, Move.Act},
            new[] {Move.Down, Move.Act}
        };

        public MySolver(string serverUrl) : base(serverUrl)
        {
        }

        protected override IEnumerable<Move> GetMoves(Board gameBoard)
        {
            //WaitingInput = true;
            //LastKey = ConsoleKey.B;
            ////var key = Console.ReadKey(true);
            //while (LastKey == ConsoleKey.B)
            //{
            //    Thread.Sleep(1);
            //}
            //WaitingInput = false;
            //switch (LastKey)
            //{
            //    case ConsoleKey.UpArrow:
            //        return new[] { Move.Up };
            //    case ConsoleKey.DownArrow:
            //        return new[] { Move.Down };
            //    case ConsoleKey.LeftArrow:
            //        return new[] { Move.Left };
            //    case ConsoleKey.RightArrow:
            //        return new[] { Move.Right };
            //    case ConsoleKey.Spacebar:
            //        return new[] { Move.Act };
            //    default:
            //        return new[] { Move.Stop };
            //}

            _stopwatch.Restart();

            currentSituation.ApplyNextTurnBoardState(gameBoard);

            Log.Info($"Precalc time: {_stopwatch.ElapsedMilliseconds:F0}ms");
            _stopwatch.Restart();

            var moves = FindBestMove();

            Log.Info($"Move search time: {_stopwatch.ElapsedMilliseconds:F0}ms");

            return moves;
        }

        private Move[] FindBestMove()
        {
            return new[] { Move.Act };

            if (currentSituation.Board.IsMyBombermanDead)
            {
                return new[] {Move.Stop};
            }

            Move[] best = null;

            double bestValue = 2 * GameState.PathValueNegativeInf;
            foreach (var move in PossibleMoves)
            {
                var evalRes = EvaluateMove(move);
                if (evalRes > bestValue)
                {
                    bestValue = evalRes;
                    best = move;
                }
            }

            if (best == null)
            {
                return new[]{Move.Stop};
            }

            Log.Debug($"Best move among alternatives: {string.Join(", ", best)} with value={bestValue}");
            
            return best;
        }

        private double EvaluateMove(Move[] move)
        {
            Log.Debug($"....................................... BEGIN EVALUATING MOVE: {string.Join(", ", move)} .......................................");

            double moveScore;
            
            if (currentSituation.IsMoveAllowed(move))
            {
                var gs = currentSituation.Clone();
                var immediateMoveValue = gs.ApplyMove(move);

                Log.Debug($"Real board position:\n{gs.Board.ToString()}");
                Log.Debug($"Move: {string.Join(", ", move)}");
                Log.Debug($"Provisioned board position:\n{gs.Board.ToString()}");
                Log.Debug($"Move immediate value: {immediateMoveValue}");

                var orderedPaths = gs.GetOrderedEvaluatedPaths().ToArray();
                var bestMove = orderedPaths.FirstOrDefault();
                var bestLongMove = gs.GetOrderedEvaluatedPaths().FirstOrDefault(p => p.Length > Settings.BombTimer);

                Log.Debug($"Best move of length>{Settings.BombTimer} in situation is: {(bestLongMove == null ? "NULL": bestLongMove.ToString())}");

                if (bestLongMove == null) // It's likely we are locked somewhere
                {
                    moveScore = GameState.PathValueNegativeInf;
                }
                else
                {
                    moveScore = immediateMoveValue + bestMove.Score;
                    Log.Debug($"Total move alternative score: {moveScore}");
                }
            }
            else
            {
                moveScore = GameState.PathValueNegativeInf*2;
            }

            Log.Debug($"....................................... END EVALUATING MOVE: {string.Join(", ", move)}. SCORE: {moveScore} ......................");

            return moveScore;
        }

        
        //public BombState CloneBombStateForNextTurn(BombState bomb, bool isMy, bool isBombActionInMove)
        //{
        //    if (isMy && isBombActionInMove && bomb.IsRemoteControlled)
        //    {
        //        return null;
        //    }

        //    if (bomb.Timer == 1)
        //    {
        //        return null;
        //    }

        //    if (bomb.IsRemoteControlled)
        //    {
        //        return new BombState(bomb.Location, bomb.Timer, bomb.WhoPlaced, bomb.CreationTime);
        //    }
        //    else
        //    {
        //        return new BombState(bomb.Location, bomb.Timer - 1, bomb.WhoPlaced, bomb.CreationTime);
        //    }
        //}

        

        //private IEnumerable<Move> FindBestMove()
        //{
        //    var maxValue = PathValueNegativeInf;
        //    List<Point> bestPath = new List<Point> {Me.Location};
        //    List<Move> bestPathDirections = new List<Move> {Move.Stop};

        //    foreach (var target in Board.Locations)
        //    {
        //        for (int d = _distance[target.Y, target.X]; d <= PredictMovesCount; d++)
        //        {
        //            if (_pathValues[d][target.Y, target.X] > maxValue)
        //            {
        //                maxValue = _pathValues[d][target.Y, target.X];
        //                bestPathDirections = _pathsDirTo[d][target.Y, target.X];
        //                bestPath = _pathsTo[d][target.Y, target.X];
        //            }
        //        }
        //    }

        //    var bestPathLength = bestPath.Count;

        //    if (Me.BombCount > 0 && 
        //        bestPath!= null && _bombPlacementValues.Turn[1].Values[Me.Location.Y, Me.Location.X] > bestPath.Take(5).Select((p, i) => _bombPlacementValues.Turn[i+1].Values[p.Y, p.X]).Max() - Config.Eplison)
        //    {
        //        yield return Move.Act;
        //    }

        //    yield return bestPathDirections[0];
        //    yield break;


        //    yield return Move.Act;
        //    var myPosition = Board.GetBomberman();
        //    var direction = (Move)_rand.Next(5);
        //    var newPosition = myPosition.Shift(direction);
        //    if (Board.IsBarrierAt(newPosition))
        //    {
        //        yield return Move.Stop;
        //    }
        //    else yield return direction;
        //}


    }
}
