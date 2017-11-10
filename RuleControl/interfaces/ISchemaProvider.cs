using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    public interface ISchemaProvider : IProvider
    {
        void Execute(Database database, IProviderCollection providers);
    }
}
