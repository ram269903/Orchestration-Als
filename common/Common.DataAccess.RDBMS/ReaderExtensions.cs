using System;
using System.Data;

namespace Common.DataAccess.RDBMS
{
    public static class ReaderExtensions
    {

        public static DateTime? GetNullableDateTime(this IDataReader reader, string name)
        {
            var col = reader.GetOrdinal(name);
            return reader.IsDBNull(col) ? null : (DateTime?)reader.GetDateTime(col);
        }

        public static int? GetNullableInt(this IDataReader reader, string name)
        {
            var col = reader.GetOrdinal(name);
            return reader.IsDBNull(col) ? null : (int?)reader.GetInt32(col);
        }
    }
}
