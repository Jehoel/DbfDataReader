using System;
using System.Collections;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Security.AccessControl;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DbfDataReader
{
    public sealed class DbfDataReader : DbDataReader
    {
        private readonly FileStream stream;
        private          Boolean    atEof;
        private          Boolean    isDisposed;

        public DbfTable Table { get; }

        public Encoding Encoding { get; }

        private const FileOptions _fileOptions = FileOptions.Asynchronous;

        internal DbfDataReader(DbfTable table, String path, Boolean randomAccess, Encoding encoding)
        {
            FileOptions fileOptions = FileOptions.Asynchronous;
            if( randomAccess ) fileOptions |= FileOptions.RandomAccess;
            else               fileOptions |= FileOptions.SequentialScan;

            ///

            this.Table    = table;
            this.Encoding = encoding;

            this.stream = new FileStream( path, FileMode.Open, FileSystemRights.ReadData, FileShare.ReadWrite, 4096, fileOptions );
        }

        private DbfRecord current;

        public DbfRecord Current
        {
            get
            {
                if( this.current == null ) throw new InvalidOperationException("No " + nameof(DbfRecord) + " has been read by this " + nameof(DbfDataReader) + " instance.");
                return this.current;
            }
        }

        #region DbDataReader

        public override void Close()
        {
            // `DbDataReader.Close()` is a NOOP.
            // `DbDataReader.Dispose(Boolean disposing)` calls Close()...
            
            this.stream.Dispose();
            this.isDisposed = true;
        }

        #region IDataRecord / Get typed values

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
                
                if( this.atEof ) return false;

                return true;
            }
        }

        public override Boolean IsClosed => this.isDisposed;

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

        public override Boolean Read()
        {
            return DbfTable.Read(DbfRecord);
        }

        public async Task<Boolean> ReadAsync()
        {
            throw new NotImplementedException();
        }
    }
}