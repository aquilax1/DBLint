using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Various methods dealing with dependencies of executables
    /// </summary>
    public static class DependencyGraph
    {
        /// <summary>
        /// Makes a topological sort of the given executables based on their dependencies
        /// </summary>
        /// <param name="executables">The executables to be sorted</param>
        /// <returns>A topological-sorted list of executables</returns>
        public static IEnumerable<IExecutable> TopologicalSort(IEnumerable<IExecutable> executables)
        {
            List<IExecutable> result = new List<IExecutable>();
            var typeMapper = new Dictionary<Type, IExecutable>();
            executables.ToList().Where(e => !e.IsSQLRule()).ToList().ForEach(e => typeMapper.Add(e.GetType(), e));
            //Find executables with no incomming edges
            var noIncommingEdges = executables.Where(e => !hasIncommingEdges(e, executables) ||e.IsProvider() && ((IProvider)e).RunAlways);
            HashSet<IExecutable> visited = new HashSet<IExecutable>();
            foreach (var node in noIncommingEdges)
            {
                visit(node, visited, result, typeMapper);
            }
            return result;
        }

        private static bool hasIncommingEdges(IExecutable executable, IEnumerable<IExecutable> executables)
        {
            return executables.Any(e => e.Dependencies != null && e.Dependencies.Contains(executable.GetType()));
        }

        private static void visit(IExecutable node, HashSet<IExecutable> visited, List<IExecutable> result, Dictionary<Type, IExecutable> typeMapper)
        {
            if (!visited.Contains(node))
            {
                visited.Add(node);
                if (node.Dependencies != null)
                    foreach (var dep in node.Dependencies)
                        if (typeMapper.ContainsKey(dep))
                            visit(typeMapper[dep], visited, result, typeMapper);
                result.Add(node);
            }
        }

        /// <summary>
        /// Constructs a list of passes based on the given list topological-sorted executables. All executables in 
        /// a pass can be executed concurrently, that is, there are no dependencies among executables in a pass
        /// </summary>
        /// <param name="executableEnum">Topological-sorted executables</param>
        /// <returns>A list of passes</returns>
        public static IEnumerable<Pass> CreatePasses(IEnumerable<IExecutable> executableEnum)
        {
            List<Pass> passes = new List<Pass>();
            var executables = executableEnum.ToList();
            executables.ForEach(e => passes.Add(new Pass()));

            for (int i = 0; i < executables.Count(); i++)
            {
                for (int j = i; j >= 0; j--)
                {
                    if (depInList(executables[i], passes[j]))
                    {
                        passes[j+1].Add(executables[i]);
                        break;
                    }
                    if (j == 0)
                        passes[j].Add(executables[i]);
                }
            }
            passes.Where(p => p.Count==0).ToList().ForEach(p => passes.Remove(p));
            return passes;
        }

        private static bool depInList(IExecutable e, IEnumerable<IExecutable> elist)
        {
            if (e.Dependencies == null)
                return false;
            return ((from dep in e.Dependencies
                     from ex in elist
                     where dep == ex.GetType()
                     select dep).Count() > 0);
        }
    }

    /// <summary>
    /// Contains a list of executables that can be executed concurrently without dependencies among them
    /// </summary>
    public class Pass : List<IExecutable>
    {
    }
}
