using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Utils.RTree
{
    public class Node : INode
    {
        internal Rectangle mbr = null;
        internal INode[] entries = null;
        private int entryCount = 0;
        private int level;

        public INode[] Entries
        { get { return this.entries; } }

        public bool IsLeaf
        { get { return this.level == 1; } }

        public int Level
        { get { return this.level; } }

        public int Count
        { get { return this.entryCount; } }

        public Node(int nodeId, int level)
        {
            this.nodeId = nodeId;
            this.level = level;
            this.entries = new INode[RTree.MAXENTRIES];
        }

        public void Add(INode n)
        {
            this.entries[this.entryCount] = n;
            this.entryCount++;
            if (this.mbr == null)
            {
                if (n is Point)
                {
                    var obj = n as Point;
                    this.mbr = new Rectangle(obj.coordinates, obj.coordinates);
                }
                else
                {
                    var obj = n as Node;
                    this.mbr = obj.mbr.Copy();
                }
            }
            else
                this.RecalculateMBR();
        }

        public override float Enlargement(INode n)
        {
            if (n is Point)
                return this.Enlargement(n as Point);
            else if (n is Node)
                return this.Enlargement(n as Node);
            return float.NaN;
        }

        public bool Contains(Point p)
        {
            for (int i = 0; i < Count; i++)
            {
                var obj = this.entries[i] as Point;
                if (obj.Equals(p))
                    return true;
            }
            return false;
        }

        public Point GetPoint(Point p)
        {
            for (int i = 0; i < Count; i++)
            {
                var obj = this.entries[i] as Point;
                if (obj.Equals(p))
                    return obj;
            }
            return null;
        }

        private float Enlargement(Point p)
        {
            var enlargement = 1f;
            var area = 1f;
            for (int i = 0; i < RTree.DIM; i++)
            {
                enlargement *= (Math.Max(this.mbr.max[i], p.coordinates[i]) - Math.Min(this.mbr.min[i], p.coordinates[i]));
                area *= (this.mbr.max[i] - this.mbr.min[i]);
            }
            return enlargement - area;
        }

        private float Enlargement(Node n)
        {
            var enlargement = 1f;
            var area = 1f;
            for (int i = 0; i < RTree.DIM; i++)
            {
                enlargement *= (Math.Max(this.mbr.max[i], n.mbr.max[i]) - Math.Min(this.mbr.min[i], n.mbr.min[i]));
                area *= (this.mbr.max[i] - this.mbr.min[i]);
            }
            return enlargement - area;
        }

        public override float Distance(Point p)
        {
            float distance = 0;
            for (int i = 0; i < RTree.DIM; i++)
            {
                var greatesMin = Math.Max(this.mbr.min[i], p.coordinates[i]);
                var leastMax = Math.Min(this.mbr.max[i], p.coordinates[i]);
                if (greatesMin > leastMax)
                    distance += ((greatesMin - leastMax) * (greatesMin - leastMax));
            }
            return (float)Math.Sqrt(distance);
        }

        public override float Area()
        {
            var area = 1f;
            for (int i = 0; i < RTree.DIM; i++)
                area *= (this.mbr.max[i] - this.mbr.min[i]);
            return area;
        }

        public void Clear()
        {
            this.entries = new INode[RTree.MAXENTRIES];
            this.entryCount = 0;
            this.mbr = null;
        }

        private void RecalculateMBR()
        {
            for (int i = 0; i < Count; i++)
            {
                if (IsLeaf)
                {
                    for (int d = 0; d < RTree.DIM; d++)
                    {
                        var obj = this.entries[i] as Point;
                        this.mbr.max[d] = Math.Max(this.mbr.max[d], obj.coordinates[d]);
                        this.mbr.min[d] = Math.Min(this.mbr.min[d], obj.coordinates[d]);
                    }
                }
                else
                {
                    for (int d = 0; d < RTree.DIM; d++)
                    {
                        var obj = this.entries[i] as Node;
                        this.mbr.max[d] = Math.Max(this.mbr.max[d], obj.mbr.max[d]);
                        this.mbr.min[d] = Math.Min(this.mbr.min[d], obj.mbr.min[d]);
                    }
                }
            }
        }
    }
}
