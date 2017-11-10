using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Rules.Utils.RTree;

namespace DBLint.Rules.OutlierDetection
{
    public class OutlierAnalysis
    {
        private RTree rTree = null;
        private int k;
        public Dictionary<int, OutlierData> leafs;

        public RTree RTree
        { get { return this.rTree; } }

        public OutlierAnalysis(Dimensions diemensions, int maxEntries, int k)
        {
            this.k = k;
            this.rTree = new RTree(diemensions, maxEntries);
            this.leafs = new Dictionary<int, OutlierData>();
        }

        public void Insert(Point p, int rowNumber)
        {
            var res = this.rTree.Insert(p);
            if (!leafs.ContainsKey(res.NodeId))
                leafs.Add(res.NodeId, new OutlierData());
            leafs[res.NodeId].AddRowNumber(rowNumber);
        }

        public void Insert(Point p, string value)
        {
            var res = this.rTree.Insert(p);
            if (!leafs.ContainsKey(res.NodeId))
                leafs.Add(res.nodeId, new OutlierData());
            leafs[res.nodeId].AddRowValue(value);
        }

        public void Insert(Point p)
        {
            var res = this.rTree.Insert(p);
            if (!leafs.ContainsKey(res.NodeId))
                leafs.Add(res.nodeId, new OutlierData());
        }

        public void SetNeighbourhoods()
        {
            foreach (var node in this.rTree.Leafs)
            {
                this.KDistance(node.Value);
                //this.leafs[node.Value.NodeId].Neighbours = this.rTree.GetSurrounding(node.Value, k);
            }
        }

        public float KDistance(Point p)
        {
            if (this.leafs[p.NodeId].KDistance == null)
            {
                var neighbours = this.rTree.NearestNeighbours(p, this.k - 1);
                this.leafs[p.NodeId].Neighbours = neighbours;
                this.leafs[p.NodeId].NearestNeighbourCalls = this.rTree.NearestNeighbourCalls;
                float maxDistance = float.MinValue;
                for (int i = 0; i < neighbours.Count; i++)
                {
                    var tmpDist = p.Distance(neighbours[i]);
                    if (tmpDist > maxDistance)
                        maxDistance = tmpDist;
                }
                this.leafs[p.NodeId].KDistance = maxDistance;
            }
            return (float)this.leafs[p.NodeId].KDistance;
        }

        public float ReachDistance(Point p, Point o)
        {
            return Math.Max(KDistance(o), p.Distance(o));
        }

        public List<Point> KDistanceNeighborhood(Point p)
        {
            this.SetNeighbours(p);
            return this.leafs[p.NodeId].Neighbours;
        }

        public float LRD(Point p)
        {
            if (this.leafs[p.NodeId].LRD == null)
            {
                this.SetNeighbours(p);
                var reac = 0f;
                foreach (var o in this.leafs[p.NodeId].Neighbours)
                {
                    var max = o.Equals(p) ? o.Count - 1 : o.Count;
                    reac += (this.ReachDistance(p, o) * max);
                }
                this.leafs[p.NodeId].LRD = 1f / (reac / (float)this.leafs[p.NodeId].NeighbourhoodCount);
            }
            return (float)this.leafs[p.NodeId].LRD;
        }

        public float LOF(Point p)
        {
            if (this.leafs[p.NodeId].LOF == null)
            {
                this.SetNeighbours(p);
                var reac = 0f;
                foreach (var o in this.leafs[p.NodeId].Neighbours)
                {
                    var max = o.Equals(p) ? o.Count - 1 : o.Count;
                    reac += (this.LRD(o) / this.LRD(p)) * max;
                }
                this.leafs[p.NodeId].LOF = reac / (float)this.leafs[p.NodeId].NeighbourhoodCount;
            }
            return (float)this.leafs[p.NodeId].LOF;
        }

        public float DirectMin(Point p)
        {
            var pObj = this.leafs[p.NodeId];
            if (pObj.DirectMin == null)
                this.SetDirectValues(p);
            return (float)pObj.DirectMin;
        }

        public float DirectMax(Point p)
        {
            var pObj = this.leafs[p.NodeId];
            if (pObj.DirectMax == null)
                this.SetDirectValues(p);
            return (float)pObj.DirectMax;
        }

        public float IndirectMin(Point p)
        {
            var pObj = this.leafs[p.NodeId];
            if (pObj.IndirectMin == null)
                this.SetIndirectValues(p);
            return (float)pObj.IndirectMin;
        }

        public float IndirectMax(Point p)
        {
            var pObj = this.leafs[p.NodeId];
            if (pObj.IndirectMax == null)
                this.SetIndirectValues(p);
            return (float)pObj.IndirectMax;
        }

        private void SetIndirectValues(Point p)
        {
            var pObj = this.leafs[p.NodeId];
            this.SetNeighbours(p);
            var minDist = float.MaxValue;
            var maxDist = float.MinValue;
            foreach (var q in pObj.Neighbours)
            {
                this.SetNeighbours(q);
                var qObj = this.leafs[q.NodeId];
                foreach (var o in qObj.Neighbours)
                {
                    var tmpDist = this.ReachDistance(q, o);
                    if (tmpDist < minDist)
                        minDist = tmpDist;
                    if (tmpDist > maxDist)
                        maxDist = tmpDist;
                }
            }
            pObj.IndirectMin = minDist;
            pObj.IndirectMax = maxDist;
        }

        private void SetDirectValues(Point p)
        {
            var pObj = this.leafs[p.NodeId];
            this.SetNeighbours(p);
            var minDist = float.MaxValue;
            var maxDist = float.MinValue;
            foreach (var q in pObj.Neighbours)
            {
                var tmpDist = this.ReachDistance(p, q);
                if (tmpDist < minDist)
                    minDist = tmpDist;
                if (tmpDist > maxDist)
                    maxDist = tmpDist;
            }
            pObj.DirectMin = minDist;
            pObj.DirectMax = maxDist;
        }

        private void SetNeighbours(Point p)
        {
            if (this.leafs[p.NodeId].KDistance == null)
                this.leafs[p.NodeId].KDistance = this.KDistance(p);

            if (this.leafs[p.NodeId].Neighbours == null)
                this.leafs[p.NodeId].Neighbours = this.rTree.GetSurrounding(p, (float)this.leafs[p.NodeId].KDistance);
        }
    }
}
