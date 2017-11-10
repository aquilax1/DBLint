using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Naming
{
    internal class TokenTrieConverter : ITrieConverter<Token, Tokens>
    {
        private static TokenTrieConverter converter = new TokenTrieConverter();
        public static TokenTrieConverter GetInstance()
        {
            return converter;
        }

        public Token GetElement(Tokens word, int index)
        {
            return word[index];
        }

        public int GetCount(Tokens word)
        {
            return word.Count;
        }

        public Tokens Create(IEnumerable<Token> input)
        {
            return new Tokens(input.ToList());
        }
    }

    /// <summary>
    /// Implements a naming convention detector using a Trie to find the must common used tokens.
    /// </summary>
    public class TrieNameDetector : INameConventionDetector
    {
        private ITrie<Token, Tokens> trie;
        private ITokenizer tokenizer = new RegexTokenizer();
        private int tol;
        private Tokens convention = null;

        public TrieNameDetector(int tol)
        {
            this.tol = tol;
        }

        public bool DetectConvention(IEnumerable<string> names)
        {
            var tokenLists = (from name in names
                              let tokens = tokenizer.TokenizeWord(name)
                              let temp = tokens.Remove(Token.END)
                              select tokens).ToList();            
            trie = new Trie<Token, Tokens>(tokenLists, TokenTrieConverter.GetInstance());
            
            this.convention = (from freq_word in trie.FrequentWords(tol)
                            orderby freq_word.Value
                            select freq_word.Key).FirstOrDefault();

            Console.WriteLine(this.convention);

            trav(trie.Root, 0);
            
            return (this.convention != null) ? true : false;
        }

        private void trav(TrieNode<Token, Tokens> node, int level)
        {
            for (int i = 0; i < level; i++)
                Console.Write("\t");
            Console.Write(node._nodeChar + " (" + node.Count + ")\n");
            foreach (var child in node.Children)
                trav(child, level + 1);
        }

        public bool IsValid(String name)
        {
            Tokens tokens = this.tokenizer.TokenizeWord(name);
            for (int i = 0; i < tokens.Count-1 && i < this.convention.Count; i++)
            {
                if (tokens[i] != this.convention[i])
                    return false;
            }
            return true;
        }
    }
}
