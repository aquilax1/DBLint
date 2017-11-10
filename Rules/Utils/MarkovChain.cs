using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Utils
{
    /// <summary>
    /// A generic Markov Chain model
    /// </summary>
    /// <typeparam word="T">Type stored in the nodes/states</typeparam>
    public class MarkovChain<T>
    {
        protected Dictionary<T, Dictionary<T, int>> matrix = new Dictionary<T, Dictionary<T, int>>();
        protected Dictionary<T, Dictionary<T, float>> probabilities = new Dictionary<T, Dictionary<T, float>>();

        /// <summary>
        /// Adds a connection between two states
        /// </summary>
        /// <param word="from">From state</param>
        /// <param word="to">Target state</param>
        public void AddConnection(T from, T to)
        {
            if (!matrix.ContainsKey(from))
                matrix.Add(from, new Dictionary<T, int>());
            if (!matrix[from].ContainsKey(to))
                matrix[from].Add(to, 0);

            matrix[from][to] += 1;
        }

        /// <summary>
        /// Gets the probability of going from one state to another state
        /// </summary>
        /// <param word="from"></param>
        /// <param word="to"></param>
        /// <returns></returns>
        public float GetProbability(T from, T to)
        {
            if (!probabilities.ContainsKey(from) || !probabilities[from].ContainsKey(to))
                return 0f;

            return this.probabilities[from][to];
        }

        public float GetTransitionCount(T from, T to)
        {
            if (!matrix.ContainsKey(from) || !matrix[from].ContainsKey(to))
                return 0f;

            return this.matrix[from][to];
        }

        public List<T> GetHighestProbabilityChain(T startState, int maxLength)
        {
            List<T> chain = new List<T>();
            chain.Add(startState);
            T state = startState;
            while (chain.Count < maxLength)
            {
                if (!probabilities.ContainsKey(state))
                    break;
                var maxProbState = probabilities[state].OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
                chain.Add(maxProbState);
                state = maxProbState;
            }
            return chain;
        }

        public void calculateProbabilities()
        {
            this.probabilities = new Dictionary<T, Dictionary<T, float>>();
            foreach (T from in matrix.Keys)
            {
                probabilities.Add(from, new Dictionary<T, float>());
                int total = matrix[from].Values.Sum();
                foreach (T to in matrix[from].Keys)
                {
                    probabilities[from].Add(to, ((float)matrix[from][to] / total));
                }
            }
        }
    }
}
