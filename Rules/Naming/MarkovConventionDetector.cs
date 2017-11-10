using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Rules.Utils;

namespace DBLint.Rules.Naming
{
    /// <summary>
    /// A naming convention detector based on a markov model
    /// </summary>
    public class MarkovConventionDetector : INameConventionDetector
    {
        private MarkovChain<Token> markov = new MarkovChain<Token>();
        private ITokenizer tokenizer = new RegexTokenizer();
        private float tolerance;

        public MarkovConventionDetector(float tolerance)
        {
            this.tolerance = tolerance;
        }

        public bool DetectConvention(IEnumerable<String> names)
        {
            if (names.Count() == 0)
                return false;

            //Tokenize list of names
            var tokenLists = (from name in names select tokenizer.TokenizeWord(name));
            //Add each tokenized word to the markov chain
            foreach (Tokens tokens in tokenLists)
            {
                for (int i = 0; i < tokens.Count - 2; i++)
                {
                    this.markov.AddConnection(tokens[i], tokens[i + 1]);
                }
            }
            this.markov.calculateProbabilities();

            List<Token> conv = this.markov.GetHighestProbabilityChain(Token.BEGIN, 10);
            for (int i = 0; i < conv.Count - 1; i++)
            {
                if (this.markov.GetProbability(conv[i], conv[i + 1]) < this.tolerance)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsValid(String name)
        {
            Tokens tokens = this.tokenizer.TokenizeWord(name);
            for (int i = 0; i < tokens.Count - 2; i++)
            {
                if (this.markov.GetProbability(tokens[i], tokens[i + 1]) < this.tolerance)
                {
                    //var prob = this.markov.GetProbability(tokens[i], tokens[i + 1]);
                    //Console.WriteLine(String.Format("Fail on '{0}' on transition: {1}->{2}, prob.: {3}", name, tokens[i], tokens[i+1], prob));

                    return false;
                }
            }
            return true;
        }
    }
}
