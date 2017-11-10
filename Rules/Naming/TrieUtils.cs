using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Naming
{
    public interface ITrieConverter<T, I> where T : struct
    {
        T GetElement(I word, int index);
        int GetCount(I word);
        I Create(IEnumerable<T> input);
    }

    public class StringCharConverter : ITrieConverter<char, string>
    {
        public readonly static StringCharConverter Default = new StringCharConverter();
        private StringCharConverter() { }
        public char GetElement(string word, int index)
        {
            return word[index];
        }

        public int GetCount(string word)
        {
            return word.Length;
        }
        public string Create(IEnumerable<char> input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var c in input)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
