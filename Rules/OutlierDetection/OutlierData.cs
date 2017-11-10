using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Rules.Utils.RTree;

namespace DBLint.Rules.OutlierDetection
{
    public class OutlierData
    {
        //public Point Data { get; internal set; }
        public List<int> RowNumbers { get; internal set; }
        public List<string> RowValues { get; internal set; }
        private List<Point> neighbours = null;
        private int? neighbourCount = null;
        public float? KDistance { get; internal set; }
        public float? LRD { get; internal set; }
        public float? LOF { get; internal set; }
        public float? DirectMax { get; internal set; }
        public float? DirectMin { get; internal set; }
        public float? IndirectMax { get; internal set; }
        public float? IndirectMin { get; internal set; }

        public int NearestNeighbourCalls = 0;

        public List<Point> Neighbours
        {
            get { return this.neighbours; }
            set { this.neighbours = value; }
        }

        public int NeighbourhoodCount
        {
            get
            {
                if (this.neighbourCount == null)
                {
                    var n = this.neighbours.Distinct();
                    this.neighbourCount = n.Sum(p => p.Count);
                }
                return (int)this.neighbourCount;
            }
        }

        public OutlierData()
        {
            //this.Data = data;
            this.RowNumbers = new List<int>();
            this.RowValues = new List<string>();
        }

        public void AddRowNumber(int rowNumber)
        {
            this.RowNumbers.Add(rowNumber);
        }

        public void AddRowValue(string value)
        {
            this.RowValues.Add(value);
        }

        public void SetNeighbour(Point neighbour)
        {
            this.Neighbours.Add(neighbour);
        }

        public void SetNeighbour(List<Point> neighbours)
        {
            this.neighbours = new List<Point>();
            this.Neighbours.AddRange(neighbours);
        }
    }
}
