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
        public TypedCsvColumn() : base()
        {
        }
        public TypedCsvColumn(int sourceOrdinal, int destinationOrdinal, FormatType formatType = FormatType.Any,
            int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0, string defaultValue = "")
        {
            this.ColumnOrdinal = sourceOrdinal;
            this.DestinationOrdinal = destinationOrdinal;
            this.FormatType = formatType;
            this.MinLength = minLength;
            this.MaxLength = maxLength;
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.DefaultValue = defaultValue;

            //
            this.AllowDBNull = true;
            this.DataType = Type.GetType("String");
        }

        public TypedCsvColumn(string sourceColumn, string destinationColumn, FormatType formatType = FormatType.Any,
            string fixedValue = "", int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0,
            string defaultValue = "")
        {
            this.ColumnName = sourceColumn;
            this.DestinationColumn = destinationColumn;
            this.FormatType = formatType;
            this.FixedValue = fixedValue;
            this.MinLength = minLength;
            this.MaxLength = maxLength;
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.DefaultValue = defaultValue;

            //
            this.AllowDBNull = true;
            this.DataType = Type.GetType("String");
        }

        public string DestinationColumn { get; set; }
        public string FixedValue { get; set; }
        public int DestinationOrdinal { get; set; }
        public FormatType FormatType { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }

        public string SourceColumn
        {
            get { return this.ColumnName; }
        }

        public int SourceOrdinal
        {
            get { return this.ColumnOrdinal ?? -1; }
        }

        public int MaxValue { get; set; }
        public string DefaultValue { get; set; }

        public int MinValue { get; set; }
    }

    public class TypedCsvSchema : ICsvSchemaProvider, IEnumerable
    {
        public readonly List<TypedCsvColumn> Columns;

        public TypedCsvSchema()
        {
            this.Columns = new List<TypedCsvColumn>();
        }

        public int Count
        {
            get { return this.Columns.Count; }
        }


        public DbColumn GetColumn(string name, int ordinal)
        {
            //throw new NotImplementedException();
            if (!Utils.IsBlank(name))
            {
                foreach (var column in this.Columns)
                {
                    if (column.ColumnName == name)
                    {
                        return column;
                    }
                }
            }
            else if (ordinal >= 0)
            {
                foreach (var column in this.Columns)
                {
                    if (column.ColumnOrdinal == ordinal)
                    {
                        return column;
                    }
                }
            }

            return null;
        }

        public IEnumerator GetEnumerator()
        {
            return this.Columns.GetEnumerator();
        }


        public TypedCsvSchema Add(int sourceOrdinal, int destinationOrdinal, FormatType formatType = FormatType.Any,
            int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0)
        {
            this.Columns.Add(new TypedCsvColumn(sourceOrdinal, destinationOrdinal, formatType, minLength, maxLength));
            return this;
        }

        public TypedCsvSchema Add(TypedCsvColumn column)
        {
            this.Columns.Add(column);
            return this;
        }

        public TypedCsvSchema Add(string sourceColumn, string destinationColumn, FormatType formatType = FormatType.Any,
            int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0)
        {
            this.Columns.Add(
                new TypedCsvColumn(sourceColumn, destinationColumn, formatType, null, minLength, maxLength));
            return this;
        }
    }

    public class CobraTypedCsvColumn : TypedCsvColumn
    {
        public CobraTypedCsvColumn(string sourceColumn, FormatType formatType = FormatType.Any,
           int maxLength = 0, int isRequired=0, string notes = "", string possibleValues = "") : base()
        {
            this.ColumnName = sourceColumn;
            this.DestinationColumn = sourceColumn;
            this.FormatType = formatType;
            this.FixedValue = "";
            this.MinLength = isRequired > 0 ? 1 : 0;
            this.MaxLength = maxLength;
            this.MinValue = 0;
            this.MaxValue = 0;
            this.DefaultValue = "";

            //
            this.AllowDBNull = true;
            this.DataType = Type.GetType("String");
        }


    }

} // NAMESPACE