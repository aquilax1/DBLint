using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Threading;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.DataAccess;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Diagnostics;

namespace DBLint.Data
{
    public class DataTable : Table, IEnumerable<DataRow>
    {
        //private Stopwatch debugClock = new Stopwatch();

        private List<Column> _allowedQueryColumns = new List<Column>();
        private Factory factory;
        private List<DataType> allowedDataTypesForQuery;
        private bool? _CardinalityVerified;
        public DBMSs DBMS { get { return factory.DBMS; } }

        public List<Column> QueryableColumns
        {
            get { return this._allowedQueryColumns; }
        }

        public object QueryTable(string query)
        {
            return factory.QueryTable(query);
        }

        public decimal GetDecimal(string query)
        {
            return factory.GetDecimal(query);
        }

        public DataTable(string databaseName, string schemaName, string tableName, Factory factory)
            : base(databaseName, schemaName, tableName)
        {
            this.factory = factory;
            #region allowed data types for query
            this.allowedDataTypesForQuery = new List<DataType>() {
                DataType.BIGINT,
                DataType.BIT,
                DataType.BOOLEAN,
                DataType.CHAR,
                DataType.DATE,
                DataType.DATETIME,
                DataType.DECIMAL,
                DataType.DOUBLE,
                DataType.FLOAT,
                DataType.NUMBER,
                DataType.INTEGER,
                DataType.LONGNVARCHAR,
                DataType.LONGVARBINARY,
                DataType.LONGVARCHAR,
                DataType.NCHAR,
                DataType.NULL,
                DataType.NUMERIC,
                DataType.NVARCHAR,
                DataType.REAL,
                DataType.SMALLINT,
                DataType.TEXT,
                DataType.TIME,
                DataType.TIMESTAMP,
                DataType.TINYINT,
                DataType.VARBINARY,
                DataType.VARCHAR,
                DataType.XML,
                DataType.ENUM
            };
            #endregion
        }


        public DataEnumeratorBase GetTableRowEnumerable()
        {
            /*
            if (this.debugClock.IsRunning == false)
                this.debugClock.Start();
            Console.WriteLine(debugClock.ElapsedMilliseconds + " ; " + this.TableName);*/
            //return new DataEnumerator2(this);
            if (this.Cardinality == 0)
            {
                if (this._CardinalityVerified.HasValue)
                {
                    if (this._CardinalityVerified.Value)
                    {
                        return new EmptyDataEnumerator();
                    }
                    else
                    {
                        return new DataEnumerator(this);
                    }
                }

                lock (this)
                {
                    if (!this._CardinalityVerified.HasValue)
                    {
                        var tableString = this.Database.Escaper.Escape(this);
                        var query = string.Format("SELECT COUNT(*) FROM {0}", tableString);
                        this._CardinalityVerified = Convert.ToInt64(this.QueryTable(query)) == 0;
                    }
                }
                return this.GetTableRowEnumerable();
            }
            else
            {
                return new DataEnumerator(this);
            }
        }

        public IEnumerator<DataRow> GetEnumerator()
        {
            if (this.QueryableColumns.Count == 0)
                yield break;

            DbDataReader reader = null;
            DbConnection conn = this.factory.Connection;
            try
            {
                // Continue until it gets a connection
                while (true)
                {
                    try
                    {
                        conn.Open();
                        break;
                    }
                    catch { }
                }
                var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 3600;
                cmd.CommandText = this.factory.GetFormatedSelectStatement(this.SchemaName, this.TableName, this._allowedQueryColumns.Select(c => c.ColumnName).ToList());
                reader = cmd.ExecuteReader();
                while (reader.Read())
                    yield return this.factory.GetDataRow(reader);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<Column> GetColumnForSelect()
        {
            var result = new List<Column>();
            foreach (var column in this.Columns)
            {
                if (this.allowedDataTypesForQuery.Contains(column.DataType))
                    result.Add(column);
            }
            return result;
        }

        public override void Dispose()
        {
            base.Dispose();
            this._allowedQueryColumns = this.GetColumnForSelect();
        }
    }


    public class DataEnumerator2 : DataEnumeratorBase
    {
        private DataTable _table;

        public DataEnumerator2(DataTable table)
        {
            this._table = table;
        }
        public override IEnumerator<DataRow> GetEnumerator()
        {
            return _table.GetEnumerator();
        }



        public override void Dispose()
        {
        }
    }


    public abstract class DataEnumeratorBase : IEnumerable<DataRow>, IDisposable
    {
        public abstract IEnumerator<DataRow> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public abstract void Dispose();
    }

    public class EmptyDataEnumerator : DataEnumeratorBase
    {
        public override IEnumerator<DataRow> GetEnumerator()
        {
            yield break;
        }

        public override void Dispose()
        { }
    }

    public class DataEnumerator : DataEnumeratorBase
    {

        public bool Disposed { get; private set; }

        private static object[] _masterLock = new object[0];
        private static Thread masterThread;

        private static Queue<KeyValuePair<DataTable, List<DataEnumerator>>> _tableQueuedEnumerators = new Queue<KeyValuePair<DataTable, List<DataEnumerator>>>();
        private static Semaphore NextTable = new Semaphore(0, int.MaxValue);
        private Semaphore ConsumeNext = new Semaphore(0, 1);
        private Semaphore HasConsumedCurrent = new Semaphore(1, 1);
        private volatile DataRow _currentRow;
        private volatile DataRow[] _currentRows;

        public DataEnumerator(DataTable table)
        {
            this.Disposed = false;
            lock (_masterLock)
            {
                if (!_tableQueuedEnumerators.Any(kv => TableID.TableEquals(kv.Key, table)))
                {
                    _tableQueuedEnumerators.Enqueue(new KeyValuePair<DataTable, List<DataEnumerator>>(table, new List<DataEnumerator>() { this }));

                    if (masterThread == null)
                    {
                        masterThread = new Thread(DoWork);
                        masterThread.Start();
                    }
                    NextTable.Release();
                }
                else
                {
                    _tableQueuedEnumerators.First(kv => TableID.TableEquals(table, kv.Key)).Value.Add(this);
                }
            }
        }

        private static void DoWork()
        {
            while (true)
            {
                if (!NextTable.WaitOne(5000))
                {
                    lock (_masterLock)
                    {
                        if (!NextTable.WaitOne(0))
                        {
                            masterThread = null;
                            return;
                        }
                    }
                }

                Thread.Sleep(200);

                List<DataEnumerator> enumerators;
                DataTable currentTable;
                lock (_masterLock)
                {
                    var kv = _tableQueuedEnumerators.Dequeue();
                    enumerators = kv.Value;
                    currentTable = kv.Key;
                }


                List<DataRow> list = new List<DataRow>(150);
                foreach (var row in currentTable)
                {
                    list.Add(row);

                    if (list.Count == 100)
                    {
                        var rows = list.ToArray();
                        list.Clear();
                        for (int index = 0; index < enumerators.Count; index++)
                        {
                            var enumerator = enumerators[index];
                            if (enumerator.Disposed)
                            {
                                enumerators.RemoveAt(index);
                                index--;
                                continue;
                            }
                            enumerator.SetNextElement(rows);
                        }
                        if (enumerators.Count == 0)
                            break;
                    }
                }
                if (list.Count > 0)
                {
                    var rows = list.ToArray();
                    for (int index = 0; index < enumerators.Count; index++)
                    {
                        var enumerator = enumerators[index];
                        if (enumerator.Disposed)
                        {
                            enumerators.RemoveAt(index);
                            index--;
                            continue;
                        }
                        enumerator.SetNextElement(rows);
                    }
                }
                DataRow[] empty = null;
                enumerators.ForEach(de => de.SetNextElement(empty));
            }
        }


        private void SetNextElement(DataRow[] rows)
        {
            while (!HasConsumedCurrent.WaitOne(50) && !Disposed)
            { }
            if (this.Disposed)
                return;
            this._currentRows = rows;
            ConsumeNext.Release();
        }

        private void SetNextElement(DataRow row)
        {
            while (!HasConsumedCurrent.WaitOne(50) && !Disposed)
            { }
            if (this.Disposed)
                return;
            _currentRow = row;
            ConsumeNext.Release();
        }

        public override IEnumerator<DataRow> GetEnumerator()
        {
            while (true)
            {
                while (!this.ConsumeNext.WaitOne(100))
                { }
                var rows = _currentRows;
                HasConsumedCurrent.Release();
                if (rows == null || rows.Length == 0)
                {
                    this.Disposed = true;
                    yield break;
                }
                else
                {
                    foreach (var row in rows)
                    {
                        yield return row;
                    }
                }
                /*
                var row = this._currentRow;
                HasConsumedCurrent.Release();
                if (row == null)
                {
                    this.Disposed = true;
                    yield break;
                }
                yield return row;*/
            }
        }


        public override void Dispose()
        {
            this.Disposed = true;
        }
    }
}
