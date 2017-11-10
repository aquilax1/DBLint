using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBLint.Rules.DataRules
{
    enum ValueType { String, Bool, Int, Float, Date }

    class Classifier
    {
        private static readonly String[] boolWords = new[] { "true", "false", "yes", "no", "ja", "nej", "sandt", "falsk", "y", "n" };
        private static readonly Regex floatRegex = new Regex(@"^\d{1,}\.\d{1,}$", RegexOptions.Compiled);

        public static ValueType Classify(String str)
        {
            str = str.Trim();

            if (IsInt(str))
                return ValueType.Int;
            else if (IsFloat(str))
                return ValueType.Float;
            else if (IsDate(str))
                return ValueType.Date;
            else
                return ValueType.String;
        }

        public static bool IsInt(String str)
        {
            int res;
            return int.TryParse(str, out res);
        }

        public static bool IsFloat(String str)
        {
            return floatRegex.IsMatch(str);
        }

        public static bool IsBool(String str)
        {
            var word = str.Trim().ToLower();
            foreach (String boolWord in boolWords)
            {
                if (word == boolWord.ToLower())
                    return true;
            }
            return false;
        }

        public static bool IsDate(String str)
        {
            DateTime o;
            return DateTime.TryParse(str, out o);
        }
    }
}
