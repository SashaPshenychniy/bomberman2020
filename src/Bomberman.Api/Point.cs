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

namespace Bomberman.Api
{
    public struct Point
    {
        public readonly int X;
        public readonly int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Checks is current point on board or out of range.
        /// </summary>
        /// <param name="size">Board size to comapre</param>
        public bool IsOutOf(int size)
        {
            return X >= size || Y >= size || X < 0 || Y < 0;
        }

        /// <summary>
        /// Returns new BoardPoint object shifted left to "delta" points
        /// </summary>
        public Point ShiftLeft(int delta = 1)
        {
            return new Point(X - delta, Y);
        }

        /// <summary>
        /// Returns new BoardPoint object shifted right to "delta" points
        /// </summary>
        public Point ShiftRight(int delta = 1)
        {
            return new Point(X + delta, Y);
        }

        /// <summary>
        /// Returns new BoardPoint object shifted top "delta" points
        /// </summary>
        public Point ShiftUp(int delta = 1)
        {
            return new Point(X, Y - delta);
        }

        /// <summary>
        /// Returns new BoardPoint object shifted bottom "delta" points
        /// </summary>
        public Point ShiftDown(int delta = 1)
        {
            return new Point(X, Y + delta);
            
        }

        public Point Shift(Move direction, int distance = 1)
        {
            switch (direction)
            {
                case Move.Act:
                case Move.Stop:
                    return this;
                case Move.Left:
                    return ShiftLeft(distance);
                case Move.Right:
                    return ShiftRight(distance);
                case Move.Up:
                    return ShiftUp(distance);
                case Move.Down:
                    return ShiftDown(distance);
                default:
                    throw new NotImplementedException($"Move '{direction}' not recognized");
            }
        }

        public Move GetShiftDirectionTo(Point newPoint)
        {
            if (newPoint == this)
            {
                return Move.Stop;
            }

            if (newPoint.Y < Y)
            {
                return Move.Up;
            }
            if (newPoint.Y > Y)
            {
                return Move.Down;
            }
            if (newPoint.X < X)
            {
                return Move.Left;
            }
            if (newPoint.X > X)
            {
                return Move.Right;
            }

            throw new NotImplementedException();
        }

        public static bool operator ==(Point p1, Point p2)
        {
            if (ReferenceEquals(p1, p2))
                return true;

            if (ReferenceEquals(p1, null) || ReferenceEquals(p2, null))
                return false;

            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public static bool operator !=(Point p1, Point p2)
        {
            return !(p1 == p2);
        }

        public override string ToString()
        {
            return String.Format("[{0},{1}]", Y, X);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is Point)) return false;

            Point that = (Point)obj;

            return that.X == this.X && that.Y == this.Y;
        }

        public override int GetHashCode()
        {
            return Y.GetHashCode() * 100 + X.GetHashCode();
        }
    }

    public class PointEqualityComparer : IEqualityComparer<Point>
    {
        public static PointEqualityComparer Instance { get; } = new PointEqualityComparer();

        public bool Equals(Point x, Point y)
        {
            return x == y;
        }

        public int GetHashCode(Point obj)
        {
            return obj.GetHashCode();
        }
    }
}
