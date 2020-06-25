using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace Bomberman.Api
{
    public class GameState
    {
        protected static readonly ILog Log = LogManager.GetLogger("MainLogger");

        public const int PredictMovesCount = Config.PredictMovesCount;

        protected Board PrevBoard { get; private set; }
        public Board Board { get; private set; }

        protected int Time { get; private set; }
        protected BomberState Me { get; private set; }
        protected List<BomberState> Enemies { get; private set; } = new List<BomberState>();
        protected IEnumerable<BomberState> Bombermans => Enumerable.Repeat(Me, 1).Concat(Enemies);
        protected List<MeatchoperState> Chopers { get; private set; } = new List<MeatchoperState>();
        protected Bombs Bombs { get; private set; } = new Bombs();
        protected List<PerkState> Perks { get; private set; } = new List<PerkState>();

        private int[,] _distance;
        private bool[,,] _reached;
        private Move[][,] _directions;
        private PredictionField _pathRisk;

        private PredictionField _chopperLocationProbabilities;
        private PredictionField _chopperLocationProbabilitiesTmp;
        private PredictionField _enemyLocationProbabilities;
        private PredictionField _enemyLocationProbabilitiesTmp;
        private PredictionField _destroyValues;
        private PredictionField _bombBlastValues;
        private PredictionField _bombPlacementValues;

        private PredictionField _takePerkValues;
        private PredictionField _bombBlastProbabilities;
        private PredictionField _visitValues;
        private PredictionField _aggVisitValues;
        private double[][,] _pathValues;
        private List<Move>[][,] _pathsDirTo;
        private List<Point>[][,] _pathsTo;

        public const double PathValueNegativeInf = -999;

        public const int PossibleMovesCount = 5; // 4 directions + Stop
        private double[,,,] _movePredictions;

        public GameState()
        {

        }

        public GameState Clone()
        {
            var myClone = Me.Clone();
            var enemiesClones = Enemies.Select(e => e.Clone()).ToList();

            return new GameState
            {
                PrevBoard = PrevBoard.Clone(),
                Board = Board.Clone(),
                Time = Time,
                Me = myClone,
                Enemies = enemiesClones,
                Chopers = Chopers.Select(c => c.Clone()).ToList(),
                Bombs = Bombs.Clone(myClone, enemiesClones),
                Perks = Perks.Select(p => p.Clone()).ToList(),
                _distance = (int[,]) _distance.Clone(),
                _reached = (bool[,,]) _reached.Clone(),
                _directions = _directions.Select(d => (Move[,]) d.Clone()).ToArray(),
                _pathRisk = _pathRisk.Clone(),
                _chopperLocationProbabilities = _chopperLocationProbabilities.Clone(),
                _enemyLocationProbabilities = _enemyLocationProbabilities.Clone(),
                _destroyValues = _destroyValues.Clone(),
                _bombBlastValues = _bombBlastValues.Clone(),
                _bombPlacementValues = _bombPlacementValues.Clone(),
                _takePerkValues = _takePerkValues.Clone(),
                _bombBlastProbabilities = _bombBlastProbabilities.Clone(),
                _visitValues = _visitValues.Clone(),
                _aggVisitValues = _aggVisitValues.Clone(),
                _pathValues = _pathValues.Select(p => (double[,]) p.Clone()).ToArray(),
                _pathsDirTo = _pathsDirTo.Select(p => (List<Move>[,]) p.Clone()).ToArray(),
                _pathsTo = _pathsTo.Select(p => (List<Point>[,]) p.Clone()).ToArray(),
                _movePredictions = (double[,,,]) _movePredictions.Clone()
            };
        }

        private void ApplyMove(Move[] move)
        {
            var posBeforeMove = Board.GetBomberman();
            var pos = posBeforeMove;

            if (move[0] == Move.Act)
            {

            }

            foreach (var m in move)
            {
                if (m == Move.Stop)
                {
                    continue;
                }

                if (m == Move.Act)
                {
                    Board.Replace(Board.GetBomberman(), Element.BOMB_BOMBERMAN);
                    continue;
                }

                var newPos = pos.Shift(m);

                if (Board.GetAt(pos) == Element.BOMB_BOMBERMAN)
                {
                    var bombUnderMe = Bombs.My.Single(b => b.Location == pos);
                    var myBombClone = bombUnderMe.Clone(Me);
                    //myBombClone.ProcessTurn();

                    if (bombUnderMe.IsRemoteControlled)
                    {
                        Board.Replace(pos, Element.BOMB_TIMER_5);
                    }
                    else
                    {
                        var newBombEl = GetBombElementByTimer(Settings.BombTimer - Time + 1 - bombUnderMe.CreationTime);
                    }

                    Board.Replace(newPos, Element.BOMBERMAN);
                }

                pos = newPos;
            }

            //myNewState.ProcessNewLocation(pos, b.GetAt(pos), Time + 1);


        }

        private static Element GetBombElementByTimer(int? timer)
        {
            if (!timer.HasValue)
            {
                return Element.BOMB_TIMER_5;
            }

            switch (timer.Value)
            {
                case 5:
                    return Element.BOMB_TIMER_5;
                case 4:
                    return Element.BOMB_TIMER_4;
                case 3:
                    return Element.BOMB_TIMER_3;
                case 2:
                    return Element.BOMB_TIMER_2;
                case 1:
                    return Element.BOMB_TIMER_1;
            }

            throw new NotImplementedException();
        }

        public void ApplyNextTurnBoardState(Board newBoard)
        {
            PrevBoard = Board;
            Board = newBoard;

            if (IsNewRoundStarted())
            {
                InitializeNewRound();
            }
            else
            {
                ProcessNewBoardState();
            }

            LogGameState();
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

            for (int k = 0; unassignedBombs.Count > 0 && k < unassignedBombs.Count * 2; k++)
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



        /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void AnalyzeGameState()
        {
            FillDestroyObjectValues();
            Log.Debug($"DESTROY VALUES:\n{_destroyValues}");
            FillVisitLocationValues();
            Log.Debug($"VISIT VALUES:\n{_visitValues}");
            FillBombPlacementValues();
            Log.Debug($"BOMB BLAST VALUES:\n{_bombBlastValues}");
            Log.Debug($"BOMB PLACEMENT VALUES:\n{_bombPlacementValues}");

            FillDistance();

            FillAggregatedVisitLocationValues();
            Log.Debug($"AGG VISIT VALUES:\n{_aggVisitValues}");


            Log.Debug($"Blasts:\n {_bombBlastProbabilities}");
            Log.Debug($"Distances:\n{_distance.ToLogStr(x => x.ToString(), 3)}");

            for (int i = 1; i <= PredictMovesCount; i++)
            {
                Log.Debug($"Directions at step {i}:\n{_directions[i].ToLogStr(x => x.ToString().Substring(0, 1), 3)}");
            }

            FillPathValues();

            for (int i = 1; i <= PredictMovesCount; i++)
            {
                Log.Debug($"Path Values with length {i}:\n{_pathValues[i].ToLogStr(x => x.ToString("F2"), 7)}");
            }
        }

        public IEnumerable<EvaluatedPath> GetOrderedEvaluatedPaths()
        {
            return GetAllEvaluatedPaths().OrderBy(p => p.Score);
        }

        public class EvaluatedPath
        {
            public Point Target { get; set; }
            public int Length { get; set; }
            public List<Point> Path { get; set; }
            public List<Move> Directions { get; set; }
            public double Score { get; set; }
        }

        private IEnumerable<EvaluatedPath> GetAllEvaluatedPaths()
        {
            foreach (var target in Board.Locations)
            {
                for (int d = _distance[target.Y, target.X]; d <= PredictMovesCount; d++)
                {
                    yield return new EvaluatedPath
                    {
                        Target = target,
                        Length = d,
                        Path = _pathsTo[d][target.Y, target.X],
                        Directions = _pathsDirTo[d][target.Y, target.X],
                        Score = _pathValues[d][target.Y, target.X]
                    };
                }
            }
        }

        private const int INF_DIST = 999;

        private void FillDistance()
        {
            if (_distance == null || _directions == null || _reached == null)
            {
                _distance = new int[Board.Size, Board.Size];
                _directions = new Move[PredictMovesCount + 1][,];
                for (int i = 0; i <= PredictMovesCount; i++)
                {
                    _directions[i] = new Move[Board.Size, Board.Size];
                }
                _reached = new bool[Board.Size, Board.Size, PredictMovesCount + 1];
            }
            ResetPredictionField(ref _pathRisk);

            for (int i = 0; i < Board.Size; i++)
            {
                for (int j = 0; j < Board.Size; j++)
                {
                    _distance[i, j] = INF_DIST;

                    for (int m = 0; m <= PredictMovesCount; m++)
                    {
                        _directions[m][i, j] = Move.Stop;
                        _pathRisk.Turn[m].Values[i, j] = 1.0;
                    }
                }
            }

            Array.Clear(_reached, 0, _reached.Length);

            var q = new Queue<(Point Location, int Distance, double BlastRisk, Move LastMoveToGetHere)>();
            q.Enqueue((Me.Location, 0, 0, Move.Stop));

            while (q.Count > 0)
            {
                var (p, d, risk, dir) = q.Dequeue();

                if (risk > Config.BombBlastMaxProbabilityToConsiderPassable)
                {
                    continue;
                }

                if (Board.IsAt(p, Element.WALL))
                {
                    continue;
                }

                if (Board.IsAt(p, Element.DESTROYABLE_WALL) && ProbabilityBlastAtBefore(p, d) < Config.TempObjectDisapperMinProbabilityToConsiderPassable)
                {
                    continue;
                }

                if (Board.IsOtherUnitAt(p) && ProbabilityBlastAtBefore(p, d) < Config.TempObjectDisapperMinProbabilityToConsiderPassable && d < 10 /*Units are likely to move somewhere*/)
                {
                    continue;
                }

                if (Board.IsBombAt(p))
                {
                    var bomb = Bombs.All.Single(b => b.Location == p);
                    if (bomb.ProbabilityExplodesBefore(d, Time) < Config.TempObjectDisapperMinProbabilityToConsiderPassable)
                    {
                        continue;
                    }
                }

                if (d < _distance[p.Y, p.X] + 5)
                {
                    if (d < _distance[p.Y, p.X])
                    {
                        _distance[p.Y, p.X] = d;
                    }

                    if (risk < _pathRisk.Turn[d].Values[p.Y, p.X] || risk < Config.Eplison)
                    {
                        _pathRisk.Turn[d].Values[p.Y, p.X] = risk;
                        _directions[d][p.Y, p.X] = dir;
                    }

                    if (!_reached[p.Y, p.X, d])
                    {
                        _reached[p.Y, p.X, d] = true;

                        if (d < PredictMovesCount)
                        {
                            for (var m = Move.Left; m <= Move.Stop; m++)
                            {
                                var nextPos = p.Shift(m);
                                q.Enqueue((nextPos, d + 1, CalcProbabilityAnyOf(risk, _bombBlastProbabilities.Turn[d + 1].Values[nextPos.Y, nextPos.X]), m));
                            }
                        }
                    }
                }
            }
        }

        private bool TryFindSafePathTo(Point p, out List<Move> directions, out List<Point> path, int eta = -1)
        {
            var dist = eta == -1 ? _distance[p.Y, p.X] : eta;

            if (_distance[p.Y, p.X] == INF_DIST)
            {
                directions = null;
                path = null;
                return false;
            }

            if (dist == 0)
            {
                directions = new List<Move> { Move.Stop };
                path = new List<Point> { p };
                return true;
            }

            directions = new List<Move>(dist);
            path = new List<Point>(dist);

            for (var pp = p; dist != 0; dist--)
            {
                path.Add(pp);
                var dir = _directions[dist][pp.Y, pp.X];
                directions.Add(dir);
                pp = pp.Shift(dir.Reverse());
            }

            directions.Reverse();
            path.Reverse();

            return true;
        }

        private double ProbabilityBlastAtBefore(Point location, int turn)
        {
            if (turn <= 1)
            {
                return 0;
            }
            return CalcProbabilityAnyOf(Enumerable.Range(1, turn - 1).Select(t => _bombBlastProbabilities.Turn[t].Values[location.Y, location.X]));
        }

        private void FillDestroyObjectValues()
        {
            ResetPredictionField(ref _destroyValues, PredictMovesCount + Config.MaxPredictedBlastDelay);

            AddWallDestructionValues(_destroyValues);
            AddChopperValueMovePredictions(_destroyValues);
            AddEnemyValueMovePredictions(_destroyValues);
            AddPerkDestroyValues(_destroyValues);
        }

        private void FillVisitLocationValues()
        {
            ResetPredictionField(ref _visitValues);

            AddTakePerkValues(_visitValues);
            AddDieFromChopperValues(_visitValues);
            AddDieFromBombValues(_visitValues);

            //TODO: Implement DieFromZombie values
            //TODO: Implement Lockdown chance analysis both by walls and by enemies
        }

        private void AddWallDestructionValues(PredictionField f)
        {
            foreach (var boardLocation in Board.Locations)
            {
                var element = Board.GetAt(boardLocation);
                var elementDestructionValue = GetElementDestructionValue(element);

                if (IsStaticElementOrMe(element))
                {
                    f.Turn[0].Values[boardLocation.Y, boardLocation.X] = elementDestructionValue;

                    for (int t = 1; t < f.Turn.Length; t++)
                    {
                        f.Turn[t].Values[boardLocation.Y, boardLocation.X] = elementDestructionValue;
                    }
                }
            }
        }

        private void AddTakePerkValues(PredictionField f)
        {
            FillTakePerkValues();
            f.Add(_takePerkValues);
        }

        private void FillTakePerkValues()
        {
            ResetPredictionField(ref _takePerkValues);

            for (int t = 0; t <= PredictMovesCount; t++)
            {
                foreach (var perk in Perks)
                {
                    if (perk.ExpiresIn >= t)
                    {
                        //TODO: Take into account different perk value based on existing taken perks
                        _takePerkValues.Turn[t].Values[perk.Location.Y, perk.Location.X] = GetElementVisitValue(perk.PerkType);
                    }
                }
            }
        }

        public void FillAggregatedVisitLocationValues()
        {
            ResetPredictionField(ref _aggVisitValues);

            _aggVisitValues.Add(_visitValues);

            _aggVisitValues.Add(_bombPlacementValues);

            //for (int t = 1; t <= PredictMovesCount; t++)
            //{
            //    foreach (var boardLocation in Board.Locations)
            //    {
            //        _aggVisitValues.Turn[t].Values[boardLocation.Y, boardLocation.X] += t * Config.SkipTurnExpectedLoss;
            //    }
            //}

            //TODO: for each location find if it's safe to put bomb - risk of death
        }

        private void FillPathValues()
        {
            if (_pathValues == null)
            {
                _pathValues = new double[PredictMovesCount + 1][,];
                _pathsTo = new List<Point>[PredictMovesCount + 1][,];
                _pathsDirTo = new List<Move>[PredictMovesCount + 1][,];

                for (int i = 0; i <= PredictMovesCount; i++)
                {
                    _pathValues[i] = new double[Board.Size, Board.Size];
                    _pathsTo[i] = new List<Point>[Board.Size, Board.Size];
                    _pathsDirTo[i] = new List<Move>[Board.Size, Board.Size];
                }
            }

            var noPath = new List<Point> { Me.Location };
            var noPathDirections = new List<Move> { Move.Stop };

            for (int i = 0; i <= PredictMovesCount; i++)
            {
                foreach (var pos in Board.Locations)
                {
                    _pathValues[i][pos.Y, pos.X] = PathValueNegativeInf;
                    _pathsTo[i][pos.Y, pos.X] = noPath;
                    _pathsDirTo[i][pos.Y, pos.X] = noPathDirections;
                }
            }


            foreach (var targetLocation in Board.Locations)
            {
                for (int d = _distance[targetLocation.Y, targetLocation.X]; d <= PredictMovesCount; d++)
                {
                    if (TryFindSafePathTo(targetLocation, out List<Move> directions, out List<Point> locations, d))
                    {
                        _pathsDirTo[d][targetLocation.Y, targetLocation.X] = directions;
                        _pathsTo[d][targetLocation.Y, targetLocation.X] = locations;

                        _pathValues[d][targetLocation.Y, targetLocation.X] = 0;
                        for (var t = 1; t <= locations.Count; t++)
                        {
                            var pathLoc = locations[t - 1];
                            _pathValues[d][targetLocation.Y, targetLocation.X] += _visitValues.Turn[t].Values[pathLoc.Y, pathLoc.X];
                            for (int i = 0; i < t - 1; i++)
                            {
                                if (locations[i] == pathLoc)
                                {
                                    //We've already taken this perk on this path before - avoid counting multiple times
                                    _pathValues[d][targetLocation.Y, targetLocation.X] -= _takePerkValues.Turn[t].Values[pathLoc.Y, pathLoc.X];
                                }
                            }
                        }

                        var bombPlacement = PlanBombPlacementOnPath(locations);
                        foreach (var plannedBomb in bombPlacement)
                        {
                            _pathValues[d][targetLocation.Y, targetLocation.X] += _bombPlacementValues.Turn[plannedBomb.Turn].Values[plannedBomb.Location.Y, plannedBomb.Location.X];
                        }

                        _pathValues[d][targetLocation.Y, targetLocation.X] += locations.Count * Config.SkipTurnExpectedLoss;
                    }
                }
            }
        }

        private IEnumerable<(int Turn, Point Location)> PlanBombPlacementOnPath(List<Point> path)
        {
            yield return (path.Count, path[path.Count - 1]);

            //TODO: IMPLEMENT INTERMEDIATE BOMB PLACEMENT FOR LONG PATHS
        }

        private void FillBombBlastValues()
        {
            ResetPredictionField(ref _bombBlastValues, PredictMovesCount + Config.MaxPredictedBlastDelay);

            for (int t = 1; t <= PredictMovesCount + Config.MaxPredictedBlastDelay; t++)
            {
                var bombRadius = t >= Me.BombRadiusExpirationTime ? Settings.BombRadiusDefault : Me.BombRadius;

                foreach (var bombPos in Board.Locations)
                {
                    if (!Board.IsAnyOfAt(bombPos, Element.WALL, Element.DESTROYABLE_WALL))
                    {
                        _bombBlastValues.Turn[t].Values[bombPos.Y, bombPos.X] += _destroyValues.Turn[t].Values[bombPos.Y, bombPos.X]; // In case choppers expected location is same sa bomb - it's good location in fact

                        for (var m = Move.Left; m <= Move.Down; m++)
                        {
                            var blastPassProbability = 1.0;
                            for (int d = 1; d <= bombRadius && blastPassProbability > Config.Eplison; d++)
                            {
                                var blastPos = bombPos.Shift(m, d);
                                _bombBlastValues.Turn[t].Values[bombPos.Y, bombPos.X] += _destroyValues.Turn[t].Values[blastPos.Y, blastPos.X] * blastPassProbability;

                                if (Board.IsAt(blastPos, Element.DESTROYABLE_WALL))
                                {
                                    blastPassProbability = CalcProbabilityAllOf(blastPassProbability, ProbabilityBlastAtBefore(bombPos, t));
                                }

                                if (Board.IsAt(blastPos, Element.WALL))
                                {
                                    blastPassProbability = 0;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FillBombPlacementValues()
        {
            FillBombBlastValues();

            ResetPredictionField(ref _bombPlacementValues);

            for (int t = 1; t <= PredictMovesCount; t++)
            {
                int bombTimer;
                bool isRemote;

                if (Me.DetonatorsCount > 0)
                {
                    isRemote = true;

                    const int blastUnderImmuneInSec = 1;
                    if (Me.WhenImmuneExpires(Time) > t + blastUnderImmuneInSec)
                    {
                        bombTimer = blastUnderImmuneInSec;
                    }
                    else
                    {
                        bombTimer = 3; // TODO: Maybe worth calculating more precisely for each case to find safe spot
                    }
                }
                else
                {
                    bombTimer = Settings.BombTimer;
                    isRemote = false;
                }

                if (t + bombTimer > PredictMovesCount)
                {
                    break;
                }

                foreach (var bombPos in Board.Locations)
                {
                    if (isRemote)
                    {
                        double maxBlastValueOfAllDelays = 0;
                        for (int bt = t; bt <= PredictMovesCount - bombTimer; bt++)
                        {
                            maxBlastValueOfAllDelays = Math.Max(maxBlastValueOfAllDelays, _bombBlastValues.Turn[bt + bombTimer].Values[bombPos.Y, bombPos.X] + (bt - t) * Config.SkipTurnExpectedLoss);
                        }
                        _bombPlacementValues.Turn[t].Values[bombPos.Y, bombPos.X] = maxBlastValueOfAllDelays;
                    }
                    else
                    {
                        _bombPlacementValues.Turn[t].Values[bombPos.Y, bombPos.X] += _bombBlastValues.Turn[t + bombTimer].Values[bombPos.Y, bombPos.X];
                    }
                }
            }
        }

        private bool IsStaticElementOrMe(Element e)
        {
            return e != Element.OTHER_BOMBERMAN && e != Element.OTHER_BOMB_BOMBERMAN && e != Element.MEAT_CHOPPER;
        }

        private double GetElementDestructionValue(Element el)
        {
            switch (el)
            {
                case Element.DESTROYABLE_WALL:
                    return Settings.DestroyWallAward + Settings.PerkProbability * Config.PerkDiscoveringAvgValue;
                case Element.MEAT_CHOPPER:
                    return Settings.KillMeatchopperAward;
                case Element.OTHER_BOMBERMAN:
                case Element.OTHER_BOMB_BOMBERMAN:
                    return Settings.KillBombermanAward;
                default: return 0;
            }
        }

        private double GetElementVisitValue(Element el)
        {
            switch (el)
            {
                case Element.BOMB_BLAST_RADIUS_INCREASE:
                    return Settings.PerkTakingAward + Config.BombRadiusFutureExpectedValue;
                case Element.BOMB_COUNT_INCREASE:
                    return Settings.PerkTakingAward + Config.BombCountFutureExpectedValue;
                case Element.BOMB_REMOTE_CONTROL:
                    return Settings.PerkTakingAward + Config.BombRemoteControlFutureExpectedValue;
                case Element.BOMB_IMMUNE:
                    return Settings.PerkTakingAward + Config.BombImmuneFutureExpectedValue;
                default: return 0;
            }
        }

        private void AddChopperValueMovePredictions(PredictionField f)
        {
            ResetPredictionField(ref _chopperLocationProbabilities, PredictMovesCount + Config.MaxPredictedBlastDelay);

            foreach (var meatchoper in Chopers)
            {
                ResetPredictionField(ref _chopperLocationProbabilitiesTmp, PredictMovesCount + Config.MaxPredictedBlastDelay);
                FillMovePredictions(_chopperLocationProbabilitiesTmp, meatchoper.Location, meatchoper.LastDirection, false, 1.0, Config.ChopperMoveProbability, CanChopperEnterLocation);
                f.AddWithMultiplier(_chopperLocationProbabilitiesTmp, GetElementDestructionValue(Board.GetAt(meatchoper.Location)));
                _chopperLocationProbabilities.AddWithAggregator(_chopperLocationProbabilitiesTmp, (x, y) => CalcProbabilityAnyOf(x, y));
            }
        }

        private void AddEnemyValueMovePredictions(PredictionField f)
        {
            ResetPredictionField(ref _enemyLocationProbabilities, PredictMovesCount + Config.MaxPredictedBlastDelay);

            foreach (var enemy in Enemies)
            {
                ResetPredictionField(ref _enemyLocationProbabilitiesTmp, PredictMovesCount + Config.MaxPredictedBlastDelay);
                FillMovePredictions(_enemyLocationProbabilitiesTmp, enemy.Location, enemy.LastDirection, enemy.IsLongStanding, 1.0, Config.EnemyMoveProbability, CanBomberEnterLocation);
                f.AddWithMultiplier(_enemyLocationProbabilitiesTmp, GetElementDestructionValue(Board.GetAt(enemy.Location)));
                _enemyLocationProbabilities.AddWithAggregator(_enemyLocationProbabilitiesTmp, (x, y) => CalcProbabilityAnyOf(x, y));
            }
        }

        private void FillMovePredictions(PredictionField f, Point currentLocation, Move lastMoveDirection, bool longStanding, double estimatedCurrentValue, MoveProbability probability, Func<Point, int, bool> canEnterLocation)
        {
            if (_movePredictions == null)
            {
                _movePredictions = new double[PredictMovesCount + 1, Board.Size, Board.Size, PossibleMovesCount];
            }
            else
            {
                Array.Clear(_movePredictions, 0, _movePredictions.Length);
            }

            _movePredictions[0, currentLocation.Y, currentLocation.X, (int)lastMoveDirection] = estimatedCurrentValue;

            for (int t = 0; t < PredictMovesCount; t++)
            {
                if (longStanding)
                {
                    _movePredictions[t + 1, currentLocation.Y, currentLocation.X, (int)lastMoveDirection] = estimatedCurrentValue;
                    continue;
                }

                foreach (var boardLocation in Board.Locations)
                {
                    for (Move lm = 0; lm <= Move.Stop; lm++)
                    {
                        var v = _movePredictions[t, boardLocation.Y, boardLocation.X, (int)lm];
                        if (v > 0)
                        {
                            if (lm == Move.Stop)
                            {
                                Point leftPos = boardLocation.Shift(Move.Left);
                                bool leftAllowed = canEnterLocation(leftPos, t + 1);
                                Point rightPos = boardLocation.Shift(Move.Right);
                                bool rightAllowed = canEnterLocation(rightPos, t + 1);
                                Point upPos = boardLocation.Shift(Move.Up);
                                bool upAllowed = canEnterLocation(upPos, t + 1);
                                Point downPos = boardLocation.Shift(Move.Down);
                                bool downAllowed = canEnterLocation(downPos, t + 1);
                                bool stopAllowed = probability.Stop > Config.Eplison;

                                var everyDirectionProbability = 1.0 / (leftAllowed.AsInt() + rightAllowed.AsInt() + upAllowed.AsInt() + downAllowed.AsInt() + stopAllowed.AsInt());

                                if (stopAllowed)
                                {
                                    _movePredictions[t + 1, boardLocation.Y, boardLocation.X, (int)Move.Stop] += v * everyDirectionProbability;
                                }

                                if (leftAllowed)
                                {
                                    _movePredictions[t + 1, leftPos.Y, leftPos.X, (int)Move.Left] += v * everyDirectionProbability;
                                }
                                if (rightAllowed)
                                {
                                    _movePredictions[t + 1, rightPos.Y, rightPos.X, (int)Move.Right] += v * everyDirectionProbability;
                                }
                                if (upAllowed)
                                {
                                    _movePredictions[t + 1, upPos.Y, upPos.X, (int)Move.Up] += v * everyDirectionProbability;
                                }
                                if (downAllowed)
                                {
                                    _movePredictions[t + 1, downPos.Y, downPos.X, (int)Move.Down] += v * everyDirectionProbability;
                                }
                            }
                            else
                            {
                                var keepDirectionAllowed = canEnterLocation(boardLocation.Shift((Move)lm), t + 1);
                                var turnLeftAllowed = canEnterLocation(boardLocation.Shift(((Move)lm).TurnLeft()), t + 1);
                                var turnRightAllowed = canEnterLocation(boardLocation.Shift(((Move)lm).TurnRight()), t + 1);

                                var normalizedProbabilities = probability.Normalize(keepDirectionAllowed, turnLeftAllowed, turnRightAllowed);

                                _movePredictions[t + 1, boardLocation.Y, boardLocation.X, (int)Move.Stop] += v * normalizedProbabilities.Stop;

                                var keepDirectionNewPos = boardLocation.Shift(lm);
                                if (keepDirectionAllowed)
                                {
                                    _movePredictions[t + 1, keepDirectionNewPos.Y, keepDirectionNewPos.X, (int)lm] += v * normalizedProbabilities.KeepDirection;
                                }

                                if (turnLeftAllowed)
                                {
                                    var turnLeftPos = boardLocation.Shift(lm.TurnLeft());
                                    _movePredictions[t + 1, turnLeftPos.Y, turnLeftPos.X, (int)lm.TurnLeft()] += v * normalizedProbabilities.TurnLeft;
                                }

                                if (turnRightAllowed)
                                {
                                    var turnRightPos = boardLocation.Shift(lm.TurnRight());
                                    _movePredictions[t + 1, turnRightPos.Y, turnRightPos.X, (int)lm.TurnRight()] += v * normalizedProbabilities.TurnRight;
                                }

                                var reverseNewPos = boardLocation.Shift(lm.Reverse());
                                _movePredictions[t + 1, reverseNewPos.Y, reverseNewPos.X, (int)lm.Reverse()] += v * normalizedProbabilities.Reverse;
                            }
                        }
                    }
                }
            }

            for (int t = 0; t <= PredictMovesCount; t++)
            {
                foreach (var boardLocation in Board.Locations)
                {
                    for (Move lm = 0; lm <= Move.Stop; lm++)
                    {
                        f.Turn[t].Values[boardLocation.Y, boardLocation.X] += _movePredictions[t, boardLocation.Y, boardLocation.X, (int)lm];
                    }
                }
            }
        }

        private bool CanBomberEnterLocation(Point location, int afterSecondsCount)
        {
            //TODO: Take into account bombers, bombs, choppers etc.
            return !Board.IsAnyOfAt(location, Element.WALL, Element.DESTROYABLE_WALL);
        }

        private bool CanChopperEnterLocation(Point location, int afterSecondsCount)
        {
            return !Board.IsAnyOfAt(location, Element.WALL, Element.DESTROYABLE_WALL);
        }

        private bool CanZombieEnterLocation(Point location, int afterSecondsCount)
        {
            return !Board.IsAnyOfAt(location, Element.WALL);
        }

        private void AddPerkDestroyValues(PredictionField f)
        {
            for (int t = 0; t < f.Turn.Length; t++)
            {
                foreach (var perk in Perks)
                {
                    if (perk.ExpiresIn >= t)
                    {
                        f.Turn[t].Values[perk.Location.Y, perk.Location.X] += Config.PerkDestroyFutureExpectedValueLoss;
                    }
                }
            }
        }

        private void AddDieFromChopperValues(PredictionField f)
        {
            f.AddWithMultiplier(_chopperLocationProbabilities, 0, 1, Config.DeathExpectedValueLoss);
        }

        private void AddDieFromBombValues(PredictionField f)
        {
            FillBombBlastProbabilities();
            f.AddWithMultiplier(_bombBlastProbabilities, Config.DeathExpectedValueLoss);
            //Log.Debug($"BLAST VALUES:\n{_bombBlastProbabilities}");
        }

        private void FillBombBlastProbabilities()
        {
            ResetPredictionField(ref _bombBlastProbabilities, PredictMovesCount + Config.MaxPredictedBlastDelay);

            foreach (var bomb in
                //Important to process timer-bombs first
                Bombs.Enemy.Where(b => !b.IsRemoteControlled).OrderBy(b => b.Timer.Value)
                    .Concat(Bombs.Enemy.Where(b => b.IsRemoteControlled))
                    .Concat(Bombs.My/*.Where(b => !b.IsRemoteControlled)*/)

                )
            {
                if (!bomb.IsRemoteControlled)
                {
                    AddBlastProbabilityAtTurn(bomb, bomb.Timer.Value, 1.0, _bombBlastProbabilities);
                }
                else
                {
                    for (int t = PredictMovesCount; t >= 1; t--) // Reverse order processing for bomb not to count destroyable walls of itself potentially blasting at earlier moves
                    {
                        var turnBlastProbability = Config.RemoteBombEnemyDetonationChanceAtTurn((Time - bomb.CreationTime) + t);
                        AddBlastProbabilityAtTurn(bomb, t, turnBlastProbability, _bombBlastProbabilities);
                    }
                }
            }
        }

        private void AddBlastProbabilityAtTurn(BombState bomb, int turn, double blastProbability, PredictionField blastProbabilityField)
        {
            blastProbabilityField.Turn[turn].Values[bomb.Location.Y, bomb.Location.X] = blastProbability;

            for (var m = Move.Left; m <= Move.Down; m++)
            {
                var blastReachProbability = blastProbability;
                for (int d = 1; d <= bomb.Radius && blastReachProbability > Config.Eplison; d++)
                {
                    var pos = bomb.Location.Shift(m, d);

                    if (!Board.IsNonDestroyableBlastStopperAt(pos))
                    {
                        blastProbabilityField.Turn[turn].Values[pos.Y, pos.X] = CalcProbabilityAnyOf(blastProbabilityField.Turn[turn].Values[pos.Y, pos.X], blastReachProbability);
                    }

                    if (Board.IsBlastStopperAt(pos))
                    {
                        if (Board.IsDestroyableBlastStopperAt(pos))
                        {
                            var wallAlreadyDestroyedProbability = CalcProbabilityAnyOf(Enumerable.Range(1, turn - 1)
                                .Select(t => blastProbabilityField.Turn[t].Values[pos.Y, pos.X]));

                            blastReachProbability *= wallAlreadyDestroyedProbability;
                        }
                        else
                        {
                            blastReachProbability = 0;
                        }
                    }
                }
            }
        }

        public static double CalcProbabilityAnyOf(params double[] probabilities)
        {
            return CalcProbabilityAnyOf((IEnumerable<double>)probabilities);
        }

        public static double CalcProbabilityAnyOf(IEnumerable<double> probabilities)
        {
            return 1 - CalcProbabilityAllOf(probabilities.Select(p => 1.0 - p).ToArray()); // 1.0 - All not happen
        }

        public static double CalcProbabilityAllOf(params double[] probabilities)
        {
            return CalcProbabilityAllOf((IEnumerable<double>)probabilities);
        }

        public static double CalcProbabilityAllOf(IEnumerable<double> probabilities)
        {
            return probabilities.Aggregate(1.0, (rp, p) => rp * p);
        }

        private void ResetPredictionField(ref PredictionField f, int turnsCount = PredictMovesCount)
        {
            if (f == null)
            {
                f = new PredictionField(turnsCount, Board.Size, Board.Size);
            }
            else
            {
                f.Clear();
            }
        }
    }
}
