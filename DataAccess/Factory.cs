using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using DBLint.DataAccess.DBObjects;

namespace DBLint.DataAccess
{
    public abstract class Factory
    {
        private String connectionString;
        private DbProviderFactory factory;
        public abstract DBMSs DBMS { get; }
        public DbConnection Connection
        {
            get
            {
                var connection = this.factory.CreateConnection();
                connection.ConnectionString = this.connectionString;
                return connection;
            }
        }

        /// <summary>
        /// Set up the connection to a dababase, based on the ODBC driver.
        /// </summary>
        /// <param name="connectionString">DBMS specific connection string</param>
        /// <param name="providerFactory">ODBC factory instance</param>
        internal void Setup(string connectionString, DbProviderFactory providerFactory)
        {
            this.connectionString = connectionString;
            this.factory = providerFactory;
        }

        public bool TestConnection()
        {
            var conn = this.factory.CreateConnection();
            conn.ConnectionString = this.connectionString;
            try
            {
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;
                throw ex;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        /// <summary>
        /// Executes a single select query. If the query contains parameters these must be added first,
        /// by using the AddParameter function.<see cref="AddParameter"/>
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <returns>DataSet of the select statement</returns>
        internal DataSet ExecuteSelect(string query)
        {
            var result = new DataSet();
            using (var conn = this.factory.CreateConnection())
            {
                conn.ConnectionString = this.connectionString;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandTimeout = 2700; //45 minutes
                    cmd.CommandType = CommandType.Text;
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
                            catch
                            {
                            }
                        }
                        using (var adapter = this.factory.CreateDataAdapter())
                        {
                            adapter.SelectCommand = cmd;
                            unchecked
                            {
                                adapter.Fill(result);
                            }
                        }
                    }
                    finally
                    {
                        if (conn.State == ConnectionState.Open)
                            conn.Close();
                    }
                }
            }
            return result;
        }

        internal object ExecuteSingleValue(string query)
        {
            using (var conn = this.factory.CreateConnection())
            {
                conn.ConnectionString = this.connectionString;
                var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;
                try
                {
                    while (true)
                    {
                        try
                        {
                            conn.Open();
                            break;
                        }
                        catch { }
                    }
                    var value = cmd.ExecuteScalar();
                    if (value == DBNull.Value)
                        return 0;
                    return value;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                        conn.Close();
                }
            }
        }

        /// <summary>
        /// Gets a list of tables from the specified schema names
        /// </summary>
        /// <param name="schemaNames">List of schema names</param>
        /// <returns>All tables from the specified schemas</returns>
        public List<Table> GetTables(IEnumerable<string> schemaNames)
        {
            var tables = new List<Table>();

            foreach (string schemaName in schemaNames)
            {
                tables.AddRange(this.GetTables(schemaName));
            }

            return tables;
        }

        /// <summary>
        /// Gets a list of all tables in all schemas in the database
        /// </summary>
        /// <returns>List of all tables in the database</returns>
        public List<Table> GetTables()
        {
            var tables = new List<Table>();

            foreach (var schema in this.GetSchemas())
            {
                tables.AddRange(this.GetTables(schema.SchemaName));
            }

            return tables;
        }

        /// <summary>
        /// Gets a select * from <paramref name="schemaName"/>.<paramref name="tableName"/> formated with
        /// escape characters for the specific DBMS.
        /// </summary>
        /// <param name="schemaName">The name of the schema.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>Select SQL statement.</returns>
        public abstract string GetFormatedSelectStatement(string schemaName, string tableName, List<String> columnNames);

        /// <summary>
        /// Gets a DBMS specific DataRow object.
        /// </summary>
        /// <param name="reader">A datareader from the ADO.NET library.</param>
        /// <returns>A DataRow</returns>
        public abstract Data.DataRow GetDataRow(DbDataReader reader);

        /// <summary>
        /// Extracts names of schemas available for the user.
        /// </summary>
        /// <returns>List of DBObject.Schema object</returns>
        public abstract List<Schema> GetSchemas();

        /// <summary>
        /// Extracts names of tables in a single schema.
        /// </summary>
        /// <param name="schemaName">A schema name</param>
        /// <returns>List of DBObject.Table object</returns>
        public abstract List<Table> GetTables(string schemaName);

        /// <summary>
        /// Extracts all columns belonging to all tables in a schema
        /// </summary>
        /// <param name="schemaName">A schema name</param>
        /// <returns>List og DBObject.Column object</returns>
        public abstract List<Column> GetColumns(string schemaName);

        /// <summary>
        /// Ectracts unique constraint info for columns.
        /// </summary>
        /// <param name="schemaName">A schema name</param>
        /// <returns>List of DBObject.Unique object</returns>
        public abstract List<Unique> GetUniqueConstraints(string schemaName);

        /// <summary>
        /// Extracts all primary keys for a schema.
        /// </summary>
        /// <param name="schemaName">A schema name</param>
        /// <returns>List of DBObject.PrimaryKey</returns>
        public abstract List<PrimaryKey> GetPrimaryKeys(string schemaName);

        /// <summary>
        /// Extracts all foreign keys for a schema.
        /// </summary>
        /// <param name="schemaName">A schema name</param>
        /// <returns>List of DBObject.ForeignKey</returns>
        public abstract List<ForeignKey> GetForeignKeys(string schemaName);


        // TESTING STORED PROCEDURES, FUNCTIONS AND VIEWS
        public abstract List<StoredProcedure> GetStoredProcedures(string schemaName);
        public abstract List<Function> GetFunctions(string schemaName);
        public abstract List<Parameter> GetStoredProceduresParameters(string schemaName);
        public abstract List<Parameter> GetFunctionsParameters(string schemaName);
        public abstract List<View> GetViews(string schemaName);
        public abstract List<ViewColumn> GetViewColumns(string schemaName);
        // END TESTING


        public abstract List<TableCardinality> GetTableCardinalities(string schemaName);

        /// <summary>
        /// Extracts all indices for a schema.
        /// </summary>
        /// <param name="schemaName">A schema name</param>
        /// <returns>DataSet[schema_name, table_name, index_name, column_name, ordinal_position, is_unique]</returns>
        public abstract List<Index> GetIndices(string schemaName);
        public virtual DataTable Query(string query)
        {
            DataTable dataTable = this.ExecuteSelect(query).Tables[0];
            DataRow t = dataTable.Rows[0];
            return dataTable;
        }
        public abstract object QueryTable(string query);

        public abstract decimal GetDecimal(string query);
    }
}
