using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq;
using System.ComponentModel;
using System.Text;

namespace Common.DataAccess.RDBMS
{
    public class DataHelper
    {
        public static List<T> DataReaderToEntity<T>(IDataReader dr)
        {
            List<T> list = new List<T>();

            while (dr.Read())
            {
                T obj = Activator.CreateInstance<T>();
                foreach (PropertyInfo prop in obj.GetType().GetProperties())
                {
                    if (!object.Equals(dr[prop.Name], DBNull.Value))
                    {
                        prop.SetValue(obj, Convert.ChangeType(dr[prop.Name], prop.PropertyType), null);
                    }
                }

                list.Add(obj);
            }

            return list;
        }

        public static T ChangeType<T>(object value)
        {
            var t = typeof(T);

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return default;
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return (T)Convert.ChangeType(value, t);
        }

        public static string PrepareInsertStatement<T>(string tableName, bool includeIdColumn) 
        {
            var sql = new StringBuilder($"INSERT INTO {tableName} ");
            var properties = GetProperties<T>();

            var strProperties = string.Join(",", properties);
            if (includeIdColumn)
                strProperties.Replace("Id,", "");

            sql.Append($"( {strProperties} ) VALUES ");

            var values = string.Join(",@", strProperties.Split(','));

            sql.Append($"( {values} ) ");

            return sql.ToString();
        }
        
        public static List<string> GetProperties<T>()
        {
            var properties = typeof(T).GetProperties();

            return (from prop in properties
                    let attributes = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    where attributes.Length <= 0 || (attributes[0] as DescriptionAttribute)?.Description != "ignore"
                    select prop.Name).ToList();
        }
    }
}
