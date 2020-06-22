using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.ModelBinding;

namespace Bomberman.Api
{
    public class MySolver : MyBaseSolver
    {
        private Random _rand = new Random();
        private Stopwatch _stopwatch = new Stopwatch();

        public const int PredictMovesCount = Config.PredictMovesCount;

        private static PredictionField _chopperLocationProbabilities;
        private static PredictionField _enemyLocationProbabilities;
        private static PredictionField _destroyValues;
        private static PredictionField _bombPlacementValues;

        private static PredictionField _takePerkValues;
        private static PredictionField _bombBlastProbabilities;
        private static PredictionField _visitValues;
        private static PredictionField _aggVisitValues;

        private const int PossibleMovesCount = 5; // 4 directions + Stop
        private double[,,,] _movePredictions;

        public MySolver(string serverUrl) : base(serverUrl)
        {
        }

        protected override IEnumerable<Move> GetMoves()
        {
            _stopwatch.Restart();

            FillDestroyObjectValues();
            //Log.Debug($"DESTROY VALUES:\n{_destroyValues}");
            FillVisitLocationValues();
            //Log.Debug($"VISIT VALUES:\n{_visitValues}");
            
            FillAggregatedVisitLocationValues();

            Log.Info($"Precalc time: {_stopwatch.ElapsedMilliseconds:F0}ms");

            yield return Move.Act;
            var myPosition = Board.GetBomberman();
            var direction = (Move) _rand.Next(5);
            var newPosition = myPosition.Shift(direction);
            if (Board.IsBarrierAt(newPosition))
            {
                yield return Move.Stop;
            }
            else yield return direction;
        }

        private void FillDestroyObjectValues()
        {
            ResetPredictionField(ref _destroyValues);
            ResetPredictionField(ref _visitValues);

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

                    for (int t = 1; t <= PredictMovesCount; t++)
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

            _aggVisitValues.Add(FillBombLocationValues());
            _aggVisitValues.Add(_visitValues);
        }

        private PredictionField FillBombLocationValues()
        {
            ResetPredictionField(ref _bombPlacementValues);

            //TODO: for each location find if it's safe to put bomb - risk of death
            //TODO: for each destroyValue mark places where bomb would hit that

            //return _bombPlacementValues;
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
            ResetPredictionField(ref _chopperLocationProbabilities);

            foreach (var meatchoper in Chopers)
            {
                FillMovePredictions(_chopperLocationProbabilities, meatchoper.Location, meatchoper.LastDirection, false, 1.0, Config.ChopperMoveProbability, CanChopperEnterLocation);
                f.AddWithMultiplier(_chopperLocationProbabilities, GetElementDestructionValue(Board.GetAt(meatchoper.Location)));
            }
        }

        private void AddEnemyValueMovePredictions(PredictionField f)
        {
            ResetPredictionField(ref _enemyLocationProbabilities);

            foreach (var enemy in Enemies)
            {
                FillMovePredictions(_enemyLocationProbabilities, enemy.Location, enemy.LastDirection, enemy.IsLongStanding, GetElementDestructionValue(Board.GetAt(enemy.Location)), Config.EnemyMoveProbability, CanBomberEnterLocation);
                f.AddWithMultiplier(_enemyLocationProbabilities, GetElementDestructionValue(Board.GetAt(enemy.Location)));
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

            _movePredictions[0, currentLocation.Y, currentLocation.X, (int) lastMoveDirection] = estimatedCurrentValue;
            
            for (int t = 0; t < PredictMovesCount; t++)
            {
                if (longStanding)
                {
                    _movePredictions[t+1, currentLocation.Y, currentLocation.X, (int)lastMoveDirection] = estimatedCurrentValue;
                    continue;
                }

                foreach (var boardLocation in Board.Locations)
                {
                    for (Move lm = 0; lm <= Move.Stop; lm++)
                    {
                        var v = _movePredictions[t, boardLocation.Y, boardLocation.X, (int) lm];
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

                                var everyDirectionProbability = 1.0 / (leftAllowed.AsInt() + rightAllowed.AsInt() + upAllowed.AsInt() + downAllowed.AsInt() + 1);
                                
                                _movePredictions[t + 1, boardLocation.Y, boardLocation.X, (int)Move.Stop] += v * everyDirectionProbability;
                                if (leftAllowed)
                                {
                                    _movePredictions[t + 1, leftPos.Y, leftPos.X, (int) Move.Left] += v * everyDirectionProbability;
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
                                var keepDirectionAllowed = canEnterLocation(boardLocation.Shift((Move) lm), t + 1);
                                var turnLeftAllowed = canEnterLocation(boardLocation.Shift(((Move) lm).TurnLeft()), t + 1);
                                var turnRightAllowed = canEnterLocation(boardLocation.Shift(((Move) lm).TurnRight()), t + 1);

                                var normalizedProbabilities = probability.Normalize(keepDirectionAllowed, turnLeftAllowed, turnRightAllowed);

                                _movePredictions[t + 1, boardLocation.Y, boardLocation.X, (int) Move.Stop] += v * normalizedProbabilities.Stop;

                                var keepDirectionNewPos = boardLocation.Shift(lm);
                                _movePredictions[t + 1, keepDirectionNewPos.Y, keepDirectionNewPos.X, (int) lm] += v * normalizedProbabilities.KeepDirection;

                                if (turnLeftAllowed)
                                {
                                    var turnLeftPos = boardLocation.Shift(lm.TurnLeft());
                                    _movePredictions[t + 1, turnLeftPos.Y, turnLeftPos.X, (int) lm.TurnLeft()] += v * normalizedProbabilities.TurnLeft;
                                }

                                if (turnRightAllowed)
                                {
                                    var turnRightPos = boardLocation.Shift(lm.TurnRight());
                                    _movePredictions[t + 1, turnRightPos.Y, turnRightPos.X, (int) lm.TurnRight()] += v * normalizedProbabilities.TurnRight;
                                }

                                var reverseNewPos = boardLocation.Shift(lm.Reverse());
                                _movePredictions[t + 1, reverseNewPos.Y, reverseNewPos.X, (int) lm.Reverse()] += v * normalizedProbabilities.Reverse;
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
                        f.Turn[t].Values[boardLocation.Y, boardLocation.X] += _movePredictions[t, boardLocation.Y, boardLocation.X, (int) lm];
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
            for (int t = 0; t <= PredictMovesCount; t++)
            {
                foreach (var perk in Perks)
                {
                    if (perk.ExpiresIn >= t)
                    {
                        f.Turn[t].Values[perk.Location.Y, perk.Location.X] -= Config.PerkDestroyFutureExpectedValueLoss;
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
            ResetPredictionField(ref _bombBlastProbabilities);

            foreach (var bomb in
                //Important to process timer-bombs first
                Bombs.Enemy.Where(b => !b.IsRemoteControlled).OrderBy(b => b.Timer.Value)
                    .Concat(Bombs.Enemy.Where(b => b.IsRemoteControlled))
                    .Concat(Bombs.My.Where(b => !b.IsRemoteControlled)))
            {
                if (!bomb.IsRemoteControlled && bomb.Timer.HasValue)
                {
                    AddBlastProbabilityAtTurn(bomb, bomb.Timer.Value, 1.0, _bombBlastProbabilities);
                }
                else
                {
                    for (int t = PredictMovesCount; t >= 1; t--) // Reverse order processing for bomb not to count destroyable walls of itself potentially blasting at earlier moves
                    {
                        var turnBlastProbability = Config.RemoteBombEnemyDetonationChanceAtTurn(t);
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

        private double CalcProbabilityAnyOf(params double[] probabilities)
        {
            return CalcProbabilityAnyOf((IEnumerable<double>) probabilities);
        }

        private double CalcProbabilityAnyOf(IEnumerable<double> probabilities)
        {
            return 1 - CalcProbabilityAllOf(probabilities.Select(p => 1.0 - p).ToArray()); // 1.0 - All not happen
        }

        private double CalcProbabilityAllOf(params double[] probabilities)
        {
            return CalcProbabilityAllOf((IEnumerable<double>)probabilities);
        }

        private double CalcProbabilityAllOf(IEnumerable<double> probabilities)
        {
            return probabilities.Aggregate(1.0, (rp, p) => rp * p);
        }

        private void ResetPredictionField(ref PredictionField f)
        {
            if (f == null)
            {
                f = new PredictionField(PredictMovesCount, Board.Size, Board.Size);
            }
            else
            {
                f.Clear();
            }
        }
    }
}
