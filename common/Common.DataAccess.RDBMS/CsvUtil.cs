//using CsvHelper;
//using CsvHelper.Configuration;
//using System.Data;
//using System.IO;
//using System.Linq;

//namespace Common.DataAccess.RDBMS
//{
//    public static class CsvUtil
//    {
//        public static void GenerateCsvFile(IDataReader reader, string file, string separator = ",", bool includeHeaders = true)
//        {
//            var hasHeaderBeenWritten = false;

//            using (var streamWriter = new StreamWriter(file))
//            {
//                var csvWriter = new CsvWriter(streamWriter);
//                csvWriter.Configuration.HasHeaderRecord = includeHeaders;
//                csvWriter.Configuration.Delimiter = separator;

//                while (reader.Read())
//                {
//                    if (!hasHeaderBeenWritten)
//                    {
//                        for (var i = 0; i < reader.FieldCount; i++)
//                        {
//                            csvWriter.WriteField(reader.GetName(i));
//                        }
//                        csvWriter.NextRecord();
//                        hasHeaderBeenWritten = true;
//                    }

//                    for (var i = 0; i < reader.FieldCount; i++)
//                    {
//                        csvWriter.WriteField(reader[i]);
//                    }
//                    csvWriter.NextRecord();
//                }
//            }
//        }

//        public static void GenerateCsvFile(DataTable sourceTable, string file, string separator = ",", bool includeHeaders = true)
//        {
//            using (var streamWriter = new StreamWriter(file))
//            {
//                var csvWriter = new CsvWriter(streamWriter);
//                csvWriter.Configuration.HasHeaderRecord = includeHeaders;
//                csvWriter.Configuration.Delimiter = separator;

//                foreach (DataColumn column in sourceTable.Columns)
//                {
//                    csvWriter.WriteField(column.ColumnName);
//                }

//                csvWriter.NextRecord();

//                foreach (DataRow row in sourceTable.Rows)
//                {
//                    for (var i = 0; i < sourceTable.Columns.Count; i++)
//                    {
//                        csvWriter.WriteField(row[i]);
//                    }
//                    csvWriter.NextRecord();
//                }
//            }
//        }

//        public static DataTable GetDataTableFromCsv(string filePath, int? maxRows = null, string separator = ",", bool includeHeaders = true)
//        {
//            var dt = new DataTable();

//            using (var stream = File.OpenRead(filePath))
//            {
//                using (var reader = new StreamReader(stream))
//                {
//                    using (var csvReader = new CsvReader(reader))
//                    {
//                        csvReader.Configuration.HasHeaderRecord = includeHeaders;
//                        csvReader.Configuration.ShouldSkipRecord = record => record.All(string.IsNullOrEmpty);
//                        csvReader.Configuration.TrimOptions = TrimOptions.Trim;
//                        csvReader.Configuration.Delimiter = separator;
//                        //csvReader.Configuration.CultureInfo = CultureInfo.CurrentCulture;

//                        int i = 0;
//                        while (csvReader.Read())
//                        {
//                            if (i == 0)
//                            {
//                                foreach (var field in csvReader.Context.HeaderRecord)
//                                {
//                                    dt.Columns.Add(field);
//                                }
//                            }

//                            if (maxRows != null && i > maxRows.Value)
//                            {
//                                break;
//                            }

//                            var row = dt.NewRow();

//                            foreach (var field in csvReader.Context.HeaderRecord)
//                            {
//                                row[field] = csvReader.GetField(field);
//                            }

//                            dt.Rows.Add(row);

//                            i += 1;
//                        }
//                    }
//                }
//            }

//            return dt;
//        }

//        public static DataTable GetDataTableFromCsv(DataTable dataTableWithColumns, string filePath, string separator = ",", bool includeHeaders = true)
//        {
//            dataTableWithColumns.Clear();

//            using (var stream = File.OpenRead(filePath))
//            {
//                using (var reader = new StreamReader(stream))
//                {
//                    using (var csvReader = new CsvReader(reader))
//                    {
//                        csvReader.Configuration.HasHeaderRecord = includeHeaders;
//                        csvReader.Configuration.ShouldSkipRecord = record => record.All(string.IsNullOrEmpty);
//                        csvReader.Configuration.TrimOptions = TrimOptions.Trim;
//                        csvReader.Configuration.Delimiter = separator;

//                        while (csvReader.Read())
//                        {
//                            var row = dataTableWithColumns.NewRow();
//                            foreach (DataColumn column in dataTableWithColumns.Columns)
//                            {
//                                row[column.ColumnName] = csvReader.GetField(column.DataType, column.ColumnName);
//                            }
//                            dataTableWithColumns.Rows.Add(row);
//                        }
//                    }
//                }
//            }

//            return dataTableWithColumns;

//        }
//    }
//}
