using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Util
{
    public static class Utils
    {
        public static String ExtractReportSummary(String mainpagePath)
        {
            String mainpage = File.ReadAllText(mainpagePath);
            Regex regex = new Regex("<div id=\"tabs-home\">(.*?)<div id=\"tabs-tables\">", RegexOptions.Singleline);
            Match match = regex.Match(mainpage);
            if (match.Success)
                return match.Groups[1].Value;
            else
                return null;
        }
    }
}
