using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Naming
{
    public class Tokens : Collection<Token>
    {
        public Tokens() { }

        public Tokens(params Token[] tokens)
        {
            foreach (Token token in tokens)
                this.Add(token);
        }

        public Tokens(IEnumerable tokens)
        {
            foreach (Token t in tokens)
                this.Add(t);
        }

        public override bool Equals(object listObj)
        {
            Tokens tokList = (Tokens)listObj;
            if (tokList.Count != this.Count)
                return false;

            for (int i = 0; i < tokList.Count; i++)
            {
                if (tokList[i] != this[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            String tokens = "";
            for (int i = 0; i < this.Count; i++)
            {
                tokens += this[i];
                if (i != this.Count - 1)
                    tokens += "->";
            }
            return tokens;
        }
    }
}
