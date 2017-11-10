using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBLint.Rules.Naming
{
    public enum Token { BEGIN, UPPERWORD, LOWERWORD, UNDERSCORE, HASH, AT, SYMBOL, END, SPACE }


    /// <summary>
    /// Specifices a tokenizer which splits words into tokens
    /// </summary>
    public interface ITokenizer
    {
        /// <summary>
        /// Split a given word into a list of tokens
        /// </summary>
        /// <param name="word">Input word</param>
        /// <returns>A list of tokens</returns>
        Tokens TokenizeWord(String word);
    }

    /// <summary>
    /// Implementation of a tokenizer using regular expressions
    /// </summary>
    public class RegexTokenizer : ITokenizer
    {
        //Specificy a regex for each Token (must be mutually exclusive)
        private static List<KeyValuePair<Token, Regex>> tokenDefs = new List<KeyValuePair<Token, Regex>>()
        {
            new KeyValuePair<Token,Regex>(Token.UPPERWORD, new Regex("([A-Z][A-Z0-9]*)")),
            new KeyValuePair<Token,Regex>(Token.LOWERWORD, new Regex("([a-z][a-z0-9]*)")),
            new KeyValuePair<Token,Regex>(Token.UNDERSCORE, new Regex("(_)")),
            new KeyValuePair<Token,Regex>(Token.HASH, new Regex("(#)")),
            new KeyValuePair<Token,Regex>(Token.AT, new Regex("(@)")),
            new KeyValuePair<Token,Regex>(Token.SPACE, new Regex("( +)")),
            new KeyValuePair<Token,Regex>(Token.SYMBOL, new Regex("([^a-zA-Z0-9_#@ ])"))
        };

        public Tokens TokenizeWord(String name)
        {
            SortedDictionary<int, Token> tokenMatches = new SortedDictionary<int, Token>(); //index->token
            tokenMatches.Add(-1, Token.BEGIN); //begin/end are bit different because they dont consume any space in the string
            tokenMatches.Add(name.Length, Token.END);
            foreach (KeyValuePair<Token, Regex> tokenDef in tokenDefs)
            {
                Token token = tokenDef.Key;
                Regex regex = tokenDef.Value;
                MatchCollection regexMatches = regex.Matches(name);
                foreach (Match match in regexMatches)
                {
                    if (!tokenMatches.ContainsKey(match.Index))
                        tokenMatches.Add(match.Index, token);
                }
            }
            return new Tokens(tokenMatches.Values.ToList());
        }
    }
}
