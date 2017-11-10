using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Utils.RTree
{
    public class Point : INode
    {
        private int? hash = null;

        internal float[] coordinates;
        internal int count = 1;

        public float X { get { return this.coordinates[0]; } }
        public float Y { get { return this.coordinates[1]; } }
        public float Z { get { return this.coordinates[2]; } }

        public int Count
        { get { return this.count; } }

        public void Increment()
        {
            this.count++;
        }

        public Point()
        {
            this.coordinates = new float[RTree.DIM];
        }

        public Point(float x, float y) : this()
        {
            this.coordinates[0] = x;
            this.coordinates[1] = y;
        }

        public Point(float x, float y, float z)
            : this(x, y)
        {
            this.coordinates[2] = z;
        }

        public Point(float[] coordinates)
            : this()
        {
            Array.Copy(coordinates, 0, this.coordinates, 0, RTree.DIM);
        }

        public override float Enlargement(INode n)
        {
            if (n is Point)
            {
                var obj = n as Point;
                var enlargemnt = 1f;
                for (int i = 0; i < RTree.DIM; i++)
                    enlargemnt *= Math.Max(this.coordinates[i], obj.coordinates[i]) - Math.Min(this.coordinates[i], obj.coordinates[i]);
                return enlargemnt;
            }
            return float.NaN;
        }

        public override float Area()
        {
            return 0;
        }

        public override float Distance(Point p)
        {
            var distance = 0f;
            for (int i = 0; i < RTree.DIM; i++)
                distance += (float)Math.Pow((this.coordinates[i] - p.coordinates[i]), 2);
            return (float)Math.Sqrt(distance);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var s = obj as Point;
            return this.coordinates.SequenceEqual(s.coordinates);
        }

        public override int GetHashCode()
        {
            if (this.hash == null)
            {
                this.hash = 0;
                foreach (var val in this.coordinates)
                    this.hash += val.GetHashCode();
            }
            return (int)this.hash;
        }

        public override string ToString()
        {
            return String.Format("(x,y) : ({0},{1})", X, Y);
        }
    }
}
