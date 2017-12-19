using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Dbf
{
    public class SyncDbfDataReader : DbfDataReader
    {
        private readonly FileStream   fileStream;
        private readonly BinaryReader binaryReader;
        private          Boolean      isDisposed;
        private          Boolean      isEof;

        private readonly DbfDataReaderOptions options;

        public override Encoding TextEncoding { get; }

        internal SyncDbfDataReader(DbfTable table, Boolean randomAccess, Encoding textEncoding, DbfDataReaderOptions options)
            : base( table )
        {
            FileStream stream = Utility.OpenFileForReading( table.File.FullName, randomAccess, async: false );
            if( !stream.CanRead || !stream.CanSeek )
            {
                stream.Dispose();
                throw new InvalidOperationException("The created FileStream could not perform both Read and Seek operations.");
            }

            this.fileStream = stream;

            this.binaryReader = new BinaryReader( this.fileStream, Encoding.ASCII, leaveOpen: true );

            this.TextEncoding = textEncoding;

            this.options = options;
        }

        public override void Close()
        {
            this.binaryReader.Dispose();
            this.fileStream.Dispose();
            this.isDisposed = true;
        }

        public override Boolean IsClosed => this.isDisposed;

        protected override Boolean Eof => this.isEof;

        protected Boolean SetEOF()
        {
            if( this.Eof ) return true;

            if( this.binaryReader.BaseStream.Position == this.binaryReader.BaseStream.Length )
            {
                this.isEof = true;
            }

            return this.Eof;
        }

        public override Boolean Read()
        {
            if( this.Eof ) return false;

            DbfReadResult result;
            do
            {
                result = this.ReadImpl();
            }
            while( result == DbfReadResult.Skipped ); // so EOF and Read won't cause a loop iteration.

            return result == DbfReadResult.Read;
        }

        private DbfReadResult ReadImpl()
        {
            if( this.SetEOF() ) return DbfReadResult.Eof;

            Int64 offset = this.binaryReader.BaseStream.Position;

            DbfRecordStatus recordStatus = (DbfRecordStatus)this.binaryReader.ReadByte();
            if( recordStatus == DbfRecordStatus.Deleted )
            {
                if( this.options.HasFlag(DbfDataReaderOptions.AllowDeleted) )
                {
                    // NOOP.
                }
                else
                {
                    this.binaryReader.BaseStream.Seek( this.Table.Header.RecordDataLength, SeekOrigin.Current ); // skip-over those bytes. TODO: Is Seek() better than Read() for data we don't care about? will Seek() trigger Random-access behaviour - or only Seek() that extends beyond the current buffer (or two?) or goes in a backwards direction?
                    return DbfReadResult.Skipped;
                }
            }
            else if( recordStatus == DbfRecordStatus.Eof )
            {
                if( this.options.HasFlag(DbfDataReaderOptions.IgnoreEof) )
                {
                    // Check the stream length. A "real" EOF should follow.
                    if( this.SetEOF() ) return DbfReadResult.Eof;

                    // Else, NOOP and read as normal, though the data is probably garbage (as in, was-valid-when-written-but-now-probably-meaningless.
                }
                else
                {
                    return DbfReadResult.Eof;
                }
            }
            else if( recordStatus == DbfRecordStatus.Valid )
            {
                // NOOP
            }
            else
            {
                if( this.options.HasFlag(DbfDataReaderOptions.AllowInvalid) )
                {
                    // NOOP
                }
                else
                {
                    this.binaryReader.BaseStream.Seek( this.Table.Header.RecordDataLength, SeekOrigin.Current );
                    return DbfReadResult.Skipped;
                }
            }

            //////////////////////

            if( this.ReadRecord( this.binaryReader, offset, recordStatus ) )
            {
                return DbfReadResult.Read;
            }
            else
            {
                return DbfReadResult.Eof;
            }
        }

        protected virtual Boolean ReadRecord(BinaryReader reader, Int64 offset, DbfRecordStatus recordStatus)
        {
            IList<DbfColumn> cols = this.Table.Columns;

            Object[] values = new Object[ cols.Count ];

            Int32 dataLength = this.Table.Header.RecordDataLength;

            for( Int32 i = 0; i < cols.Count; i++ )
            {
                try
                {
                    Object value = this.ValueReader.ReadValue( cols[i], reader, this.TextEncoding );
                    values[i] = value;
                }
                catch(EndOfStreamException)
                {
                    this.SetEOF();
                    this.Current = null; // TODO: Set `this.Current` to a partial record?
                    return false;
                }
            }

            this.Current = new DbfRecord( this.Table, offset, recordStatus, values );
            return true;
        }

        public override Boolean Seek(Int32 recordIndex)
        {
            Int64 desiredOffset = this.GetRecordFileOffset( recordIndex );
            Int64 currentOffset = this.binaryReader.BaseStream.Seek( desiredOffset, SeekOrigin.Begin );
            return desiredOffset == currentOffset;
        }
    }

    /// <summary>Allows only specified columns to be read as a single record, potentially faster as it won't read/parse unwanted data.</summary>
    public class SubsetSyncDbfDataReader : SyncDbfDataReader
    {
        private readonly DbfTable originalTable;
        private readonly Int32[] columns;

        private static DbfTable CreateVirtualTable(DbfTable table, Int32[] columnIndexes)
        {
            DbfColumn[] selectedColumns = new DbfColumn[ columnIndexes.Length ];
            for( Int32 i = 0; i < columnIndexes.Length; i++ ) selectedColumns[i] = table.Columns[ columnIndexes[i] ];

            return new DbfTable( table.File, table.Header, selectedColumns, table.TextEncoding );
        }

        internal SubsetSyncDbfDataReader(DbfTable table, Int32[] columnIndexes, Boolean randomAccess, Encoding textEncoding, DbfDataReaderOptions options)
            : base( CreateVirtualTable( table, columnIndexes ), randomAccess, textEncoding, options )
        {
            this.originalTable = table;
            this.columns       = columnIndexes;
        }

        protected override Boolean ReadRecord(BinaryReader reader, Int64 offset, DbfRecordStatus recordStatus)
        {
            // TODO: Fix this, it doesn't work.

            IList<DbfColumn> realCols    = this.originalTable.Columns;
            IList<DbfColumn> virtualCols = this.Table.Columns;

            Object[] values = new Object[ virtualCols.Count ];

            Int32 dataLength = this.Table.Header.RecordDataLength;

            Int32 vColIdx = 0;
            for( Int32 i = 0; i < realCols.Count; i++ )
            {
                if( vColIdx >= virtualCols.Count ) break;

                if( i < virtualCols[vColIdx].Index )
                {
                    // Skip columns we're not interested in:
                    Int32 realColLength = Utility.GetDbfColumnTypeLength( realCols[i].ColumnType, realCols[i].Length );
                    reader.BaseStream.Seek( realColLength, SeekOrigin.Current );
                }
                else
                {
                    try
                    {
                        Object value = this.ValueReader.ReadValue( realCols[i], reader, this.TextEncoding );
                        values[ vColIdx ] = value;
                        vColIdx++;
                    }
                    catch(EndOfStreamException)
                    {
                        this.SetEOF();
                        this.Current = null; // TODO: Set `this.Current` to a partial record?
                        return false;
                    }
                }
            }

            this.Current = new DbfRecord( this.Table, offset, recordStatus, values );
            return true;
        }
    }
}
