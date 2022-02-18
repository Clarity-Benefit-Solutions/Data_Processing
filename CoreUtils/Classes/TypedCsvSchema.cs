﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sylvan.Data.Csv;

namespace CoreUtils.Classes
{
    // utility class that wraps DbColumn (to use with Sylvan.CsvReader) and adds props needed for fileCheck and bulkCopy to DB
    public class TypedCsvColumn : DbColumn
    {
        public string DestinationColumn { get; set; }
        public int DestinationOrdinal { get; set; }
        public FormatType FormatType { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }

        public string SourceColumn
        {
            get
            {
                return this.ColumnName;

            }
        }
        public int SourceOrdinal
        {
            get
            {
                return this.ColumnOrdinal ?? -1;

            }
        }

        public TypedCsvColumn(int sourceOrdinal, int destinationOrdinal, FormatType formatType = FormatType.Any, int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0)
        {
            this.ColumnOrdinal = sourceOrdinal;
            this.DestinationOrdinal = destinationOrdinal;
            this.FormatType = formatType;
            this.MinLength = minLength;
            this.MaxLength = maxLength;
            this.MinValue = minValue;
            this.MaxValue = maxValue;

            //
            this.AllowDBNull = true;
            this.DataType = Type.GetType("String");

        }

        public int MaxValue { get; set; }

        public int MinValue { get; set; }

        public TypedCsvColumn(string sourceColumn, string destinationColumn, FormatType formatType = FormatType.Any, int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0)
        {
            this.ColumnName = sourceColumn;
            this.DestinationColumn = destinationColumn;
            this.FormatType = formatType;
            this.MinLength = minLength;
            this.MaxLength = maxLength;
            this.MinValue = minValue;
            this.MaxValue = maxValue;

            //
            this.AllowDBNull = true;
            this.DataType = Type.GetType("String");

        }

    }

    public class TypedCsvSchema : ICsvSchemaProvider, IEnumerable
    {
        public readonly List<TypedCsvColumn> Columns;

        public int Count
        {
            get
            {
                return this.Columns.Count;

            }
        }

        public TypedCsvSchema()
        {
            this.Columns = new List<TypedCsvColumn>();
        }


        public TypedCsvSchema Add(int sourceOrdinal, int destinationOrdinal, FormatType formatType = FormatType.Any, int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0)
        {
            this.Columns.Add(new TypedCsvColumn(sourceOrdinal, destinationOrdinal, formatType, minLength, maxLength));
            return this;
        }
        public TypedCsvSchema Add(TypedCsvColumn column)
        {
            this.Columns.Add(column);
            return this;
        }
        public TypedCsvSchema Add(string sourceColumn, string destinationColumn, FormatType formatType = FormatType.Any, int minLength = 0, int maxLength = 0, int minValue = 0, int maxValue = 0)
        {
            this.Columns.Add(new TypedCsvColumn(sourceColumn, destinationColumn, formatType, minLength, maxLength));
            return this;
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
    }
}
