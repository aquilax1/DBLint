using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint
{
    public static class Settings
    {
        public const String REPORT_FOLDER = "report/";
        public const String INCREMENTAL_FOLDER = "runs/";
        public const String CONFIG_FILE = "config.xml"; //Legacy. Previously used for rule configurations
        public const String XMLCONFIG = "configurations.xml";
        /// <summary>
        /// True = normal dblint mode, false = webserver mode
        /// </summary>
        public static bool IsNormalContext = true;
    }
}
