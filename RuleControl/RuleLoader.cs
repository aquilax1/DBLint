using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace DBLint.RuleControl
{
    public class RuleLoader
    {
        public static ExecutableCollection LoadRules()
        {
            if (DBLint.Settings.IsNormalContext)
            {
                String dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return RuleLoader.LoadRules(dir);
            }
            else
            {
                var type = typeof(IExecutable);
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(s => s.FullName.Contains("Rules")).ToList();
                var alltypes = assemblies.SelectMany(s => s.GetTypes()).ToList();
                var types = alltypes
                    .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                    .Select(p => (IExecutable)Activator.CreateInstance(p))
                    .ToList();
                return new ExecutableCollection(types);
            }
        }

        public static ExecutableCollection LoadRules(String dir)
        {
            //Get files from specified directory
            DirectoryInfo di = new DirectoryInfo(dir);
            FileInfo[] ruleFiles = di.GetFiles("*.dll");

            //List of Types implementing IExecutable
            List<Type> executables = new List<Type>();
            //Loop over assemblies and find Types which implement IExecutable
            foreach (FileInfo file in ruleFiles)
            {
                try
                {
                    Type[] types = Assembly.LoadFrom(file.FullName).GetTypes();
                    var exec = types.Where(type => type.GetInterface("IExecutable") != null &&
                                                  type.IsClass == true &&
                                                  type.IsAbstract == false &&
                                                  type.Name != "SQLRule").ToList();
                    executables.AddRange(exec);
                }
                catch (Exception e)
                {
                    //Do nothing. Some dll's are not the right format, then skip
                }
            }

            //Instantiate and return all executable Types
            ExecutableCollection c = new ExecutableCollection();
            executables.ForEach(e => c.AddExecutable((IExecutable)Activator.CreateInstance(e)));

            //c.AddExecutables(SQLRuleCollection.GetRules()); //SQL rules are now added when reading the config file

            return c;
        }
    }
}
