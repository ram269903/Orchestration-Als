using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Teradata.Client.Provider;

namespace Common.DataAccess.Teradata
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

            return tablesDetailsList.Select(table => table.SchemaName + "." + table.TableName).ToList();
        }

        public async Task<bool> CheckTableExists(string schema, string tableName)
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

            _ = await new QueryHelper(_connectionString).ExecuteNonQuery(query, null);
        }

        public Task<DataTable> GetTableData(string tableName, string schema = null)
        {
            var query = $"select * from [{schema}].[{tableName}];";

            return new QueryHelper(_connectionString).ExecuteQuery(query);
        }



        public async Task<IEnumerable<DbTable>> GetTablesDetails(List<string> tablesList = null)
        {
            var tables = GetTables(tablesList);

            foreach (var table in tables)
            {
                //Get Primary Keys
                var primaryKeys = GetPrimaryKeys(table.SchemaName, table.TableName);

                //Get Columns
                table.Columns = GetColumns(table.SchemaName, table.TableName, primaryKeys);

                //Get Relations
                //table.Relations = await GetRelations(table.SchemaName, table.TableName);

            }

            return tables;
        }

        public void InsertCsvData(string schema, string tableName, DataTable data, bool isAppend, int? batchSize)
        {
            throw new NotImplementedException();
        }

        public async Task TruncateTable(string schema, string tableName)
        {
            var query = $"TRUNCATE TABLE [{schema}].[{tableName}];";

            _ = await new  QueryHelper(_connectionString).ExecuteNonQuery(query, null);
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

        private static string GetNativeDataType(string genericDataType)
        {
            string dataType;

            switch (genericDataType.ToLower().Trim())
            {
                case "string":
                    dataType = "NVARCHAR (MAX)";
                    break;
                case "int":
                    dataType = "INT";
                    break;
                case "decimal":
                    dataType = "FLOAT";
                    break;
                case "boolean":
                    dataType = "BIT";
                    break;
                case "datetime":
                    dataType = "DATETIME";
                    break;
                default:
                    dataType = "nvarchar (max)";
                    break;
            }

            return dataType;
        }

        private IEnumerable<DbTable> GetTables(ICollection<string> tablesList = null)
        {
            DataTable tblTables;
            var tables = new List<DbTable>();
            var database = new DbHelper().GetDatabase(_connectionString);

            var serverName = database.Server;

            using (var connection = new TdConnection(_connectionString))
            {
                connection.Open();
                tblTables = connection.GetSchema("Tables", new[] {$"{database.DatabaseName}"});

                foreach (DataRow table in tblTables.Rows)
                {
                    var fullyQualifiedName = table["table_schema"] + "." + table["table_name"];

                    if ((tablesList == null || tablesList.Contains(fullyQualifiedName)) && table["TABLE_TYPE"].ToString() == "TABLE")
                    {
                        tables.Add(new DbTable()
                        {
                            Server = serverName,
                            DatabaseName = database.DatabaseName,
                            SchemaName = table["table_schema"].ToString(),
                            TableName = table["table_name"].ToString()
                        });
                    }
                }

                connection.Close();
            }

            return tables;
        }

        private IEnumerable<string> GetPrimaryKeys(string schema, string tableName)
        {
            DataTable columns;
            var lstKeys = new List<string>();

            var database = new DbHelper().GetDatabase(_connectionString);

            using (var connection = new TdConnection(_connectionString))
            {
                connection.Open();
                columns = connection.GetSchema("PrimaryKeys", new string[] { $"{database.DatabaseName}", tableName });
                connection.Close();
            }

            foreach (DataRow item in columns.Rows)
            {
                var fullyQualifiedName = $"{ item["TABLE_SCHEMA"]}.{ item["TABLE_NAME"]}.{ item["COLUMN_NAME"]}";

                lstKeys.Add(fullyQualifiedName);
            }

            return lstKeys;
        }

        private IEnumerable<DbColumn> GetColumns(string schema, string tableName, IEnumerable<string> primaryKeys)
        {
            DataTable columns;
            var lstColumns = new List<DbColumn>();

            var database = new DbHelper().GetDatabase(_connectionString);

            using (var connection = new TdConnection(_connectionString))
            {
                connection.Open();
                columns = connection.GetSchema("Columns", new string[] { $"{database.DatabaseName}", tableName  });
                connection.Close();
            }

            foreach (DataRow item in columns.Rows)
            {
                var fullyQualifiedName = $"{ item["TABLE_SCHEMA"]}.{ item["TABLE_NAME"]}.{ item["COLUMN_NAME"]}";

                var column = new DbColumn
                {
                    ColumnName = item["column_name"].ToString(),
                    DataType = item["column_type"].ToString(),
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
            var sqlQuery = $@"SELECT
                                TABLE_NAME = FK.TABLE_NAME,
                                COLUMN_NAME = FK_COLS.COLUMN_NAME,
                                REFERENCED_TABLE_NAME = PK.TABLE_NAME,
                                REFERENCED_COLUMN_NAME = PK_COLS.COLUMN_NAME
                            FROM 
                                INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS REF_CONST
                            INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK 
                                ON REF_CONST.CONSTRAINT_CATALOG = FK.CONSTRAINT_CATALOG
                                AND REF_CONST.CONSTRAINT_SCHEMA = FK.CONSTRAINT_SCHEMA
                                AND REF_CONST.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
                                AND FK.CONSTRAINT_TYPE = 'FOREIGN KEY'
                            INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK 
                                ON REF_CONST.UNIQUE_CONSTRAINT_CATALOG = PK.CONSTRAINT_CATALOG
                                AND REF_CONST.UNIQUE_CONSTRAINT_SCHEMA = PK.CONSTRAINT_SCHEMA
                                AND REF_CONST.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
                                AND PK.CONSTRAINT_TYPE = 'PRIMARY KEY'
                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK_COLS 
                                ON REF_CONST.CONSTRAINT_NAME = FK_COLS.CONSTRAINT_NAME
                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE PK_COLS 
                                ON PK.CONSTRAINT_NAME = PK_COLS.CONSTRAINT_NAME
                            WHERE
                                FK.CONSTRAINT_SCHEMA = '{schema}'                                
                                AND FK.TABLE_NAME = '{tableName}'";

            return await new QueryHelper(_connectionString).Read(sqlQuery, null, MakeRelations);

        }

        private static readonly Func<IDataReader, string> MakePrimaryKeys = reader =>
            reader["COLUMN_NAME"].AsString();

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
