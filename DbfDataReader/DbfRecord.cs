using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;

namespace DbfDataReader
{
    public enum DbfRecordStatus : byte
    {
        Unknown = 0,
        EOF     = 0x1A,
        Deleted = 0x2A,
        Valid   = 0x20
    }

    public class DbfRecord : DbDataRecord
    {
        public DbfTable        Table  { get; }
        public Int64           Offset { get; }
        public DbfRecordStatus Status { get; }

        private readonly Object[] values;

        public DbfRecord(DbfTable table, Int64 offset, DbfRecordStatus status, Object[] values)
        {
            // NOTE: It might be an idea to create a shallow-copy of `values` in case the consumer mutates the array after construction.

            if( table == null ) throw new ArgumentNullException(nameof(table));
            if( status != DbfRecordStatus.Deleted && status != DbfRecordStatus.Valid ) throw new ArgumentOutOfRangeException(nameof(status), status, "Value must be either " + nameof(DbfRecordStatus.Valid) + " or " + nameof(DbfRecordStatus.Deleted) + ".");
            if( values == null ) throw new ArgumentNullException(nameof(values));

            this.Table  = table;
            this.Offset = offset;
            this.Status = status;
            this.values = values;

#if DEBUG
            // Validate values:
            if( values.Any( o => Object.ReferenceEquals( o, null ) ) ) throw new ArgumentException("Values arrays cannot contain CLR null values. Use DBNull.Value to represent NULL values.", nameof(values));
#endif

            this.Values = new ReadOnlyCollection<Object>( this.values );
        }

        public ReadOnlyCollection<Object> Values { get; }

        #region DbDataRecord

        #region Get typed values:

        public override Boolean GetBoolean(Int32 i)
        {
            return (Boolean)this.values[i];
        }

        public override Byte GetByte(Int32 i)
        {
            return (Byte)this.values[i];
        }

        public override Int64 GetBytes(Int32 i, Int64 dataIndex, Byte[] buffer, Int32 bufferIndex, Int32 length)
        {
            Byte[] sourceBuffer = (Byte[])this.values[i];

            Int32 actualLength = (Int32)Math.Min( sourceBuffer.Length - dataIndex, length );

            Array.Copy( sourceBuffer, dataIndex, buffer, bufferIndex, actualLength );

            return actualLength;
        }

        public override Char GetChar(Int32 i)
        {
            return (Char)this.values[i];
        }

        public override Int64 GetChars(Int32 i, Int64 dataIndex, Char[] buffer, Int32 bufferIndex, Int32 length)
        {
            String source = (String)this.values[i];

            Int32 actualLength = (Int32)Math.Min( source.Length - dataIndex, length );

            for( Int32 sourceIdx = (Int32)dataIndex; sourceIdx < actualLength; sourceIdx++ )
            {
                buffer[ bufferIndex ] = source[ sourceIdx ];
                bufferIndex++;
            }

            return actualLength;
        }

        public override DateTime GetDateTime(Int32 i)
        {
            return (DateTime)this.values[i];
        }

        public override Decimal GetDecimal(Int32 i)
        {
            return (Decimal)this.values[i];
        }

        public override Double GetDouble(Int32 i)
        {
            return (Double)this.values[i];
        }

        public override Single GetFloat(Int32 i)
        {
            return (Single)this.values[i];
        }

        public override Guid GetGuid(Int32 i)
        {
            return (Guid)this.values[i];
        }

        public override Int16 GetInt16(Int32 i)
        {
            return (Int16)this.values[i];
        }

        public override Int32 GetInt32(Int32 i)
        {
            return (Int32)this.values[i];
        }

        public override Int64 GetInt64(Int32 i)
        {
            return (Int64)this.values[i];
        }

        public override String GetString(Int32 i)
        {
            return (String)this.values[i];
        }

        #endregion

        public override Int32 FieldCount => this.values.Length;

        public override Object this[String name] => this.GetValue( this.GetOrdinal( name ) ); // I'm surprised DbDataRecord doesn't provide this as a default implementation.

        public override Object this[Int32 i] => this.values[i];

        /// <summary>Returns the dBase column type name.</summary>
        public override String GetDataTypeName(Int32 i)
        {
            return this.Table.Columns[i].ColumnType.ToString();
        }

        public override Type GetFieldType(Int32 i)
        {
            return this.values[i].GetType();
        }

        public override String GetName(Int32 i)
        {
            return this.Table.Columns[i].Name;
        }

        public override Int32 GetOrdinal(String name)
        {
            return this.Table.ColumnsByName[ name ].Index;
        }

        public override Object GetValue(Int32 i)
        {
            return (Decimal)this.values[i];
        }

        public override Int32 GetValues(Object[] values)
        {
            Int32 max = Math.Min( this.values.Length, values.Length );
            for( Int32 i = 0; i < max; i++ )
            {
                values[i] = this.values[i];
            }
            return max;
        }

        public override Boolean IsDBNull(Int32 i)
        {
            return Object.ReferenceEquals( this.values[i], DBNull.Value );
        }

        #endregion

        /*        private static IDbfValue CreateDbfValue(DbfColumn dbfColumn, DbfMemoFile memo)
                {
                    IDbfValue value;

                    switch (dbfColumn.ColumnType)
                    {
                        case DbfColumnType.Number:
                            if (dbfColumn.DecimalCount == 0)
                            {
                                value = new DbfValueInt(dbfColumn.Length);
                            }
                            else
                            {
                                value = new DbfValueDecimal(dbfColumn.Length, dbfColumn.DecimalCount);
                            }
                            break;
                        case DbfColumnType.Signedlong:
                            value = new DbfValueLong(dbfColumn.Length);
                            break;
                        case DbfColumnType.Float:
                            value = new DbfValueFloat(dbfColumn.Length);
                            break;
                        case DbfColumnType.Currency:
                            value = new DbfValueCurrency(dbfColumn.Length, dbfColumn.DecimalCount);
                            break;
                        case DbfColumnType.Date:
                            value = new DbfValueDate(dbfColumn.Length);
                            break;
                        case DbfColumnType.DateTime:
                            value = new DbfValueDateTime(dbfColumn.Length);
                            break;
                        case DbfColumnType.Boolean:
                            value = new DbfValueBoolean(dbfColumn.Length);
                            break;
                        case DbfColumnType.Memo:
                            value = new DbfValueMemo(dbfColumn.Length, memo);
                            break;
                        case DbfColumnType.Double:
                            value = new DbfValueDouble(dbfColumn.Length);
                            break;
                        case DbfColumnType.General:
                        case DbfColumnType.Character:
                            value = new DbfValueString(dbfColumn.Length);
                            break;
                        default:
                            value = new DbfValueNull(dbfColumn.Length);
                            break;
                    }

                    return value;
                }

                public bool Read(BinaryReader binaryReader)
                {
                    if (binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
                    {
                        return false;
                    }

                    try
                    {
                        var value = binaryReader.ReadByte();
                        if (value == EndOfFile)
                        {
                            return false;
                        }

                        IsDeleted = (value == 0x2A);

                        foreach (var dbfValue in Values)
                        {
                            dbfValue.Read(binaryReader);
                        }
                        return true;
                    }
                    catch (EndOfStreamException)
                    {
                        return false;
                    }
                }



                public object GetValue(int ordinal)
                {
                    var dbfValue = Values[ordinal];
                    return dbfValue.GetValue();
                }

                public T GetValue<T>(int ordinal)
                {
                    var dbfValue = Values[ordinal] as DbfValue<T>;
                    return dbfValue.Value;
                }*/
    }
}