using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Represents an executable item such as rules and data data providers.
    /// </summary>
    public interface IExecutable : IConfigurable
    {
        String Name { get; }
        /// <summary>
        /// A list of dataProviders that it depends on.
        /// </summary>
        DependencyList Dependencies { get; }
    }


    public static class ExecutableExtensions
    {
        public static bool IsData(this IExecutable e)
        {
            return (e is IDataProvider || e is IDataRule);
        }

        public static bool IsSchema(this IExecutable e)
        {
            return (e is ISchemaRule || e is ISchemaProvider);
        }

        public static bool IsRule(this IExecutable e)
        {
            return (e is IRule);
        }

        public static bool IsProvider(this IExecutable e)
        {
            return (e is IProvider);
        }

        public static bool IsDataRule(this IExecutable e)
        {
            return (e.IsRule() && e.IsData());
        }

        public static bool IsSchemaRule(this IExecutable e)
        {
            return (e.IsRule() && e.IsSchema());
        }

        public static bool IsSQLRule(this IExecutable e)
        {
            return (e is SQLRule);
        }

        public static bool IsDataProvider(this IExecutable e)
        {
            return (e.IsProvider() && e.IsData());
        }

        public static bool IsSchemaProvider(this IExecutable e)
        {
            return (e.IsProvider() && e.IsSchema());
        }
    }
}
