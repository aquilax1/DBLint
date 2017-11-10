using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.DataAccess.DBObjects
{
    public class DatabaseID
    {

    }

    public class SchemaID : DatabaseID
    {
        public string SchemaName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as SchemaID;
            return sid != null && sid.SchemaName == SchemaName;
        }

        public override int GetHashCode()
        {
            return (SchemaName != null ? SchemaName.GetHashCode() : 0);
        }
    }

    public class RoutineID : SchemaID
    {
        public string RoutineName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as RoutineID;
            return sid != null && base.Equals(obj) && sid.RoutineName == this.RoutineName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (this.RoutineName != null ? baseHash + this.RoutineName.GetHashCode() * 19 : 0);
        }
    }

    public class ParameterID : RoutineID
    {
        public string ParameterName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as ParameterID;
            return sid != null && base.Equals(obj) && sid.ParameterName == this.ParameterName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (this.ParameterName != null ? baseHash + this.ParameterName.GetHashCode() * 29 : 0);
        }
    }

    public class TableID : SchemaID
    {
        public string TableName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as TableID;
            return sid != null && base.Equals(obj) && sid.TableName == TableName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (TableName != null ? baseHash + TableName.GetHashCode() * 19 : 0);
        }
    }

    public class ViewID : SchemaID
    {
        public string ViewName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as ViewID;
            return sid != null && base.Equals(obj) && sid.ViewName == ViewName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (ViewName != null ? baseHash + ViewName.GetHashCode() * 19 : 0);
        }
    }

    public class ColumnID : TableID
    {
        public string ColumnName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as ColumnID;
            return sid != null && base.Equals(obj) && sid.ColumnName == ColumnName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (ColumnName != null ? baseHash + ColumnName.GetHashCode() * 29 : 0);
        }
    }

    public class ViewColumnID : ViewID
    {
        public string ColumnName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as ColumnID;
            return sid != null && base.Equals(obj) && sid.ColumnName == ColumnName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (ColumnName != null ? baseHash + ColumnName.GetHashCode() * 29 : 0);
        }
    }

    public class UniqueID : TableID
    {
        public string KeyName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as UniqueID;
            return sid != null && base.Equals(obj) && sid.KeyName == KeyName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (KeyName != null ? baseHash + KeyName.GetHashCode() * 41 : 0);
        }
    }

    public class PrimaryKeyID : TableID
    {
        public string KeyName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as PrimaryKeyID;
            return sid != null && base.Equals(obj) && sid.KeyName == KeyName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (KeyName != null ? baseHash + KeyName.GetHashCode() * 41 : 0);
        }
    }

    public class ForeignKeyID : TableID
    {
        public string ForeignKeyName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as ForeignKeyID;
            return sid != null && base.Equals(obj) && sid.ForeignKeyName == ForeignKeyName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            var keyHash = ForeignKeyName.GetHashCode();
            return (ForeignKeyName != null ? baseHash + keyHash * 41 : 0);
        }
    }

    public class IndexID : TableID
    {
        public string IndexName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as IndexID;
            return sid != null && base.Equals(obj) && sid.IndexName == IndexName;
        }

        public override int GetHashCode()
        {
            var baseHash = base.GetHashCode();
            return (IndexName != null ? baseHash + IndexName.GetHashCode() * 41 : 0);
        }
    }
}
