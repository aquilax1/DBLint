using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace DBLint.Data
{
    public abstract class DataRow
    {
        internal Dictionary<string, object> values = new Dictionary<string, object>();

        /// <summary>
        /// Gets the data stored in the column specified by name.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns></returns>
        public abstract object this[string columnName] { get; }

        /*
        /// <summary>
        /// Gets the data stored in the column specified by index.
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column.</param>
        /// <returns></returns>
        public abstract object this[int columnIndex] { get; }
         */
    }
}
