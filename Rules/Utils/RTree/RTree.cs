using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DBLint.Rules.Utils.RTree
{
    public enum Dimensions
    {
        Two = 2,
        Three = 3
    }

    public class RTree
    {
        public static Dimensions Diemensions;
        public static int DIM;
        public static int MAXENTRIES;

        private int nextNodeId = 0;
        private int treeHight = 1;
        private int leafCount = 0;
        private int nodeCount = 1;
        private Node root;

        public Dictionary<Point, Point> leafList = new Dictionary<Point, Point>();
        private Dictionary<int, Point> leafs = new Dictionary<int, Point>();
        public Dictionary<int, Point> Leafs
        { get { return this.leafs; } }

        public int TreeHeight
        { get { return this.treeHight; } }

        public int LeafCount
        { get { return this.leafCount; } }

        public int NodeCount
        { get { return this.nodeCount; } }

        public Node Root
        { get { return this.root; } }

        public RTree(Dimensions diemensions, int maxEntries)
        {
            Diemensions = diemensions;
            DIM = (int)diemensions;
            MAXENTRIES = maxEntries;
            this.root = new Node(this.GetNextNodeId(), this.treeHight);
        }

        public Point Insert(Point p)
        {
            var result = p;
            this.leafCount++;

            if (this.leafList.ContainsKey(p))
            {
                result = this.leafList[p];
                result.Increment();
            }
            else
            {
                p.nodeId = this.GetNextNodeId();
                this.leafs.Add(result.NodeId, result);
                this.leafList.Add(result, result);
                
                var n = this.ChooseLeaf(p);
                Node newLeaf = null;
                if (n.Count < MAXENTRIES)
                    n.Add(p);
                else
                    newLeaf = this.SplitNode(n, p);
                var newNode = this.AdjustTree(n, newLeaf);
                if (newNode != null)
                {
                    this.treeHight++;
                    var newRoot = new Node(this.GetNextNodeId(), this.treeHight);
                    newRoot.Add(this.root);
                    newRoot.Add(newNode);
                    this.root = newRoot;
                }
            }
            return result;
        }

        public List<Point> NearestNeighbours(Point p, int number)
        {
            this.NearestNeighbourCalls = 0;
            var res = this.GetNeighbours(this.root, p, number);
            return res.Select(i => i.Node as Point).ToList();
        }

        public List<Point> GetSurrounding(Point p, float distance)
        {
            return this.GetSurrounding(this.root, p, distance).Select(i => i as Point).ToList();
        }

        private List<INode> GetSurrounding(Node n, Point p, float distance)
        {
            var list = new List<INode>();
            for (int i = 0; i < n.Count; i++)
            {
                if (n.entries[i].Distance(p) <= distance)
                {
                    if (n.IsLeaf)
                        list.Add(n.entries[i]);
                    else
                        list.AddRange(GetSurrounding(n.entries[i] as Node, p, distance));
                }
            }
            return list;
        }

        private bool useDistinceValues = false;

        public int NearestNeighbourCalls = 0;

        private List<OrderedList> GetNeighbours(Node n, Point p, int number)
        {
            this.NearestNeighbourCalls++;
            var list = new List<OrderedList>();
            var result = new List<OrderedList>();
            for (int i = 0; i < n.Count; i++)
            {
                list.Add(new OrderedList(n.entries[i].Distance(p), n.entries[i]));
            }
            list.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            if (n.IsLeaf)
            {
                if (useDistinceValues)
                {
                    var newRes = new List<OrderedList>();
                    foreach (var item in list)
                    {
                        var obj = item.Node as Point;
                        var itMax = obj.Equals(p) ? obj.Count - 1 : obj.Count;
                        for (int i = 0; i < itMax; i++)
                        {
                            newRes.Add(item);
                        }
                    }
                    var max = Math.Min(number, newRes.Count);
                    return newRes.GetRange(0, max);
                }
                else
                {
                    list = list.Where(l => !l.Node.Equals(p)).ToList();
                    var max = Math.Min(number, list.Count());
                    return list.GetRange(0, max);
                }
            }
            else
            {
                foreach (var item in list)
                {
                    if (result.Count == number && result[number - 1].Distance < item.Distance)
                        break;
                    result.AddRange(GetNeighbours(item.Node as Node, p, number));
                    result.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                    result = result.GetRange(0, Math.Min(result.Count, number));
                }
            }
            return result;
        }

        private Node AdjustTree(Node n, Node nn)
        {
            while (n.Level != this.treeHight)
            {
                var parent = this.parents.Pop();
                var entryId = this.entryIndex.Pop();
                Node newNode = null;
                if (nn != null)
                {
                    if (parent.Count < MAXENTRIES)
                        parent.Add(nn);
                    else
                        newNode = this.SplitNode(parent, nn);
                }
                n = parent;
                nn = newNode;
            }
            return nn;
        }

        private Stack<Node> parents = new Stack<Node>();
        private Stack<int> entryIndex = new Stack<int>();

        private Node ChooseLeaf(Point p)
        {
            // CL1 []
            var n = this.root;
            parents.Clear();
            entryIndex.Clear();
            while (true)
            {
                // CL2 []
                if (n.IsLeaf)
                    return n;
                var leastEnlargemnt = float.MaxValue;
                var index = -1;
                for (int i = 0; i < n.Count; i++)
                {
                    var tmpEnlargemnt = n.entries[i].Enlargement(p);
                    // CL3 []
                    if ((tmpEnlargemnt < leastEnlargemnt) ||
                        (tmpEnlargemnt == leastEnlargemnt &&
                        n.entries[i].Area() < n.entries[index].Area()))
                    {
                        leastEnlargemnt = tmpEnlargemnt;
                        index = i;
                    }
                }
                parents.Push(n);
                entryIndex.Push(index);
                // CL4 []
                n = n.entries[index] as Node;
            }
        }

        private Node SplitNode(Node n, INode newEntry)
        {
            this.nodeCount++;
            // The new node created by the split
            var newNode = new Node(this.GetNextNodeId(), n.Level);
            // A list of all the entries that must be distibuted
            var allEntries = n.entries.ToList();
            allEntries.Add(newEntry);
            n.Clear();

            int e1, e2;
            // QS1 []
            this.PickSeed(allEntries, out e1, out e2);
            n.Add(allEntries[e1]);
            newNode.Add(allEntries[e2]);
            allEntries.RemoveAt(Math.Max(e1, e2));
            allEntries.RemoveAt(Math.Min(e1, e2));
            // QS2 []
            while (allEntries.Count > 0)
            {
                // QS3 [Select entry to assign]
                var nextId = this.PickNext(allEntries, n, newNode);
                var entry = allEntries[nextId];
                var lEnlargement = n.Enlargement(entry);
                var llEnlargement = newNode.Enlargement(entry);
                // QS3 - a: choose the node that needs the least enlargement, if tie go to QS3 - b
                if (lEnlargement < llEnlargement)
                    n.Add(entry);
                else if (llEnlargement < lEnlargement)
                    newNode.Add(entry);
                else
                {
                    // QS3 - b: choose the node with the smallest area, if tie go to QS3 - c
                    var lArea = n.Area();
                    var llArea = newNode.Area();
                    if (lArea < llArea)
                        n.Add(entry);
                    else if (llArea < lArea)
                        newNode.Add(entry);
                    else
                    {
                        // QS3 - c: choose the node with the lowest number of entries, if tie go to QS3 - d
                        var lElements = n.Count;
                        var llElements = newNode.Count;
                        if (lElements < llElements)
                            n.Add(entry);
                        else if (llElements < lElements)
                            newNode.Add(entry);
                        else
                            n.Add(entry); // QS3 - d: Just insert into one of the nodes
                    }
                }
                allEntries.RemoveAt(nextId);
            }
            return newNode;
        }

        private void PickSeed(List<INode> entries, out int e1, out int e2)
        {
            var d = float.MinValue;
            e1 = -1;
            e2 = -1;
            // PS1 []
            for (int i = 0; i < entries.Count - 1; i++)
            {
                var tmpAreaE1 = entries[i].Area();
                for (int j = i + 1; j < entries.Count; j++)
                {
                    var tmpAreaE2 = entries[j].Area();
                    // PS1 - calculatation
                    var tmpd = entries[i].Enlargement(entries[j]) - tmpAreaE1 - tmpAreaE2;
                    // PS2 []
                    if (tmpd > d)
                    {
                        d = tmpd;
                        e1 = i;
                        e2 = j;
                    }
                }
            }
        }

        private int PickNext(List<INode> entries, Node n, Node nn)
        {
            var d = float.MinValue;
            var index = -1;
            // PN1 []
            for (int i = 0; i < entries.Count; i++)
            {
                var tmpD1 = n.Enlargement(entries[i]);
                var tmpD2 = nn.Enlargement(entries[i]);
                // PN1 - Calculate difference
                var tmpD = Math.Abs(tmpD1 - tmpD2);
                // PN2 []
                if (tmpD > d)
                {
                    d = tmpD;
                    index = i;
                }
            }
            return index;
        }

        private int GetNextNodeId()
        {
            var r = this.nextNodeId;
            this.nextNodeId++;
            return r;
        }
    }

    public class OrderedList
    {
        public float Distance { get; private set; }
        public INode Node { get; private set; }

        public OrderedList(float distance, INode node)
        {
            this.Distance = distance;
            this.Node = node;
        }
    }
}
