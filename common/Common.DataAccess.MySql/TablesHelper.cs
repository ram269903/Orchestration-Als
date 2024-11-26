using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DataAccess.MySql
{
    public class TablesHelper : ITablesHelper
    {
        private readonly string _connectionString;

        public TablesHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<string> GetTableNames()
        {
            var tablesDetailsList = GetTables();

            return tablesDetailsList.Select(table => table.DatabaseName + "." + table.TableName).ToList();
        }

        public async Task<IEnumerable<DbTable>> GetTablesDetails(List<string> tablesList = null)
        {
            var tables = GetTables(tablesList);

            foreach (var table in tables)
            {
                //Get Primary Keys
                var primaryKeys = await GetPrimaryKeys(table.DatabaseName, table.TableName);

                //Get Columns
                table.Columns = GetColumns(table.DatabaseName, table.TableName, primaryKeys);

                //Get Relations
                table.Relations = await GetRelations(table.DatabaseName, table.TableName);

            }

            return tables;
        }

        public async Task<bool> CheckTableExists(string tableName, string schema = null)
        {
            var query = $"SELECT COUNT(*) FROM information_schema.tables WHERE TABLE_SCHEMA = '{schema}' AND table_name = '{tableName}'";

            var count = await new QueryHelper(_connectionString).ExecuteNonQuery(query, null);

            return count >= 1;
        }

        public async Task CreateTable(string tableName, List<DbColumn> columnDetails, string schema = null, bool dropCurrentTable = true)
        {
            var columns = GenerateColumns(columnDetails);
            var query = $"CREATE TABLE [{schema}].[{tableName}] ( {columns})";

            //var query = "CREATE TABLE " + tableName + "(" + columns + ")";

            var tableExists = await CheckTableExists(schema, tableName);

            if (tableExists && !dropCurrentTable)
                return;

            if (tableExists)
                await DropTable(schema, tableName);

            await new QueryHelper(_connectionString).ExecuteNonQuery(query, null);

        }

        public async Task DropTable(string tableName, string schema = null)
        {
            var query = $"DROP TABLE [{schema}].[{tableName}];";

            _= await new QueryHelper(_connectionString).ExecuteNonQuery(query, null);
        }

        public async Task TruncateTable(string tableName, string schema = null)
        {
            var query = $"TRUNCATE TABLE [{schema}].[{tableName}];";

            _ = await new QueryHelper(_connectionString).ExecuteNonQuery(query, null);
        }

        //public void InsertTableData(string tableName, DataTable data, bool isAppend)
        //{
        //    if (!isAppend)
        //        TruncateTable(tableName);

        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        connection.Open();

        //        using (var transaction = connection.BeginTransaction())
        //        {
        //            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
        //            {
        //                // bulkCopy.BatchSize = 100;
        //                bulkCopy.DestinationTableName = "dbo." + tableName;
        //                bulkCopy.WriteToServer(data);
        //            }

        //            transaction.Commit();
        //        }
        //    }
        //}

        //public async void InsertCsvData(string schema, string tableName, DataTable data, bool isAppend, int? batchSize)
        //{
        //    if (!isAppend)
        //        await TruncateTable(schema, tableName);

        //    using (var connection = new MySqlConnection(_connectionString))
        //    {
        //        connection.Open();

        //        using (var transaction = connection.BeginTransaction())
        //        {
        //            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
        //            {
        //                if (batchSize != null)
        //                    bulkCopy.BatchSize = (int)batchSize;

        //                bulkCopy.DestinationTableName = tableName;
        //                bulkCopy.WriteToServer(data);
        //            }

        //            transaction.Commit();
        //        }
        //    }
        //}

        public async Task<DataTable> GetTableData(string schema, string tableName)
        {
            var query = $"select * from [{schema}].[{tableName}];";

            return await new QueryHelper(_connectionString).ExecuteQuery(query);
        }

        private List<DbTable> GetTables(ICollection<string> tablesList = null)
        {
            DataTable tblTables;
            var tables = new List<DbTable>();
            //var database = new DbHelper().GetDatabase(_connectionString);
            var serverName = new DbHelper().GetServerName(_connectionString);

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                tblTables = connection.GetSchema("Tables", new[] { null, null, null, "BASE TABLE" });
                connection.Close();
            }

            foreach (DataRow table in tblTables.Rows)
            {
                var fullyQualifiedName = table["TABLE_SCHEMA"] + "." + table["TABLE_NAME"];

                if (tablesList == null || tablesList.Contains(fullyQualifiedName))
                {
                    tables.Add(new DbTable()
                    {
                        Server = serverName,
                        DatabaseName = table["TABLE_SCHEMA"].ToString(),
                        TableName = table["TABLE_NAME"].ToString()
                    });
                }
            }

            return tables;
        }

        private async Task<IEnumerable<string>> GetPrimaryKeys(string schema, string tableName)
        {
            var sql = $@"SHOW KEYS FROM {schema}.{tableName} WHERE Key_name = 'PRIMARY'";

            return await new QueryHelper(_connectionString).Read(sql, null, MakePrimaryKeys);
        }

        private List<DbColumn> GetColumns(string schema, string tableName, IEnumerable<string> primaryKeys)
        {
            DataTable columns;
            var lstColumns = new List<DbColumn>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                columns = connection.GetSchema("Columns", new[] { null, schema, tableName, null });
                connection.Close();
            }

            foreach (DataRow item in columns.Rows)
            {
                var fullyQualifiedName = $"{ item["TABLE_SCHEMA"]}.{ item["TABLE_NAME"]}.{ item["COLUMN_NAME"]}";

                var column = new DbColumn
                {
                    ColumnName = item["COLUMN_NAME"].ToString(),
                    DataType = item["DATA_TYPE"].ToString(),
                    FullyQualifiedName = fullyQualifiedName,
                };

                if (primaryKeys != null && primaryKeys.Contains(column.ColumnName))
                    column.IsPrimary = true;

                lstColumns.Add(column);
            }

            return lstColumns;
        }

        private async Task<IEnumerable<DbRelation>> GetRelations(string schema, string tableName)
        {
            var sqlQuery = $@"SELECT *
                            FROM 
                                INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                            WHERE
                                CONSTRAINT_NAME != 'PRIMARY'                                       
                                AND CONSTRAINT_SCHEMA = '{schema}'
                                AND table_name = '{tableName}'";

            return await new QueryHelper(_connectionString).Read(sqlQuery, null, MakeRelations);

        }

        private static string GenerateColumns(IEnumerable<DbColumn> columnDetails)
        {
            var columns = string.Empty;

            foreach (var column in columnDetails)
            {
                columns += "[" + column.ColumnName + "] " + GetNativeDataType(column.DataType) + ", ";

                //if (column.DataType.ToLower() == "nvarchar")
                //    columns += " (max) ,";
                //else
                //    columns += ", ";
            }

            return columns;
        }

        public static string GetNativeDataType(string genericDataType)
        {
            string dataType;

            switch (genericDataType.ToLower().Trim())
            {
                case "string":
                    dataType = "LONGTEXT";
                    break;
                case "int":
                    dataType = "INT";
                    break;
                case "decimal":
                    dataType = "FLOAT";
                    break;
                case "boolean":
                    dataType = "BIT(1)";
                    break;
                case "datetime":
                    dataType = "DATETIME";
                    break;
                default:
                    dataType = "LONGTEXT";
                    break;
            }

            return dataType;
        }

        private static readonly Func<IDataReader, string> MakePrimaryKeys = reader => reader["Column_name"].AsString();

        private static readonly Func<IDataReader, DbRelation> MakeRelations = reader =>
            new DbRelation
            {
                ForeignTable = reader["TABLE_NAME"].AsString(),
                ForeignKey = reader["COLUMN_NAME"].AsString(),
                ReferencedTable = reader["REFERENCED_TABLE_NAME"].AsString(),
                ReferencedKey = reader["REFERENCED_COLUMN_NAME"].AsString(),
            };

    }
}
//using Common.DataAccess.RDBMS;
//using Common.DataAccess.RDBMS.Model;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Common.DataAccess.MsSql
//{
//    public class TablesHelper : ITablesHelper
//    {
//        private readonly string _connectionString;

//        public TablesHelper(string connectionString)
//        {
//            _connectionString = connectionString;
//        }

//        public async Task<IEnumerable<string>> GetTableNames()
//        {
//            var tablesDetailsList = await GetTables();

//            return tablesDetailsList.Select(table => table.SchemaName + "." + table.TableName).ToList();
//        }

//        public string[,] GetSchemaTableNames()
//        {
//            var tablesDetailsList = GetTables();

//            var tableNames = new string[tablesDetailsList.Count, 2];

//            for (var i = 0; i < tablesDetailsList.Count; i++)
//            {
//                tableNames[i, 0] = tablesDetailsList[i].SchemaName;
//                tableNames[i, 1] = tablesDetailsList[i].TableName;
//            }

//            return tableNames;
//        }

//        public List<DbTable> GetTablesDetails(List<string> tablesList = null)
//        {
//            var tables = GetTables(tablesList);

//            foreach (var table in tables)
//            {
//                //Get Primary Keys
//                var primaryKeys = GetPrimaryKeys(table.SchemaName, table.TableName);

//                //Get Columns
//                table.Columns = GetColumns(table.SchemaName, table.TableName, primaryKeys);

//                //Get Relations
//                table.Relations = GetRelations(table.SchemaName, table.TableName);

//            }

//            return tables;
//        }

//        private async Task<IEnumerable<DbTable>> GetTables(ICollection<string> tablesList = null)
//        {
//            DataTable tblTables;
//            var tables = new List<DbTable>();
//            var database = DbHelper.GetDatabase(_connectionString);

//            using (var connection = new SqlConnection(_connectionString))
//            {
//                connection.Open();
//                tblTables = connection.GetSchema(SqlClientMetaDataCollectionNames.Tables, new[] { null, null, null, "BASE TABLE" });
//                connection.Close();
//            }

//            foreach (DataRow table in tblTables.Rows)
//            {
//                var fullyQualifiedName = table["TABLE_CATALOG"] + "." + table["TABLE_SCHEMA"] + "." + table["TABLE_NAME"];

//                if (tablesList == null || tablesList.Contains(fullyQualifiedName))
//                {
//                    tables.Add(new DbTable()
//                    {
//                        ServerName = database.ServerName,
//                        DatabaseName = table["TABLE_CATALOG"].ToString(),
//                        SchemaName = table["TABLE_SCHEMA"].ToString(),
//                        TableName = table["TABLE_NAME"].ToString()
//                    });
//                }
//            }

//            return tables;
//        }

//        private List<string> GetPrimaryKeys(string schema, string tableName)
//        {
//            var sql = string.Format(@"SELECT 
//                                        COLUMN_NAME
//                                      FROM 
//                                        INFORMATION_SCHEMA.KEY_COLUMN_USAGE
//                                      WHERE
//                                        CONSTRAINT_SCHEMA = '{0}'
//                                        AND OBJECTPROPERTY(OBJECT_ID(constraint_name), 'IsPrimaryKey') = 1
//                                        AND table_name = '{1}'", schema, tableName);

//            return new QueryHelper(_connectionString).Read(sql, null, MakePrimaryKeys).ToList();
//        }

//        private List<DbColumn> GetColumns(string schema, string tableName, List<string> primaryKeys)
//        {
//            DataTable columns;
//            var lstColumns = new List<DbColumn>();

//            using (var connection = new SqlConnection(_connectionString))
//            {
//                connection.Open();
//                columns = connection.GetSchema(SqlClientMetaDataCollectionNames.Columns, new[] { null, schema, tableName, null });
//                connection.Close();
//            }

//            foreach (DataRow item in columns.Rows)
//            {
//                var column = new DbColumn
//                {
//                    ColumnName = item["COLUMN_NAME"].ToString(),
//                    DataType = item["DATA_TYPE"].ToString()
//                };

//                if (primaryKeys != null && primaryKeys.Contains(column.ColumnName))
//                    column.IsPrimary = true;

//                lstColumns.Add(column);
//            }

//            return lstColumns;
//        }

//        private List<DbRelation> GetRelations(string schema, string tableName)
//        {
//            var sqlQuery = String.Format(@"SELECT
//                                TABLE_NAME = FK.TABLE_NAME,
//                                COLUMN_NAME = FK_COLS.COLUMN_NAME,
//                                REFERENCED_TABLE_NAME = PK.TABLE_NAME,
//                                REFERENCED_COLUMN_NAME = PK_COLS.COLUMN_NAME
//                            FROM 
//                                INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS REF_CONST
//                            INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK 
//                                ON REF_CONST.CONSTRAINT_CATALOG = FK.CONSTRAINT_CATALOG
//                                AND REF_CONST.CONSTRAINT_SCHEMA = FK.CONSTRAINT_SCHEMA
//                                AND REF_CONST.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
//                                AND FK.CONSTRAINT_TYPE = 'FOREIGN KEY'
//                            INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK 
//                                ON REF_CONST.UNIQUE_CONSTRAINT_CATALOG = PK.CONSTRAINT_CATALOG
//                                AND REF_CONST.UNIQUE_CONSTRAINT_SCHEMA = PK.CONSTRAINT_SCHEMA
//                                AND REF_CONST.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
//                                AND PK.CONSTRAINT_TYPE = 'PRIMARY KEY'
//                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK_COLS 
//                                ON REF_CONST.CONSTRAINT_NAME = FK_COLS.CONSTRAINT_NAME
//                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE PK_COLS 
//                                ON PK.CONSTRAINT_NAME = PK_COLS.CONSTRAINT_NAME
//                            WHERE
//                                FK.CONSTRAINT_SCHEMA = '{0}'                                
//                                AND FK.TABLE_NAME = '{1}'", schema, tableName);

//            return new QueryHelper(_connectionString).Read(sqlQuery, null, MakeRelations).ToList();

//        }

//        private static readonly Func<IDataReader, string> MakePrimaryKeys = reader =>
//            reader["COLUMN_NAME"].AsString();

//        private static readonly Func<IDataReader, DbRelation> MakeRelations = reader =>
//            new DbRelation
//            {
//                ForeignTable = reader["TABLE_NAME"].AsString(),
//                ForeignKey = reader["COLUMN_NAME"].AsString(),
//                ReferencedTable = reader["REFERENCED_TABLE_NAME"].AsString(),
//                ReferencedKey = reader["REFERENCED_COLUMN_NAME"].AsString(),
//            };

//    }
//}
