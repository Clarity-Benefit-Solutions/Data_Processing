using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Sylvan.Data.Csv;

namespace CoreUtils.Classes
{

    // utility class that wraps DbColumn (to use with Sylvan.CsvReader) and adds props needed for fileCheck and bulkCopy to DB
    public class TypedCsvColumn : DbColumn
    {
        public TypedCsvColumn(int sourceOrdinal, int destinationOrdinal, FormatType formatType = FormatType.Any,
            int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0, string defaultValue = "")
        {
            ColumnOrdinal = sourceOrdinal;
            DestinationOrdinal = destinationOrdinal;
            FormatType = formatType;
            MinLength = minLength;
            MaxLength = maxLength;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultValue = defaultValue;

            //
            AllowDBNull = true;
            DataType = Type.GetType("String");
        }

        public TypedCsvColumn(string sourceColumn, string destinationColumn, FormatType formatType = FormatType.Any,
            string fixedValue = "", int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0,
            string defaultValue = "")
        {
            ColumnName = sourceColumn;
            DestinationColumn = destinationColumn;
            FormatType = formatType;
            FixedValue = fixedValue;
            MinLength = minLength;
            MaxLength = maxLength;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultValue = defaultValue;

            //
            AllowDBNull = true;
            DataType = Type.GetType("String");
        }

        public string DestinationColumn { get; set; }
        public string FixedValue { get; set; }
        public int DestinationOrdinal { get; set; }
        public FormatType FormatType { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }

        public string SourceColumn => ColumnName;

        public int SourceOrdinal => ColumnOrdinal ?? -1;

        public int MaxValue { get; set; }
        public string DefaultValue { get; set; }

        public int MinValue { get; set; }
    }

    public class TypedCsvSchema : ICsvSchemaProvider, IEnumerable
    {
        public readonly List<TypedCsvColumn> Columns;

        public TypedCsvSchema()
        {
            Columns = new List<TypedCsvColumn>();
        }

        public int Count => Columns.Count;


        public DbColumn GetColumn(string name, int ordinal)
        {
            //throw new NotImplementedException();
            if (!Utils.IsBlank(name))
            {
                foreach (var column in Columns)
                    if (column.ColumnName == name)
                        return column;
            }
            else if (ordinal >= 0)
            {
                foreach (var column in Columns)
                    if (column.ColumnOrdinal == ordinal)
                        return column;
            }

            return null;
        }

        public IEnumerator GetEnumerator()
        {
            return Columns.GetEnumerator();
        }


        public TypedCsvSchema Add(int sourceOrdinal, int destinationOrdinal, FormatType formatType = FormatType.Any,
            int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0)
        {
            Columns.Add(new TypedCsvColumn(sourceOrdinal, destinationOrdinal, formatType, minLength, maxLength));
            return this;
        }

        public TypedCsvSchema Add(TypedCsvColumn column)
        {
            Columns.Add(column);
            return this;
        }

        public TypedCsvSchema Add(string sourceColumn, string destinationColumn, FormatType formatType = FormatType.Any,
            int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0)
        {
            Columns.Add(new TypedCsvColumn(sourceColumn, destinationColumn, formatType, null, minLength, maxLength));
            return this;
        }
    }

}