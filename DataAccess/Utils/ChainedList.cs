using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.DataAccess.Utils
{
    public class ChainedList<T>
    {
        private Dictionary<int, List<T>> list;

        public ChainedList()
        {
            this.list = new Dictionary<int, List<T>>();
        }

        /// <summary>
        /// Adds an object to the list. If the object is added without any collision the return value will
        /// be true, otherwise false. With a false return value the existing object is assigned to the 
        /// out parameter <paramref name="found"/>.
        /// </summary>
        /// <param name="data">The data to be added to the list</param>
        /// <param name="found">If a collision happens the object that collide is set to the out value.</param>
        /// <returns>True if the <paramref name="data"/> is added without collision, otherwise false.</returns>
        public bool Add(T data, out T found)
        {
            var result = true;
            found = data;
            var key = data.GetHashCode();
            // If the key is not already in the list the data is just added, with this key
            if (!this.list.ContainsKey(key))
                this.list.Add(key, new List<T>() { data });
            else
            {
                // Finds an equal object in a list, if it exists
                foreach (var item in this.list[key])
                {
                    // If the object already exists, it out parameter is assigned to it
                    if (item.Equals(data))
                    {
                        found = item;
                        result = false;
                        break;
                    }
                }
                // If the object does not exists it is just added to the list
                if (result)
                    this.list[key].Add(data);
            }
            return result;
        }

        /// <summary>
        /// Converts the chained list of objects into one normal list of objects
        /// </summary>
        /// <returns>List of <paramref name="T"/> objects</returns>
        public List<T> ToList()
        {
            var restult = new List<T>();
            foreach (var pair in this.list)
            {
                foreach (var item in pair.Value)
                    restult.Add(item);
            }
            return restult;
        }
    }
}
