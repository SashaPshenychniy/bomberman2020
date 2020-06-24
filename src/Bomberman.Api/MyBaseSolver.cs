using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace Bomberman.Api
{
    public abstract class MyBaseSolver : AbstractSolver
    {
        protected Board PrevBoard { get; private set; }
        protected Board Board { get; private set; }

        protected int Time { get; private set; }
        protected BomberState Me { get; private set; }
        protected List<BomberState> Enemies { get; } = new List<BomberState>();
        protected IEnumerable<BomberState> Bombermans => Enumerable.Repeat(Me, 1).Concat(Enemies);
        protected List<MeatchoperState> Chopers { get; } = new List<MeatchoperState>();
        protected Bombs Bombs { get; } = new Bombs();
        protected List<PerkState> Perks { get; } = new List<PerkState>();

        protected MyBaseSolver(string serverUrl) : base(serverUrl)
        {
        }

        protected abstract IEnumerable<Move> GetMoves();

        protected internal override string Get(Board gameBoard)
        {
#if !DEBUG
            try
            {
#endif
                PrevBoard = Board;
                Board = gameBoard;

                if (IsNewRoundStarted())
                {
                    InitializeNewRound();
                }
                else
                {
                    ProcessNewBoardState();
                }

                LogGameState();

                return string.Join(",", GetMoves().Select(m => m.ToString()));
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

        private bool IsNewRoundStarted()
        {
            if (PrevBoard == null)
            {
                return true;
            }

            //if (Board.GetOtherBombermans().Count > Enemies.Count)
            //{
            //    return true;
            //}

            foreach (var l in Board.Locations)
            {
                //If any wall moved without being destroyed
                if (PrevBoard.IsAt(l, Element.DESTROYABLE_WALL) &&
                    !Board.IsAt(l, Element.DESTROYABLE_WALL))
                {
                    return true;
                }
            }

            return false;
        }

        private void InitializeNewRound()
        {
            Log.Debug("");
            Log.Debug("==================================== Initializing new round ====================================");
            Log.Debug("");

            Time = 0;
            
            Me = new BomberState(Board.GetBomberman());
            
            Enemies.Clear();
            Enemies.AddRange(Board.GetAll(Element.OTHER_BOMBERMAN, Element.OTHER_BOMB_BOMBERMAN).Select(b => new BomberState(b)));

            Bombs.Clear();
#if TEST
            var allBombLocations = Board.GetBombs();
            var bombOwnerMap = allBombLocations.Select(bl =>
            {
                var closestBomber = Enemies.Concat(Enumerable.Repeat(Me, 1)).OrderBy(b => Math.Abs(b.Location.Y - bl.Y) + Math.Abs(b.Location.X - bl.X)).First();
                return (new BombState(bl, Board.GetAt(bl), closestBomber, Time), closestBomber);
            }).ToArray();

            foreach (var (b, owner) in bombOwnerMap)
            {
                if (owner == Me)
                {
                    Bombs.TrackMy(b);
                }
                else
                {
                    Bombs.TrackOther(b);
                }
            }
#endif

            Perks.Clear();
            Perks.AddRange(Board.GetPerks().Select(p => new PerkState(p, Board.GetAt(p))));

            Chopers.Clear();
            Chopers.AddRange(Board.GetMeatChoppers().Select(c => new MeatchoperState(c)));
        }

        private void ProcessNewBoardState()
        {
            Time++;
            UpdateBomberStates();
            UpdateChopperStates();
            UpdateBombStates();
            UpdatePerks();
        }

        private void UpdateBomberStates()
        {
            var myNewPosition = Board.GetBomberman();
            var perkTaken = Perks.FirstOrDefault(p => p.Location == myNewPosition);
            Me.ProcessNewLocation(myNewPosition, perkTaken?.PerkType ?? Element.Space, Time);

            var enemyMoves = MapMovements(Enemies.Select(e => e.Location), Board.GetOtherAliveBombermans()).ToArray();
            foreach (var enemyMove in enemyMoves.Where(e => !e.To.HasValue))
            {
                Enemies.RemoveAll(e => e.Location == enemyMove.From.Value);
            }

            foreach (var enemyMove in enemyMoves.Where(e => e.To.HasValue))
            {
                BomberState enemyState;
                if (enemyMove.From.HasValue)
                {
                    enemyState = Enemies.First(e => e.Location == enemyMove.From.Value);
                }
                else
                {
                    enemyState = new BomberState(enemyMove.To.Value);
                    Enemies.Add(enemyState);
                }

                var enemyPerkTaken = Perks.FirstOrDefault(p => p.Location == enemyMove.To.Value);
                enemyState.ProcessNewLocation(enemyMove.To.Value, enemyPerkTaken?.PerkType ?? Element.Space, Time);
            }
        }

        private void UpdateChopperStates()
        {
            var chopperMoves = MapMovements(Chopers.Select(e => e.Location), Board.GetMeatChoppers()).ToArray();
            foreach (var chopperMove in chopperMoves.Where(e => !e.To.HasValue))
            {
                Chopers.RemoveAll(c => c.Location == chopperMove.From.Value);
            }

            foreach (var chopperMove in chopperMoves.Where(e => e.To.HasValue))
            {
                MeatchoperState chopperState;
                if (chopperMove.From.HasValue)
                {
                    chopperState = Chopers.First(c => c.Location == chopperMove.From.Value);
                }
                else
                {
                    chopperState = new MeatchoperState(chopperMove.To.Value);
                    Chopers.Add(chopperState);
                }

                chopperState.SetNewPosition(chopperMove.To.Value);
            }
        }

        private void UpdatePerks()
        {
            foreach (var p in Perks)
            {
                p.ProcessTurn();
            }

            Perks.RemoveAll(p => p.IsExpired);

            //Self-healing state
            Perks.RemoveAll(p => !Board.IsAnyOfAt(p.Location, p.PerkType,
                Element.MEAT_CHOPPER, Element.DeadMeatChopper,
                Element.BOMBERMAN, Element.BOMB_BOMBERMAN, Element.DEAD_BOMBERMAN, Element.OTHER_BOMBERMAN,
                Element.OTHER_DEAD_BOMBERMAN, Element.OTHER_BOMB_BOMBERMAN));

            //Track newly appeared perks
            Perks.AddRange(
                Board.GetPerks().Except(Perks.Select(p => p.Location), PointEqualityComparer.Instance)
                    .Select(np => new PerkState(np, Board.GetAt(np))));
        }

        private void UpdateBombStates()
        {
            foreach (var oldKnownBomb in Bombs.All)
            {
                if (!Board.CanBombBeAt(oldKnownBomb.Location))
                {
                    oldKnownBomb.NotifyDetonated();
                    Bombs.RemoveAt(oldKnownBomb.Location);
                }
                else
                {
                    oldKnownBomb.ProcessTurn(Board.GetAt(oldKnownBomb.Location));
                }
            }

            List<Point> newBombLocations = new List<Point>();
            foreach (var l in Board.Locations)
            {
                // ReSharper disable once SimplifyLinqExpression
                if (Board.IsBombAt(l) && !Bombs.All.Any(b => b.Location == l))
                {
                    newBombLocations.Add(l);
                }
            }

            var bombOwnersMap = WhoPlacedThoseBombs(newBombLocations, Bombermans);
            foreach (var newBomb in bombOwnersMap)
            {
                if (newBomb.Bomber == Me)
                {
                    Bombs.TrackMy(new BombState(newBomb.BombLocation, Board.GetAt(newBomb.BombLocation), Me, Time));
                }
                else
                {
                    Bombs.TrackOther(new BombState(newBomb.BombLocation, Board.GetAt(newBomb.BombLocation), newBomb.Bomber, Time));
                }
            }
        }

        private IEnumerable<(Point BombLocation, BomberState Bomber)> WhoPlacedThoseBombs(IEnumerable<Point> newBombLocations, IEnumerable<BomberState> newBomberStates)
        {
            var unassignedBombs = newBombLocations.ToList();
            var unassignedBombers = newBomberStates.ToList();

            for (int k = 0; unassignedBombs.Count > 0 && k < unassignedBombs.Count*2; k++)
            {
                for (int i = 0; i < unassignedBombs.Count; i++)
                {
                    var bomberOnTopOfBomb = unassignedBombers.FirstOrDefault(bs => bs.Location == unassignedBombs[i]);
                    if (bomberOnTopOfBomb != null)
                    {
                        yield return (unassignedBombs[i], bomberOnTopOfBomb);

                        unassignedBombs.RemoveAt(i);
                        unassignedBombers.Remove(bomberOnTopOfBomb);
                        break;
                    }

                    BomberState potentialMiner = null;
                    int countPotentialMiners = 0;
                    foreach (var bombNeighbouringLocation in Board.GetNeighbouringLocations(unassignedBombs[i]))
                    {
                        var bomberAtNeignboringLocation =
                            newBomberStates.FirstOrDefault(b => b.Location == bombNeighbouringLocation);
                        if (bomberAtNeignboringLocation != null)
                        {
                            potentialMiner = bomberAtNeignboringLocation;
                            countPotentialMiners++;
                        }
                    }

                    if (countPotentialMiners == 1)
                    {
                        yield return (unassignedBombs[i], potentialMiner);

                        unassignedBombs.RemoveAt(i);
                        unassignedBombers.Remove(potentialMiner);
                        break;
                    }
                }
            }

            if (unassignedBombs.Count == 0)
            {
                yield break;
            }

            Log.Warn($"Unable to uniquely identify bomb owners: \nUnassigned bombs: {string.Join(", ", unassignedBombs)}\nUnassigned bombers: {string.Join(", ", unassignedBombers.Select(b => b.Location))}\nBombs: {string.Join(", ", newBombLocations)}\nBombers: {string.Join(", ", newBomberStates.Select(b => b.Location))}");

            for (int k = 0; unassignedBombs.Count > 0 && k < unassignedBombs.Count * 2; k++)
            {
                for (int i = 0; i < unassignedBombs.Count; i++)
                {
                    foreach (var bombNeighbouringLocation in Board.GetNeighbouringLocations(unassignedBombs[i]))
                    {
                        var bomberAtNeignboringLocation =
                            newBomberStates.FirstOrDefault(b => b.Location == bombNeighbouringLocation);
                        if (bomberAtNeignboringLocation != null)
                        {

                            yield return (unassignedBombs[i], bomberAtNeignboringLocation);

                            unassignedBombs.RemoveAt(i);
                            unassignedBombers.Remove(bomberAtNeignboringLocation);

                            i = unassignedBombs.Count;
                            break;
                        }
                    }
                }
            }

            if (unassignedBombs.Count > 0)
            {
                Log.Error($"Unable to identify bomb owners: \nUnassigned bombs: {string.Join(", ", unassignedBombs)}\nUnassigned bombers: {string.Join(", ", unassignedBombers.Select(b => b.Location))}\nBombs: {string.Join(", ", newBombLocations)}\nBombers: {string.Join(", ", newBomberStates.Select(b => b.Location))}");
            }
        }

        private IEnumerable<(Point? From, Point? To)> MapMovements(IEnumerable<Point> from, IEnumerable<Point> to)
        {
            var unassignedFrom = from.ToList();
            var unassignedTo = to.ToList();

            for (int k = unassignedFrom.Count; k > 0; k--)
            {
                for (int i = 0; i < unassignedFrom.Count; i++)
                {
                    int countCandidates = 0;
                    Point candidate = default;

                    foreach (var t in unassignedTo)
                    {
                        if (Math.Abs(unassignedFrom[i].X - t.X) + Math.Abs(unassignedFrom[i].Y - t.Y) <= 1)
                        {
                            countCandidates++;
                            candidate = t;
                        }
                    }

                    if (countCandidates == 1)
                    {
                        yield return (unassignedFrom[i], candidate);

                        unassignedFrom.RemoveAt(i);
                        unassignedTo.Remove(candidate);

                        break;
                    }
                }
            }

            foreach (var uf in unassignedFrom)
            {
                yield return (uf, null);
            }

            foreach (var ut in unassignedTo)
            {
                yield return (null, ut);
            }
        }

        private void LogGameState()
        {
            Log.Debug($"------------------------------------ TURN {Time} ------------------------------------");
            Log.Debug($"\n{Board.ToString()}");
            Log.Debug("GAME STATE:");
            Log.Debug($"Me: {Me}");
            Log.Debug($"Enemies: {string.Concat(Enemies.Select(e => "\n    " + e))}");
            Log.Debug($"Choppers: {string.Join(", ", Chopers)}");
            Log.Debug($"My Bombs: {string.Join(", ", Bombs.My)}");
            Log.Debug($"Enemy Bombs: {string.Join(", ", Bombs.Enemy)}");
            Log.Debug($"Perks: {string.Join(", ", Perks)}");
        }
    }
}
