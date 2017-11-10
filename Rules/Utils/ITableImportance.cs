using DBLint.Model;

namespace DBLint.Rules.Utils
{
    public interface  ITableImportance
    {
        double this[TableID tableID] { get; }
    }
}