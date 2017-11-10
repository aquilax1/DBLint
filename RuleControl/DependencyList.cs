using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Represents a list of dependencies. If a rule has a dependency, 
    /// then this dependency must be executed before the rule.
    /// </summary>
    public class DependencyList : List<Type>
    {
        public DependencyList(params Type[] dependencies)
        {
            this.AddRange(dependencies);
        }
        public static DependencyList Create<T>()
        {
            return new DependencyList(typeof(T));
        }
        public static DependencyList Create<A, B>()
        {
            return new DependencyList(typeof(A), typeof(B));
        }
        public static DependencyList Create<A, B, C>()
        {
            return new DependencyList(typeof(A), typeof(B), typeof(C));
        }
    }
}
