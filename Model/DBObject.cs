namespace DBLint.Model
{
    public class DatabaseID
    {
        private int? _hashCode = null;
        public string DatabaseName { get; internal set; }
        public DatabaseID(string databaseName)
        {
            DatabaseName = databaseName;
        }

        public bool DatabaseEquals(DatabaseID dbid)
        {
            if (dbid == null)
                return false;
            return dbid.DatabaseName.Equals(this.DatabaseName);
        }

        public override int GetHashCode()
        {
            if (!this._hashCode.HasValue)
                this._hashCode = this.DatabaseName.GetHashCode();
            return this._hashCode.Value;
        }

        public static int GetDatabaseHashCode(DatabaseID dbid)
        {
            return dbid.DatabaseName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            var dbid = obj as DatabaseID;
            return DatabaseEquals(dbid);
        }
    }

    public class SchemaID : DatabaseID
    {
        private int? _hashCode = null;

        private string _schemaName;
        public string SchemaName
        {
            get { return _schemaName; }
            internal set
            {
                _schemaName = value;
                _hashCode = null;
            }
        }

        public SchemaID(string databaseName, string schemaName)
            : base(databaseName)
        {
            this.SchemaName = schemaName;
        }

        public SchemaID(SchemaID schemaID)
            : base(schemaID.DatabaseName)
        {
            this.SchemaName = schemaID.SchemaName;
        }

        public static bool SchemaEquals(SchemaID sid, SchemaID sid2)
        {
            if (sid == null || sid2 == null || SchemaID.GetSchemaHashCode(sid) != SchemaID.GetSchemaHashCode(sid2))
                return false;
            return sid2.SchemaName.Equals(sid.SchemaName) && sid2.DatabaseEquals(sid);
        }

        public static int GetSchemaHashCode(SchemaID sid)
        {
            if (!sid._hashCode.HasValue)
                sid._hashCode = sid._schemaName.GetHashCode();

            return DatabaseID.GetDatabaseHashCode(sid) * 17 + sid._hashCode.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as SchemaID;
            return SchemaEquals(this, sid);
        }

        public override string ToString()
        {
            return SchemaName;
        }

        public override int GetHashCode()
        {
            return GetSchemaHashCode(this);
        }
    }

    public class ParameterID : RoutineID
    {
        private int? _hashCode = null;
        private string _parameterName = null;

        public string ParameterName
        {
            get { return this._parameterName; }
            set
            {
                this._parameterName = value;
                this._hashCode = null;
            }
        }

        public ParameterID(string databaseName, string schemaName, string routineName, string parameterName)
            : base(databaseName, schemaName, routineName)
        {
            this.ParameterName = parameterName;
        }

        public static int GetParameterHashCode(ParameterID pid)
        {
            if (!pid._hashCode.HasValue)
                pid._hashCode = pid._parameterName.GetHashCode();

            return GetRoutineHashCode(pid) * 29 + pid._hashCode.Value;
        }
        public static bool ParameterEquals(ParameterID pid, ParameterID pid2)
        {
            if (pid == null || pid2 == null || GetParameterHashCode(pid) != GetParameterHashCode(pid2))
                return false;
            return pid.ParameterName.Equals(pid2.ParameterName) && RoutineEquals(pid, pid2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as ParameterID;
            return ParameterEquals(this, sid);
        }

        public override string ToString()
        {
            return this.ParameterName;
        }

        public override int GetHashCode()
        {
            return GetParameterHashCode(this);
        }
    }

    public class RoutineID : SchemaID
    {
        private int? _hashCode = null;
        private string _routineName = null;

        internal string RoutineName
        {
            get { return this._routineName; }
            set
            {
                this._routineName = value;
                this._hashCode = null;
            }
        }

        public RoutineID(string databaseName, string schemaName, string routineName)
            : base(databaseName, schemaName)
        {
            RoutineName = routineName;
        }

        public static int GetRoutineHashCode(RoutineID rid)
        {
            if (!rid._hashCode.HasValue)
                rid._hashCode = rid._routineName.GetHashCode();

            return GetSchemaHashCode(rid) * 19 + rid._hashCode.Value;
        }

        public static bool RoutineEquals(RoutineID rid, RoutineID rid2)
        {
            if (rid == null || rid2 == null || GetRoutineHashCode(rid) != GetRoutineHashCode(rid2))
                return false;
            return rid.RoutineName.Equals(rid2.RoutineName) && SchemaEquals(rid, rid2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as RoutineID;
            return RoutineEquals(this, sid);
        }

        public override string ToString()
        {
            return RoutineName;
        }

        public override int GetHashCode()
        {
            return GetRoutineHashCode(this);
        }
    }

    public class FunctionID : RoutineID
    {
        public string FunctionName
        {
            get { return this.RoutineName; }
            set { this.RoutineName = value; }
        }

        public FunctionID(string databaseName, string schemaName, string functionName)
            : base(databaseName, schemaName, functionName)
        {
        }

        public static int GetFunctionHashCode(FunctionID fid)
        {
            return GetRoutineHashCode(fid);
        }

        public static bool FunctionEquals(FunctionID fid, FunctionID fid2)
        {
            return RoutineEquals(fid, fid2);
        }
        
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override int GetHashCode()
        {
            return GetFunctionHashCode(this);
        }
    }

    public class StoredProcedureID : RoutineID
    {
        public string StoredProcedureName
        {
            get { return this.RoutineName; }
            set { this.RoutineName = value; }
        }

        public StoredProcedureID(string databaseName, string schemaName, string storedProcedureName)
            : base(databaseName, schemaName, storedProcedureName)
        {
        }

        public static int GetStoredProcedureHashCode(StoredProcedureID spid)
        {
            return GetRoutineHashCode(spid);
        }

        public static bool StoredProcedureEquals(StoredProcedureID spid, StoredProcedureID spid2)
        {
            return RoutineEquals(spid, spid2);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override int GetHashCode()
        {
            return GetStoredProcedureHashCode(this);
        }
    }

    public class WriteableTableID : TableID
    {
        public void SetDatabaseName(string databaseName)
        {
            this.DatabaseName = databaseName;
        }
        public void SetSchemaName(string schemaName)
        {
            this.SchemaName = schemaName;
        }
        public void SetTableName(string tableName)
        {
            this.TableName = tableName;
        }
        public WriteableTableID(string databaseName, string schemaName, string tableName)
            : base(databaseName, schemaName, tableName)
        {
        }
    }

    public class TableID : SchemaID
    {
        private int? _hashCode = null;
        private string _tableName;
        public string TableName
        {
            get { return _tableName; }
            internal set
            {
                _tableName = value;
                _hashCode = null;
            }
        }

        public TableID(string databaseName, string schemaName, string tableName)
            : base(databaseName, schemaName)
        {
            TableName = tableName;
        }

        public TableID(TableID tableID)
            : base(tableID.DatabaseName, tableID.SchemaName)
        {
            TableName = tableID.TableName;
        }

        public static int GetTableHashCode(TableID tid)
        {
            if (!tid._hashCode.HasValue)
                tid._hashCode = tid._tableName.GetHashCode();

            return GetSchemaHashCode(tid) * 19 + tid._hashCode.Value;
        }

        public static bool TableEquals(TableID tid, TableID tid2)
        {
            if (tid == null || tid2 == null || GetTableHashCode(tid) != GetTableHashCode(tid2))
                return false;
            return tid.TableName.Equals(tid2.TableName) && SchemaEquals(tid, tid2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as TableID;
            return TableEquals(this, sid);
        }
        public override string ToString()
        {
            return TableName;
        }
        public override int GetHashCode()
        {
            return GetTableHashCode(this);
        }
    }

    public class ViewID : SchemaID
    {
        private int? _hashCode = null;
        private string _viewName;
        public string ViewName
        {
            get { return _viewName; }
            internal set
            {
                _viewName = value;
                _hashCode = null;
            }
        }

        public ViewID(string databaseName, string schemaName, string viewName)
            : base(databaseName, schemaName)
        {
            ViewName = viewName;
        }

        public static int GetViewHashCode(ViewID vid)
        {
            if (!vid._hashCode.HasValue)
                vid._hashCode = vid._viewName.GetHashCode();

            return GetSchemaHashCode(vid) * 19 + vid._hashCode.Value;
        }

        public static bool ViewEquals(ViewID vid, ViewID vid2)
        {
            if (vid == null || vid2 == null || GetViewHashCode(vid) != GetViewHashCode(vid2))
                return false;
            return vid.ViewName.Equals(vid2.ViewName) && SchemaEquals(vid, vid2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var sid = obj as ViewID;
            return ViewEquals(this, sid);
        }
        public override string ToString()
        {
            return ViewName;
        }
        public override int GetHashCode()
        {
            return GetViewHashCode(this);
        }
    }

    public class IndexID : TableID
    {
        public string IndexName { get; internal set; }

        public IndexID(string databaseName, string schemaName, string tableName, string indexName)
            : base(databaseName, schemaName, tableName)
        {
            IndexName = indexName;
        }

        public bool IndexEquals(IndexID iid)
        {
            if (iid == null)
                return false;

            return TableEquals(this, iid) && iid.IndexName.Equals(IndexName);
        }

        public static int GetIndexHashCode(IndexID iid)
        {
            return GetTableHashCode(iid) * 29 + iid.IndexName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(obj, this)) return true;

            var iid = obj as IndexID;
            return IndexEquals(iid);
        }

        public override string ToString()
        {
            return IndexName;
        }

        public override int GetHashCode()
        {
            return GetIndexHashCode(this);
        }
    }

    public class ColumnID : TableID
    {
        private string _columnName;
        public string ColumnName
        {
            get { return _columnName; }
            internal set
            {
                if (_columnName != value)
                    _hashCode = null;
                _columnName = value;
            }
        }

        private int? _hashCode;

        public ColumnID(string databaseName, string schemaName, string tableName, string columnName)
            : base(databaseName, schemaName, tableName)
        {
            ColumnName = columnName;
        }

        public ColumnID(ColumnID colID)
            : base(colID.DatabaseName, colID.SchemaName, colID.TableName)
        {
            ColumnName = colID.ColumnName;
        }

        public static bool ColumnEquals(ColumnID cid, ColumnID cid2)
        {
            if (cid == null)
                return false;
            if (GetColumnHashCode(cid2) != GetColumnHashCode(cid))
                return false;
            return TableEquals(cid, cid2) && cid.ColumnName.Equals(cid2.ColumnName);
        }

        public static int GetColumnHashCode(ColumnID cid)
        {
            if (!cid._hashCode.HasValue)
                cid._hashCode = cid.ColumnName.GetHashCode();
            return GetTableHashCode(cid) * 23 + cid._hashCode.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var cid = obj as ColumnID;
            return ColumnEquals(this, cid);
        }

        public override string ToString()
        {
            return ColumnName;
        }

        public override int GetHashCode()
        {
            return GetColumnHashCode(this);
        }
    }

    public class ViewColumnID : ViewID
    {
        private string _columnName;
        public string ColumnName
        {
            get { return _columnName; }
            internal set
            {
                if (_columnName != value)
                    _hashCode = null;
                _columnName = value;
            }
        }

        private int? _hashCode;

        public ViewColumnID(string databaseName, string schemaName, string viewName, string columnName)
            : base(databaseName, schemaName, viewName)
        {
            ColumnName = columnName;
        }

        public static bool ViewColumnEquals(ViewColumnID vcid, ViewColumnID vcid2)
        {
            if (vcid == null)
                return false;
            if (GetViewColumnHashCode(vcid2) != GetViewColumnHashCode(vcid))
                return false;
            return ViewEquals(vcid, vcid2) && vcid.ColumnName.Equals(vcid2.ColumnName);
        }

        public static int GetViewColumnHashCode(ViewColumnID vcid)
        {
            if (!vcid._hashCode.HasValue)
                vcid._hashCode = vcid.ColumnName.GetHashCode();
            return GetViewHashCode(vcid) * 23 + vcid._hashCode.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var vcid = obj as ViewColumnID;
            return ViewColumnEquals(this, vcid);
        }

        public override string ToString()
        {
            return ColumnName;
        }

        public override int GetHashCode()
        {
            return GetViewColumnHashCode(this);
        }
    }
}
