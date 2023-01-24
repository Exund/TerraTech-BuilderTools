using System;
using System.Collections;
using System.Collections.Generic;

namespace BuilderTools
{
    public class IntVector3Interval
    {
        private IntVector3 start;
        private IntVector3 end;

        private int maxAxis;
        private int maxAxisDiff;
        private int maxAxisDiffAbsolute;
        private int maxAxisDir;
        private int maxAxisStart;
        private int maxAxisEnd;

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

        public int MaxAxis => maxAxis;

        public int MaxAxisDiff => maxAxisDiff;

        public int MaxAxisDiffAbsolute => maxAxisDiffAbsolute;

        public int MaxAxisDir => maxAxisDir;

        public int MaxAxisStart => maxAxisStart;

        public int MaxAxisEnd => maxAxisEnd;

        public IntVector3Interval()
        {
            this.start = this.end = IntVector3.zero;
        }

        public IntVector3Interval(IntVector3 start, IntVector3 end, bool calc = true)
        {
            this.start = start;
            this.end = end;

            if (calc)
            {
                Recalc();
            }
        }

        private void Recalc()
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
            maxAxisDiff = max;
            maxAxisDiffAbsolute = Math.Abs(max);
            maxAxisDir = Math.Sign(max);
            maxAxisStart = start[maxi];
            maxAxisEnd = end[maxi];
        }

        public void IteratePositions(Action<IntVector3, int> action, bool skipFirst = true, bool includeEnd = false)
        {
            var inc = this.MaxAxisDir;
            var start = this.MaxAxisStart + (skipFirst ? inc : 0);
            var end = this.MaxAxisEnd + (includeEnd ? inc : 0);
            var axis = this.MaxAxis;

            var startPos = this.Start;

            for (int i = start; i != end; i += inc)
            {
                var pos = startPos;
                pos[axis] = i;

                action(pos, Math.Abs(i - start));
            }
        }
    }

    public class IntVector3Interval_ : IEnumerable<IntVector3>
    {
        public enum IterationType
        {
            MaxAxis,
            Diagonal,
            Lines
        }

        public enum Axis
        {
            X,
            Y,
            Z
        }

        private int[] axisOrder = new int[] { 0, 1, 2 };

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

        private IterationType Type
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

        public IntVector3Interval_()
        {
            this.start = this.end = IntVector3.zero;
        }

        public IntVector3Interval_(IntVector3 start, IntVector3 end)
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
                case IterationType.Diagonal:
                    break;
                case IterationType.Lines:
                    RecalcLines();
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
            size = max;
        }

        private void RecalcLines()
        {
            var diff = end - start;
            size = Math.Abs(diff.x) + Math.Abs(diff.y) + Math.Abs(diff.z) - 2;
        }

        private IEnumerable<IntVector3> MaxAxis(bool skipFirst, bool includeEnd)
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

        private IEnumerable<IntVector3> Lines(bool skipFirst, bool includeEnd)
        {
            var current = start;
            for (int a = 0; a < 3; a++)
            {
                var axis = axisOrder[a];
                var axis_diff = (end - start)[axis];
                var axis_dir = Math.Sign(axis_diff);

                int i = (a == 0 && skipFirst ? axis_dir : 0);

                if (includeEnd && axis == 2)
                {
                    axis_diff += axis_dir;
                }

                var pos = current;
                for (; i != axis_diff; i += axis_dir)
                {
                    pos = current;
                    pos[axis] = i;

                    yield return pos;
                }

                current = pos;
            }
        }

        public IEnumerator<IntVector3> GetEnumerator(bool skipFirst = true, bool includeEnd = false)
        {
            switch (type)
            {
                case IterationType.MaxAxis:
                    return MaxAxis(skipFirst, includeEnd).GetEnumerator();
                case IterationType.Diagonal:
                    return null;
                case IterationType.Lines:
                    return Lines(skipFirst, includeEnd).GetEnumerator();
                default:
                    return null;
            }
        }

        public IEnumerator<IntVector3> GetEnumerator()
        {
            return GetEnumerator(true, false);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
