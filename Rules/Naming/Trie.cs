using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Naming
{
    public interface ITrie<T, I> where T : struct
    {
        void Insert(I word);
        bool Contains(I word);
        int Frequency(I word);
        List<KeyValuePair<I, int>> FrequentWords(int percentage);
        TrieNode<T, I> Root { get; }
    }

    public class TrieNode<T, I> where T : struct
    {
        private readonly Dictionary<T, TrieNode<T, I>> _children = new Dictionary<T, TrieNode<T, I>>();
        private static readonly TrieNode<T, I>[] _empty = new TrieNode<T, I>[] { };
        private int _count = 0;
        private TrieNode<T, I> _parent = null;
        public T _nodeChar;
        private ITrieConverter<T, I> _converter;

        public int Count
        {
            get { return _count; }
        }

        public IEnumerable<TrieNode<T, I>> Children
        {
            get { return this._children.Values; }
        }

        public TrieNode(TrieNode<T, I> parent, T nodeChar, ITrieConverter<T, I> Converter)
        {
            _converter = Converter;
            _parent = parent;
            _nodeChar = nodeChar;
        }

        private List<T> _GetWord()
        {
            if (this._parent == null)
            {
                return new List<T>();
            }
            else
            {
                var sb = this._parent._GetWord();
                sb.Add(this._nodeChar);
                return sb;
            }
        }

        public List<T> GetWord()
        {
            var sb = this._GetWord();
            return sb;
        }

        public IEnumerable<TrieNode<T, I>> FrequentWords(int minCount)
        {
            if (this.Count > minCount)
            {

                var newlist = (from val in this._children.Values
                               let freqNodes = val.FrequentWords(minCount)
                               where freqNodes.Count() > 0
                               select freqNodes).ToList();
                if (newlist.Count == 0)
                {
                    return new[] { this };
                }
                else
                {
                    IEnumerable<TrieNode<T, I>> newlist2 = newlist.Aggregate((a, b) => a.Concat(b));
                    return newlist2;
                }
            }
            return _empty;
        }

        public void Insert(I word, int index)
        {
            _count++;

            if (_converter.GetCount(word) == index)
                return;
            var c = _converter.GetElement(word, index);
            TrieNode<T, I> node;
            if (_children.TryGetValue(c, out node))
            {
            }
            else
            {
                node = new TrieNode<T, I>(this, c, _converter);
                _children.Add(c, node);
            }
            node.Insert(word, index + 1);
        }
        public TrieNode<T, I> Search(I word, int index)
        {
            if (_converter.GetCount(word) == index)
                return this;
            var c = _converter.GetElement(word, index);
            TrieNode<T, I> node;
            if (_children.TryGetValue(c, out node))
            {
                return node.Search(word, index + 1);
            }
            return null;
        }
    }

    public class Trie<T, I> : ITrie<T, I> where T : struct
    {
        private readonly TrieNode<T, I> _rootNode;
        private ITrieConverter<T, I> _converter;

        public Trie(IEnumerable<I> words, ITrieConverter<T, I> Converter)
        {
            _converter = Converter;
            _rootNode = new TrieNode<T, I>(null, default(T), Converter);
            foreach (var word in words)
            {
                this.Insert(word);
            }
        }

        public TrieNode<T, I> Root
        {
            get { return this._rootNode; }
        }

        public void Insert(I word)
        {
            _rootNode.Insert(word, 0);
        }

        public bool Contains(I word)
        {
            var freq = Frequency(word);
            return freq > 0;
        }

        public int Frequency(I word)
        {
            var node = _rootNode.Search(word, 0);
            if (node == null)
                return 0;
            else
                return node.Count;
        }

        public List<KeyValuePair<I, int>> FrequentWords(int percentage)
        {
            var minCount = percentage * _rootNode.Count / 100;
            var list = _rootNode.FrequentWords(minCount).Where(p => p != _rootNode).ToList();
            var result = list.ConvertAll(p => new KeyValuePair<I, int>(_converter.Create(p.GetWord()), p.Count));
            return result;
        }
    }
}