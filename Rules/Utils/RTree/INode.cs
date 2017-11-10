using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Utils.RTree
{
    public abstract class INode
    {
        internal int nodeId;

        public int NodeId
        {
            get { return this.nodeId; }
            internal set { this.nodeId = value; }
        }

        public abstract float Enlargement(INode n);

        public abstract float Area();

        public abstract float Distance(Point p);
    }
}
