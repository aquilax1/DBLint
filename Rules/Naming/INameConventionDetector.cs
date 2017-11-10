using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Naming
{
    /// <summary>
    /// Specifices a naming convention detector.
    /// </summary>
    public interface INameConventionDetector
    {
        /// <summary>
        /// Add a training set to the detector
        /// </summary>
        /// <param name="names"></param>
        /// <returns>True if a convention was detected, false it unable to do so</returns>
        bool DetectConvention(IEnumerable<String> names);
        /// <summary>
        /// Is the given name valid according to the found name convention?
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>True if the name is valid according to the name convention</returns>
        bool IsValid(String name);
    }
}
