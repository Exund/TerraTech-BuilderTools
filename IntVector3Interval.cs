using System;
using System.Collections;
using System.Collections.Generic;

namespace BuilderTools
{
    public class IntVector3Interval : IEnumerable<IntVector3>
    {
        public enum IterationType
        {
            MaxAxis,
            Lines3D,
            //Diagonal,
        }

        public enum Axis
        {
            X,
            Y,
            Z
        }

        private readonly int[] axisOrder = { 0, 1, 2 };

        private IntVector3 start;
        private IntVector3 end;

        private IterationType type = IterationType.MaxAxis;

        private int maxAxis;
        private int size;
        private int maxAxisDir = 1;

        public IntVector3 Start
        {
            get => start;
            set
            {
                if (start != value)
                {
                    start = value;
                    Recalc();
                }
            }
        }

        public IntVector3 End
        {
            get => end;
            set
            {
                if (end != value)
                {
                    end = value;
                    Recalc();
                }
            }
        }

        public IterationType Type
        {
            get => type;

            set
            {
                if (type != value)
                {
                    type = value;
                    Recalc();
                }
            }
        }
        
        public int Size => size;

        public IntVector3Interval()
        {
            this.start = this.end = IntVector3.zero;
        }

        public IntVector3Interval(IntVector3 start, IntVector3 end)
        {
            this.start = start;
            this.end = end;

            Recalc();
        }

        public void SetAxisOrder(Axis a, Axis b, Axis c)
        {
            axisOrder[0] = (int)a;
            axisOrder[1] = (int)b;
            axisOrder[2] = (int)c;
        }

        private void Recalc()
        {
            switch (type)
            {
                case IterationType.MaxAxis:
                    RecalcMaxAxis();
                    break;
                case IterationType.Lines3D:
                    RecalcLines();
                    break;
                default:
                    break;
            }
        }

        private void RecalcMaxAxis()
        {
            var maxi = 0;
            var max = 0;

            for (int i = 0; i < 3; i++)
            {
                var diff = end[i] - start[i];

                if (Math.Abs(diff) > Math.Abs(max))
                {
                    max = diff;
                    maxi = i;
                }
            }

            maxAxis = maxi;
            maxAxisDir = Math.Sign(max);
            size = Math.Abs(max);
        }

        private void RecalcLines()
        {
            var diff = end - start;
            size = 0;
            for (int i = 0; i < 3; i++)
            {
                size += Math.Abs(diff[i]);
            }
        }

        private IEnumerable<IntVector3> MaxAxis(bool skipFirst = true, bool includeEnd = false)
        {
            var startPos = this.start;

            var start = this.start[maxAxis] + (skipFirst ? maxAxisDir : 0);
            var end = this.end[maxAxis] + (includeEnd ? maxAxisDir : 0);

            for (int i = start; i != end; i += maxAxisDir)
            {
                var pos = startPos;
                pos[maxAxis] = i;

                yield return pos;
            }
        }

        private IEnumerable<IntVector3> Lines3D(bool skipFirst = true, bool includeEnd = false)
        {
            var currentStart = start;
            for (int a = 0; a < 3; a++)
            {
                var axis = axisOrder[a];
                IntVector3 temp = currentStart;
                Main.logger.Trace($"[Interval] Lines start: {currentStart}, end: {end}, axis: {axis}, a: {a}");
                foreach (var current in Line(currentStart, end, axis, (skipFirst && a == 0) || a != 0, (includeEnd && a == 2) || a != 2))
                {
                    temp = current;
                    yield return current;
                }
                currentStart = temp;
            }

        }

        public static IEnumerable<IntVector3> Line(IntVector3 start, IntVector3 end, int axis, bool skipFirst = true, bool includeEnd = false)
        {
            var diff = (end - start)[axis];
            var dir = Math.Sign(diff);

            if (dir == 0)
            {
                yield break;
            }

            if (includeEnd)
            {
                diff += dir;
            }

            var i = skipFirst ? dir : 0;
            for (; i != diff; i += dir)
            {
                var pos = start;
                pos[axis] += i;

                yield return pos;
            }
        }

        public IEnumerator<IntVector3> GetEnumerator()
        {
            switch (type)
            {
                case IterationType.MaxAxis:
                    return MaxAxis().GetEnumerator();
                case IterationType.Lines3D:
                    return Lines3D().GetEnumerator();
                default:
                    return (IEnumerator<IntVector3>)Array.Empty<IntVector3>().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
