using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Rules.Utils;

namespace DBLint.Rules.Naming
{
    public class MarkovTokenGraph : MarkovChain<Token>
    {
        private Dictionary<Token, String> prettyTokens = new Dictionary<Token, String>();

        public MarkovTokenGraph()
        {
            prettyTokens.Add(Token.AT, "@");
            prettyTokens.Add(Token.BEGIN, "begin");
            prettyTokens.Add(Token.END, "end");
            prettyTokens.Add(Token.HASH, "#");
            prettyTokens.Add(Token.LOWERWORD, "word");
            prettyTokens.Add(Token.SPACE, "space");
            prettyTokens.Add(Token.SYMBOL, "symbol");
            prettyTokens.Add(Token.UNDERSCORE, "_");
            prettyTokens.Add(Token.UPPERWORD, "WORD");
        }

        public IEnumerable<MarkovEdge> GetGraph()
        {
            List<MarkovEdge> edges = new List<MarkovEdge>();

            foreach (Token from in this.probabilities.Keys)
            {
                foreach (Token to in this.probabilities[from].Keys)
                {
                    var label = (Math.Round(this.probabilities[from][to] * 100, 2));
                    MarkovEdge edge = new MarkovEdge(prettyTokens[from], prettyTokens[to], label);
                    edges.Add(edge);
                }
            }

            return edges;
        }
    }
}
