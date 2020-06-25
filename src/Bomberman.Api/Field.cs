using System;
using System.CodeDom;
using System.Linq;
using System.Text;

namespace Bomberman.Api
{
    public class Field
    {
        public readonly int RowsCount;
        public readonly int ColumnsCount;

        public readonly double[,] Values;

        public Field(int rowsCount, int columnsCount)
        {
            RowsCount = rowsCount;
            ColumnsCount = columnsCount;
            Values = new double[rowsCount, columnsCount];
        }

        public Field Clone()
        {
            var c = new Field(RowsCount, ColumnsCount);
            Array.Copy(Values, c.Values, c.Values.Length);
            return c;
        }

        public void Add(Field f)
        {
            for (int r = 0; r < RowsCount; r++)
            {
                for (int c = 0; c < ColumnsCount; c++)
                {
                    Values[r, c] += f.Values[r, c];
                }
            }
        }

        public void AddWithMultiplier(Field f, double multiplier)
        {
            for (int r = 0; r < RowsCount; r++)
            {
                for (int c = 0; c < ColumnsCount; c++)
                {
                    Values[r, c] += f.Values[r, c] * multiplier;
                }
            }
        }

        public void AddWithAggregator(Field f, Func<double, double, double> aggregator)
        {
            for (int r = 0; r < RowsCount; r++)
            {
                for (int c = 0; c < ColumnsCount; c++)
                {
                    Values[r, c] = aggregator(Values[r, c], f.Values[r, c]);
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("   ");
            for (int c = 0; c < ColumnsCount; c++)
            {
                sb.Append($"{c,7:D}");
            }

            sb.AppendLine();

            for (int r = 0; r < RowsCount; r++)
            {
                sb.Append($"{r,2}: ");
                for (int c = 0; c < ColumnsCount; c++)
                {
                    sb.Append($"{Values[r, c],6:###.00}");
                    sb.Append(" ");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public class PredictionField
    {
        public int FieldRowsCount { get; }
        public int FieldColumnsCount { get; }
        public Field[] Turn { get; private set; }

        public PredictionField(int movesCountToPredict, int fieldRowsCount, int fieldColumnsCount)
        {
            FieldRowsCount = fieldRowsCount;
            FieldColumnsCount = fieldColumnsCount;
            Turn = new Field[movesCountToPredict + 1];
            for (int i = 0; i <= movesCountToPredict; i++)
            {
                Turn[i] = new Field(fieldRowsCount, fieldColumnsCount);
            }
        }

        public PredictionField Clone()
        {
            return new PredictionField(Turn.Length - 1, FieldRowsCount, FieldColumnsCount) {Turn = Turn.Select(t => t.Clone()).ToArray()};
        }

        public void Clear()
        {
            foreach (var t in Turn)
            {
                Array.Clear(t.Values, 0, t.Values.Length);
            }
        }

        public void Add(PredictionField f)
        {
            for (int t = 0; t < Turn.Length; t++)
            {
                Turn[t].Add(f.Turn[t]);
            }
        }

        public void AddWithMultiplier(PredictionField f, double multiplier)
        {
            for (int t = 0; t < Turn.Length; t++)
            {
                Turn[t].AddWithMultiplier(f.Turn[t], multiplier);
            }
        }

        public void AddWithAggregator(PredictionField f, Func<double, double, double> aggregator)
        {
            for (int t = 0; t < Turn.Length; t++)
            {
                Turn[t].AddWithAggregator(f.Turn[t], aggregator);
            }
        }

        public void AddWithMultiplier(PredictionField f, int startTurn, int endTurn, double multiplier)
        {
            for (int t = startTurn; t <= endTurn; t++)
            {
                Turn[t].AddWithMultiplier(f.Turn[t], multiplier);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            for (int t = 0; t < Turn.Length; t++)
            {
                sb.AppendLine($"TURN {t}:");
                sb.AppendLine(Turn[t].ToString());
            }

            return sb.ToString();
        }
    }
}
