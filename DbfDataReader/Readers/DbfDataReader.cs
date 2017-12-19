using System;
using System.Collections;
using System.Data.Common;
using System.Text;

namespace Dbf
{
    public abstract class DbfDataReader : DbDataReader
    {
        public DbfTable Table { get; }

        protected ValueReader ValueReader { get; }

        internal DbfDataReader(DbfTable table)
        {
            this.Table = table;

            if( table.Header.IsFoxPro )
            {
                this.ValueReader = FoxProValueReader.Instance;
            }
            else if( table.Header.Version == (Byte)0x5 )
            {
                this.ValueReader = DBase5ValueReader.Instance;
            }
            else
            {
                this.ValueReader = ValueReader.Instance;
            }
        }

        private DbfRecord current;

        public DbfRecord Current
        {
            get
            {
                if( this.current == null ) throw new InvalidOperationException("No " + nameof(DbfRecord) + " has been read by this " + nameof(DbfDataReader) + " instance.");
                return this.current;
            }
            protected set
            {
                this.current = value;
            }
        }

        public override abstract void Close();

        #region DbDataReader

        #region IDataRecord: Get typed values

        public override Boolean GetBoolean(Int32 ordinal)
        {
            return this.Current.GetBoolean( ordinal );
        }

        public override Byte GetByte(Int32 ordinal)
        {
           return this.Current.GetByte( ordinal );
        }

        public override Int64 GetBytes(Int32 ordinal, Int64 dataOffset, Byte[] buffer, Int32 bufferOffset, Int32 length)
        {
            return this.Current.GetBytes( ordinal, dataOffset, buffer, bufferOffset, length );
        }

        public override Char GetChar(Int32 ordinal)
        {
            return this.Current.GetChar( ordinal );
        }

        public override Int64 GetChars(Int32 ordinal, Int64 dataOffset, Char[] buffer, Int32 bufferOffset, Int32 length)
        {
           return this.Current.GetChars( ordinal, dataOffset, buffer, bufferOffset, length );
        }

        public override DateTime GetDateTime(Int32 ordinal)
        {
            return this.Current.GetDateTime( ordinal );
        }

        public override Decimal GetDecimal(Int32 ordinal)
        {
            return this.Current.GetDecimal( ordinal );
        }

        public override Double GetDouble(Int32 ordinal)
        {
            return this.Current.GetDouble( ordinal );
        }

        public override Single GetFloat(Int32 ordinal)
        {
            return this.Current.GetFloat( ordinal );
        }

        public override Guid GetGuid(Int32 ordinal)
        {
            return this.Current.GetGuid( ordinal );
        }

        public override Int16 GetInt16(Int32 ordinal)
        {
            return this.Current.GetInt16( ordinal );
        }

        public override Int32 GetInt32(Int32 ordinal)
        {
            return this.Current.GetInt32( ordinal );
        }

        public override Int64 GetInt64(Int32 ordinal)
        {
            return this.Current.GetInt64( ordinal );
        }

        public override String GetString(Int32 ordinal)
        {
            return this.Current.GetString( ordinal );
        }

        #endregion

        public override Int32 Depth => 0; // always zero depth.

        public override Int32 FieldCount => this.Current.FieldCount;

        /// <summary>Returns false if the current table file contains no records or if this reader has reached EOF.</summary>
        public override Boolean HasRows
        {
            get
            {
                if( this.IsClosed ) throw new InvalidOperationException("This " + nameof(DbfDataReader) + " is closed.");

                if( this.Table.Header.RecordCount == 0 ) return false;
                
                if( this.EOF ) return false;

                return true;
            }
        }

        public override Int32 RecordsAffected => 0; // we're read-only

        public override Object this[String name] => this.Current[ name ];

        public override Object this[Int32 ordinal] => this.Current[ ordinal ];

        public override String GetDataTypeName(Int32 ordinal) => this.Current.GetDataTypeName( ordinal );

        public override IEnumerator GetEnumerator()
        {
            return new DbEnumerator( this, closeReader: false );
        }

        public override Type GetFieldType(Int32 ordinal) => this.Current.GetFieldType( ordinal );

        public override String GetName(Int32 ordinal) => this.Current.GetName( ordinal );

        public override Int32 GetOrdinal(String name) => this.Current.GetOrdinal( name );

        public override Object GetValue(Int32 ordinal) => this.Current.GetValue( ordinal );

        public override Int32 GetValues(Object[] values) => this.Current.GetValues( values );

        public override Boolean IsDBNull(Int32 ordinal) => this.Current.IsDBNull( ordinal );

        public override Boolean NextResult()
        {
            // xBase does not have a concept of batched results.
            return false;
        }

        #endregion

        protected abstract Boolean EOF { get; }

        public abstract Encoding TextEncoding { get; }

        // DbDataReader already has a `public virtual Task<Boolean> ReadAsync(CancellatioinToken)` method that calls `this.Read()` synchronously.
        // So `SyncDbfDataReader` doesn't need to do anything.

        protected enum DbfReadResult
        {
            Read,
            Eof,
            Skipped
        }

        protected Int64 GetRecordFileOffset(Int32 recordIndex)
        {
            if( recordIndex < 0 ) throw new ArgumentOutOfRangeException( nameof(recordIndex), recordIndex, "Value cannot be less than zero." );

            Int64 offset = this.Table.Header.HeaderLength + (this.Table.Header.RecordLength * recordIndex);
            return offset;
        }

        // There is no async Seek method, so this is shared by both implementations.
        public abstract Boolean Seek(Int32 recordIndex);
    }

    [Flags]
    public enum DbfDataReaderOptions
    {
        None          = 0,
        /// <summary>DBF EOF values will be ignored and the entire DBF file will be read.</summary>
        IgnoreEof     = 1 << 0,
        /// <summary>Records with DbfRecordStatus.Deleted values will be read instead of being skipped-over.</summary>
        AllowDeleted = 1 << 1,
        /// <summary>Records with DbfRecordStatus values other than Deleted or Valid will be read instead of being skipped-over.</summary>
        AllowInvalid  = 1 << 2
    }
}