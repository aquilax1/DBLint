using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Utils.RTree
{
    public class Rectangle
    {
        internal float[] min;
        internal float[] max;

        public Rectangle()
        {
            this.min = new float[RTree.DIM];
            this.max = new float[RTree.DIM];
        }

        public Rectangle(float[] min, float[] max)
            : this()
        {
            Array.Copy(min, 0, this.min, 0, RTree.DIM);
            Array.Copy(max, 0, this.max, 0, RTree.DIM);
        }

        public Rectangle Copy()
        {
            return new Rectangle(this.min, this.max);
        }
    }
}
