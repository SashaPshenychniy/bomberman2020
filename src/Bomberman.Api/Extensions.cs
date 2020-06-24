using System;
using System.Text;

namespace Bomberman.Api
{
    public static class Extensions
    {
        public static int AsInt(this bool b)
        {
            return b ? 1 : 0;
        }

        public static string ToLogStr<T>(this T[,] m, Func<T, string> itemToString, int itemMaxLen)
        {
            var rowsCount = m.GetLength(0);
            var columnsCount = m.GetLength(1);

            var sb = new StringBuilder();
            sb.Append("   ");

            for (int c = 0; c < columnsCount; c++)
            {
                sb.Append($"{c.ToString().PadLeft(itemMaxLen + 1)}");
            }

            sb.AppendLine();

            for (int r = 0; r < rowsCount; r++)
            {
                sb.Append($"{r.ToString().PadLeft(2)}:");
                for (int c = 0; c < columnsCount; c++)
                {
                    sb.Append($"{itemToString(m[r, c]).PadLeft(itemMaxLen + 1)}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
