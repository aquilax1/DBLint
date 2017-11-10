using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Util
{
    public static class FileUtils
    {
        public static string EscapeDirectoryName(string reportName)
        {
            foreach (var invalidChar in Path.GetInvalidPathChars())
                reportName.Replace(invalidChar, '_');
            return reportName;
        }
    }
}
