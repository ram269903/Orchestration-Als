﻿using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DataAccess.MsSql
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

            return tablesDetailsList.Select(table => table.DatabaseName + "." + table.SchemaName + "." + table.TableName).ToList();
        }

        public async Task<IEnumerable<DbTable>> GetTablesDetails(List<string> tablesList = null)
        {
            var tables = GetTables(tablesList);

            foreach (var table in tables)
            {
                //Get Primary Keys
                var primaryKeys = await GetPrimaryKeys(table.SchemaName, table.TableName);

                //Get Columns
                table.Columns = GetColumns(table.SchemaName, table.TableName, primaryKeys);

                //Get Relations
                table.Relations = await GetRelations(table.SchemaName, table.TableName);

            }

            return tables;
        }

        public async Task CreateTable(string tableName, List<DbColumn> columnDetails, string schema = null, bool dropCurrentTable = true)
        {
            string query;

            var columns = GenerateColumns(columnDetails);

            if (string.IsNullOrEmpty(schema))
                query = $"CREATE TABLE [{tableName}] ( {columns})";
            else
                query = $"CREATE TABLE [{schema}].[{tableName}] ( {columns})";

            //var query = "CREATE TABLE " + tableName + "(" + columns + ")";

            var tableExists = await CheckTableExists(tableName, schema);

            if (tableExists && !dropCurrentTable)
                return;

            if (tableExists)
                await DropTable(tableName, schema);

            _ = await new QueryHelper(_connectionString).ExecuteNonQuery(query, null);

        }

        public async Task<bool> CheckTableExists(string tableName, string schema = null)
        {
            string query;

            if (string.IsNullOrEmpty(schema))
                query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'";
            else
                query = $"SELECT COUNT(*) FROM information_schema.tables WHERE TABLE_SCHEMA = '{schema}' AND table_name = '{tableName}'";

            var count = Convert.ToInt64(await new QueryHelper(_connectionString).ExecuteScalar(query, null));

            return count >= 1;
        }

        public async Task DropTable(string tableName, string schema = null)
        {
            string query;

            if (string.IsNullOrEmpty(schema))
                query = $"DROP TABLE [{tableName}];";
            else
                query = $"DROP TABLE [{schema}].[{tableName}];";

            _ = await new QueryHelper(_connectionString).ExecuteNonQuery(query, null);
        }

        public async Task TruncateTable(string tableName, string schema = null)
        {
            string query;

            if (string.IsNullOrEmpty(schema))
                query = $"TRUNCATE TABLE [{tableName}];";
            else
                query = $"TRUNCATE TABLE [{schema}].[{tableName}];";

            _ = await new QueryHelper(_connectionString).ExecuteNonQuery(query, null);
        }

        private IEnumerable<DbTable> GetTables(ICollection<string> tablesList = null)
        {
            DataTable tblTables;
            var tables = new List<DbTable>();
            //var database = new DbHelper().GetDatabase(_connectionString);
            var serverName = new DbHelper().GetServerName(_connectionString);

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                tblTables = connection.GetSchema(SqlClientMetaDataCollectionNames.Tables, new[] { null, null, null, "BASE TABLE" });
                connection.Close();
            }

            foreach (DataRow table in tblTables.Rows)
            {
                var fullyQualifiedName = table["TABLE_CATALOG"] + "." + table["TABLE_SCHEMA"] + "." + table["TABLE_NAME"];

                if (tablesList == null || tablesList.Contains(fullyQualifiedName))
                {
                    tables.Add(new DbTable()
                    {
                        Server = serverName,
                        DatabaseName = table["TABLE_CATALOG"].ToString(),
                        SchemaName = table["TABLE_SCHEMA"].ToString(),
                        TableName = table["TABLE_NAME"].ToString()
                    });
                }
            }

            return tables;
        }

        private async Task<IEnumerable<string>> GetPrimaryKeys(string tableName, string schema = null)
        {
            var sql = $@"SELECT 
                            COLUMN_NAME
                        FROM 
                            INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                        WHERE
                            CONSTRAINT_SCHEMA = '{schema}'
                            AND OBJECTPROPERTY(OBJECT_ID(constraint_name), 'IsPrimaryKey') = 1
                            AND table_name = '{tableName}'";

            return await new QueryHelper(_connectionString).Read(sql, null, MakePrimaryKeys);
        }

        private IEnumerable<DbColumn> GetColumns(string schema, string tableName, IEnumerable<string> primaryKeys)
        {
            DataTable columns;
            var lstColumns = new List<DbColumn>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                columns = connection.GetSchema(SqlClientMetaDataCollectionNames.Columns, new[] { null, schema, tableName, null });
                connection.Close();
            }

            foreach (DataRow item in columns.Rows)
            {
                var fullyQualifiedName = $"{ item["TABLE_CATALOG"] }.{ item["TABLE_SCHEMA"]}.{ item["TABLE_NAME"]}.{ item["COLUMN_NAME"]}";

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

        private static string GenerateColumns(IEnumerable<DbColumn> columnDetails)
        {
            var columns = string.Empty;

            foreach (var column in columnDetails)
            {
                if (column.DataType == "dateTime")
                    column.Size = "7";

                if (column.DataType == "int32" || column.DataType == "int64" || column.DataType == "bit" || column.DataType == "double" || column.DataType == "bool")
                    column.Size = "";

                if (column.DataType == "string" && string.IsNullOrEmpty(column.Size?.Trim()))
                    column.Size = "50";

                var size = string.IsNullOrEmpty(column.Size) ? "" : $"({column.Size})";

                columns += $"[{column.ColumnName?.Trim().Replace(" ","_")}] [{GetNativeDataType(column.DataType)}] {size}, ";

                //if (column.DataType.ToLower() == "nvarchar")
                //    columns += " (max) ,";
                //else
                //    columns += ", ";
            }

            return columns.Trim().TrimEnd(',');
        }

        public static string GetNativeDataType(string genericDataType)
        {
            string dataType;

            switch (genericDataType.ToLower().Trim())
            {
                case "string":
                    dataType = "NVARCHAR";
                    break;
                case "int":
                case "int32":
                    dataType = "INT";
                    break;
                case "int64":
                    dataType = "BIGINT";
                    break;
                case "double":
                    dataType = "FLOAT";
                    break;
                case "boolean":
                case "bool":
                    dataType = "BIT";
                    break;
                case "datetime":
                    dataType = "DATETIME2";
                    break;
                default:
                    dataType = "NVARCHAR (MAX)";
                    break;
            }

            return dataType;
        }

        private static readonly Func<IDataReader, string> MakePrimaryKeys = reader => reader["COLUMN_NAME"].AsString();

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
