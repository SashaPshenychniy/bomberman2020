/*-
 * #%L
 * Codenjoy - it's a dojo-like platform from developers to developers.
 * %%
 * Copyright (C) 2018 Codenjoy
 * %%
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public
 * License along with this program.  If not, see
 * <http://www.gnu.org/licenses/gpl-3.0.html>.
 * #L%
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bomberman.Api
{
    public class Board
    {
        //private String BoardString { get; }
        //private LengthToXY LengthXY;
        private string[] _board;
        public readonly int Size;

        private static Point[] _locations;
        public Point[] Locations => _locations;
        private static Point[,][] _neighbouringLocations;
        
        public Board(string boardString)
        {
            Size = (int)Math.Sqrt(boardString.Length);
            _board = new string[Size];
            for (int i = 0; i < Size; i++)
            {
                _board[i] = boardString.Substring(i * Size, Size);
            }

            if (_board.Any(row => row.Length != Size))
            {
                throw new ArgumentException("Board is not square.");
            }

            if (_locations == null || _locations.Length != boardString.Length)
            {
                _locations = Enumerable.Range(0, Size * Size).Select(i => new Point(i % Size, i / Size)).ToArray();
                _neighbouringLocations = new Point[Size,Size][];
                foreach (var l in _locations)
                {
                    _neighbouringLocations[l.Y, l.X] = _locations.Where(ll => Math.Abs(ll.X - l.X) + Math.Abs(ll.Y - l.Y) == 1).ToArray();
                }
            }

            

            //BoardString = boardString.Replace("\n", "");
            //LengthXY = new LengthToXY(Size); 
        }

        /// <summary>
        /// GameBoard size (actual board size is Size x Size cells)
        /// </summary>
        //public int Size
        //{
        //    get
        //    {
        //        return _size;
        //        //return (int)Math.Sqrt(BoardString.Length);
        //    }
        //}


        public Point[] GetNeighbouringLocations(Point location)
        {
            return _neighbouringLocations[location.Y, location.X];
        }

        public IEnumerable<Point> GetNeighbouringLocationsAndSource(Point location)
        {
            yield return location;
            foreach (var neighbouringLocation in GetNeighbouringLocations(location))
            {
                yield return neighbouringLocation;
            }
        }

        public Point GetBomberman()
        {
            return Get(Element.BOMBERMAN)
                    .Concat(Get(Element.BOMB_BOMBERMAN))
                    .Concat(Get(Element.DEAD_BOMBERMAN))
                    .Single();
        }

        private static Element[] OtherBombermans = {Element.OTHER_BOMBERMAN, Element.OTHER_BOMB_BOMBERMAN, Element.OTHER_DEAD_BOMBERMAN};

        public List<Point> GetOtherBombermans()
        {
            return GetAll(OtherBombermans);
        }

        public List<Point> GetOtherAliveBombermans()
        {
            return Get(Element.OTHER_BOMBERMAN)
                .Concat(Get(Element.OTHER_BOMB_BOMBERMAN))
                .ToList();
        }

        public bool IsMyBombermanDead
        {
            get
            {
                return _board.Any(row => row.Contains((char) Element.DEAD_BOMBERMAN));
                //return BoardString.Contains((char)Element.DEAD_BOMBERMAN);
            }
        }

        public Element GetAt(Point point)
        {
            if (point.IsOutOf(Size))
            {
                return Element.WALL;
            }

            return (Element)_board[point.Y][point.X];
            //return (Element)BoardString[LengthXY.GetLength(point.X, point.Y)];
        }

        public bool IsAt(Point point, Element element)
        {
            if (point.IsOutOf(Size))
            {
                return false;
            }

            return GetAt(point) == element;
        }

        public string BoardAsString()
        {
            return string.Join("\n", _board);
            //string result = "";
            //for (int i = 0; i < Size; i++)
            //{
            //    result += BoardString.Substring(i * Size, Size);
            //    result += "\n";
            //}
            //return result;
        }

        /// <summary>
        /// gets board view as string
        /// </summary>
        public string ToString()
        {
           return string.Format("{0}\n" +
                    "Bomberman at: {1}\n" +
                    "Other bombermans at: {2}\n" +
                    "Meat choppers at: {3}\n" +
                    "Destroy walls at: {4}\n" +
                    "Bombs at: {5}\n" +
                    "Blasts: {6}\n" +
                    "Expected blasts at: {7}\n" +
                    "Perks at: {8}",
                    BoardAsString().Replace((char)Element.BOMBERMAN, 'B'),
                    GetBomberman(),
                    ListToString(GetOtherBombermans()),
                    ListToString(GetMeatChoppers()),
                    ListToString(GetDestroyableWalls()),
                    ListToString(GetBombs()),
                    ListToString(GetBlasts()),
                    ListToString(GetFutureBlasts()),
                    ListToString(GetPerks()));
        }

        private string ListToString(List<Point> list)
        {
            return string.Join(",", list.ToArray());
        }

        public List<Point> GetBarrier()
        {
            return GetMeatChoppers()
                .Concat(GetWalls())
                .Concat(GetBombs())
                .Concat(GetDestroyableWalls())
                .Concat(GetOtherBombermans())
                .Distinct()
                .ToList();
        }

        private static Element[] PotentialMeatchoppers = new Element[] { Element.MEAT_CHOPPER, Element.DeadMeatChopper, Element.DestroyedWall };

        public List<Point> GetMeatChoppers()
        {
            return Get(Element.MEAT_CHOPPER);
        }

        public List<Point> Get(Element element)
        {
            return Locations.Where(l => IsAt(l, element)).ToList();

            //List<Point> result = new List<Point>();

            //for (int i = 0; i < Size * Size; i++)
            //{
            //    Point pt = LengthXY.GetXY(i);

            //    if (IsAt(pt, element))
            //    {
            //        result.Add(pt);
            //    }
            //}

            //return result;
        }

        public List<Point> GetAll(params Element[] elements)
        {
            return Locations.Where(l => IsAnyOfAt(l, elements)).ToList();

            //List<Point> result = new List<Point>();

            //for (int i = 0; i < Size * Size; i++)
            //{
            //    Point pt = LengthXY.GetXY(i);

            //    if (IsAt(pt, element))
            //    {
            //        result.Add(pt);
            //    }
            //}

            //return result;
        }

        public List<Point> GetWalls()
        {
            return Get(Element.WALL);
        }

        public List<Point> GetDestroyableWalls()
        {
            return Get(Element.DESTROYABLE_WALL);
        }

        private static Element[] BombIndicators = new[]
        {
            Element.BOMB_TIMER_1, 
            Element.BOMB_TIMER_2,
            Element.BOMB_TIMER_3, 
            Element.BOMB_TIMER_4, 
            Element.BOMB_TIMER_5, 
            Element.BOMB_BOMBERMAN,
            Element.OTHER_BOMB_BOMBERMAN
        };

        private static Element[] BombPotentialIndicators = BombIndicators.Concat(new[] {Element.MEAT_CHOPPER}).ToArray();

        public bool IsBombAt(Point location)
        {
            return IsAnyOfAt(location, BombIndicators);
        }

        private static Element[] PerkIndicators = new[]
        {
            Element.BOMB_COUNT_INCREASE,
            Element.BOMB_BLAST_RADIUS_INCREASE,
            Element.BOMB_REMOTE_CONTROL,
            Element.BOMB_IMMUNE
        };

        public bool IsPerkAt(Point location)
        {
            return IsAnyOfAt(location, PerkIndicators);
        }

        public bool CanBombBeAt(Point location)
        {
            return IsAnyOfAt(location, BombPotentialIndicators);
        }

        public List<Point> GetBombs()
        {
            return Get(Element.BOMB_TIMER_1)
                .Concat(Get(Element.BOMB_TIMER_2))
                .Concat(Get(Element.BOMB_TIMER_3))
                .Concat(Get(Element.BOMB_TIMER_4))
                .Concat(Get(Element.BOMB_TIMER_5))
                .Concat(Get(Element.BOMB_BOMBERMAN))
                .Concat(Get(Element.OTHER_BOMB_BOMBERMAN))
                .ToList();
        }

        public List<Point> GetPerks()
        {
            return Get(Element.BOMB_BLAST_RADIUS_INCREASE)
                .Concat(Get(Element.BOMB_COUNT_INCREASE))
                .Concat(Get(Element.BOMB_IMMUNE))
                .Concat(Get(Element.BOMB_REMOTE_CONTROL))
                .ToList();
        }

        public List<Point> GetBlasts()
        {
            return Get(Element.BOOM);
        }

        public List<Point> GetFutureBlasts()
        {
            var bombs = GetBombs();
            var result = new List<Point>();
            foreach (var bomb in bombs)
            {
                result.Add(bomb);
                result.Add(bomb.ShiftLeft());
                result.Add(bomb.ShiftRight());
                result.Add(bomb.ShiftUp());
                result.Add(bomb.ShiftDown());
            }

            return result.Where(blast => !blast.IsOutOf(Size) && !GetWalls().Contains(blast)).Distinct().ToList();
        }

        public bool IsAnyOfAt(Point point, params Element[] elements)
        {
            return elements.Contains(GetAt(point));
        }

        public bool IsNear(Point point, Element element)
        {
            if (point.IsOutOf(Size))
                return false;

            return IsAt(point.ShiftLeft(),   element) ||
                   IsAt(point.ShiftRight(),  element) ||
                   IsAt(point.ShiftUp(),    element) ||
                   IsAt(point.ShiftDown(), element);
        }

        private static Element[] Barriers = new[] {Element.WALL, Element.DESTROYABLE_WALL}
            .Concat(BombIndicators)
            .Concat(PotentialMeatchoppers)
            .Concat(OtherBombermans)
            .ToArray();

        public bool IsBarrierAt(Point point)
        {
            return IsAnyOfAt(point, Barriers);
            //return GetBarrier().Contains(point);
        }

        public int CountNear(Point point, Element element)
        {
            if (point.IsOutOf(Size))
                return 0;

            int count = 0;
            if (IsAt(point.ShiftLeft(),   element)) count++;
            if (IsAt(point.ShiftRight(),  element)) count++;
            if (IsAt(point.ShiftUp(),    element)) count++;
            if (IsAt(point.ShiftDown(), element)) count++;
            return count;
        }

        private static Element[] BlastStoppers = new[] {Element.WALL, Element.DESTROYABLE_WALL};

        public bool IsBlastStopperAt(Point point)
        {
            return IsAnyOfAt(point, BlastStoppers);
        }

        public bool IsDestroyableBlastStopperAt(Point point)
        {
            return IsAt(point, Element.DESTROYABLE_WALL);
        }

        public bool IsNonDestroyableBlastStopperAt(Point point)
        {
            return IsAt(point, Element.WALL);
        }
    }
}
