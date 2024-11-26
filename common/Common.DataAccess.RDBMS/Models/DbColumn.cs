
namespace Common.DataAccess.RDBMS.Model
{
    public class DbColumn
    {
        public string ColumnName { get; set; }
        public bool IsPrimary { get; set; }
        public string DataType { get; set; } = "string";
        public bool? IsNullable { get; set; }
        public string FullyQualifiedName { get; set; }
        public string Size { get; set; }
        //public string TableCatalog { get; private set; }

        //public string TableSchema { get; private set; }

        //public string TableName { get; private set; }

        //public int? OrdinalPosition { get; private set; }

        //public string ColumnDefault { get; private set; }

        //public int? CharacterMaximumLength { get; private set; }

        //public int? CharacterOctetLength { get; private set; }

        //public int? NumericPrecision { get; private set; }

        //public int? NumericPrecisionRadix { get; private set; }

        //public int? NumericScale { get; private set; }

        //public long? DateTimePrecision { get; private set; }

        //public string CharacterSetCatalog { get; private set; }

        //public string CharacterSetSchema { get; private set; }

        //public string CharacterSetName { get; private set; }

        //public string CollationCatalog { get; private set; }

        //public bool? IsSparse { get; private set; }

        //public bool? IsColumnSet { get; private set; }

        //public bool? IsFileStream { get; private set; }
    }
}
