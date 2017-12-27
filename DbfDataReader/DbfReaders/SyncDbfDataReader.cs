using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

        protected override Boolean SetEof()
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
            if( this.SetEof() ) return DbfReadResult.Eof;

            Int64 offset = this.binaryReader.BaseStream.Position;

            DbfRecordStatus recordStatus = (DbfRecordStatus)this.binaryReader.ReadByte();

            DbfReadResult initReadResult = ShouldRead( recordStatus, this.options );
            if( initReadResult == DbfReadResult.Skipped )
            {
                this.binaryReader.BaseStream.Seek( this.Table.Header.RecordDataLength, SeekOrigin.Current ); // skip-over those bytes. TODO: Is Seek() better than Read() for data we don't care about? will Seek() trigger Random-access behaviour - or only Seek() that extends beyond the current buffer (or two?) or goes in a backwards direction?
                return DbfReadResult.Skipped;
            }
            else if( initReadResult == DbfReadResult.Eof )
            {
                return DbfReadResult.Eof;
            }

            //////////////////////

            if( this.ReadRecord( this.binaryReader, offset, recordStatus ) )
            {
                Int64 expectedPosition = offset + this.Table.Header.RecordLength; // offset + (record-status == 1) + (record-data)
                Int64 actualPosition   = this.binaryReader.BaseStream.Position;
                if( actualPosition != expectedPosition )
                {
                    if( actualPosition > expectedPosition ) throw new InvalidOperationException("Read beyond record.");
                    else throw new InvalidOperationException("Read less than record length.");
                }

                return DbfReadResult.Read;
            }
            else
            {
                return DbfReadResult.Eof;
            }
        }

        protected virtual Boolean ReadRecord(BinaryReader reader, Int64 offset, DbfRecordStatus recordStatus)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            IList<DbfColumn> cols = this.Table.Columns;

            Object[] values = new Object[ cols.Count ];

            for( Int32 i = 0; i < cols.Count; i++ )
            {
                try
                {
                    Object value = ValueReader.ReadValue( cols[i], reader, this.TextEncoding );
                    values[i] = value;
                }
                catch(EndOfStreamException)
                {
                    this.SetEof();
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
        private readonly Int32[] selectedColumnIndexen;

        private static DbfTable CreateVirtualTable(DbfTable table, Int32[] columnIndexes)
        {
            DbfColumn[] selectedColumns = new DbfColumn[ columnIndexes.Length ];
            for( Int32 i = 0; i < columnIndexes.Length; i++ ) selectedColumns[i] = table.Columns[ columnIndexes[i] ];

            return new DbfTable( table.File, table.Header, selectedColumns, table.TextEncoding );
        }

        internal SubsetSyncDbfDataReader(DbfTable table, Int32[] columnIndexes, Boolean randomAccess, Encoding textEncoding, DbfDataReaderOptions options)
            : base( CreateVirtualTable( table, columnIndexes ), randomAccess, textEncoding, options )
        {
            this.originalTable         = table;
            this.selectedColumnIndexen = columnIndexes;
        }

        protected override Boolean ReadRecord(BinaryReader reader, Int64 offset, DbfRecordStatus recordStatus)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            IList<DbfColumn> realCols    = this.originalTable.Columns;
            IList<DbfColumn> virtualCols = this.Table.Columns;

            Int32 valuesIdx = 0;
            Object[] values = new Object[ virtualCols.Count ];

            Int32[] runs = GetRuns( realCols, this.selectedColumnIndexen );

            for( Int32 i = 0; i < runs.Length; i++ )
            {
                if( runs[i] < 0 )
                {
                    Int32 skipLength = -runs[i];
                    reader.BaseStream.Seek( skipLength, SeekOrigin.Current ); // Is Seeking cheaper or more expensive than Reading? Does it trigger any kind of random-IO or is it still considered non-random (sequential) IO reads provided the skips aren't too big? what about buffering inside FileStream and Win32 itself?
                }
                else
                {
                    DbfColumn column = realCols[ runs[i] ];

                    try
                    {
                        Object value = ValueReader.ReadValue( column, reader, this.TextEncoding );
                        values[ valuesIdx ] = value;
                        valuesIdx++;
                    }
                    catch(EndOfStreamException)
                    {
                        this.SetEof();
                        this.Current = null; // TODO: Set `this.Current` to a partial record?
                        return false;
                    }
                }
            }
            
            this.Current = new DbfRecord( this.Table, offset, recordStatus, values );
            return true;
        }

        public static Int32[] GetRuns(IList<DbfColumn> allColumns, IEnumerable<Int32> selectedColumnIndexes)
        {
            if( allColumns == null ) throw new ArgumentNullException(nameof(allColumns));
            if( selectedColumnIndexes == null ) throw new ArgumentNullException(nameof(selectedColumnIndexes));

            // Validate: Ensure arguments are in monotonically ascending order:
            if( !allColumns.Select( c => c.Index ).IsMonotonicallyIncreasing() ) throw new ArgumentException("Columns are not in index-order.");
            //if( !selectedColumnIndexen.IsMonotonicallyIncreasing() ) throw new ArgumentException("SelectedColumns are not in index-order.");

            ////////////////////////
            // Determine runs:

            // e.g.
            // Real columns:
            // Index:  0    1   2   3   4   5
            // Length: 4    4   8   16  4   1

            // Selected columns: 0, 1, 4
            
            // Runs (using negative numbers to denote skipped columns, where the values indicate the number of bytes to skip):
            // [ 0, 1, -8, -16, 4, -1 ]
            // Compacted:
            // [ 0, 1, -24, 4, -1 ]

            HashSet<Int32> selectedColumns = new HashSet<Int32>( selectedColumnIndexes );

            Int32[] runs = new Int32[ allColumns.Count ];
            for( Int32 i = 0; i < runs.Length; i++ )
            {
                if( selectedColumns.Contains( i ) )
                {
                    runs[i] = i;
                }
                else
                {
                    runs[i] = -Utility.GetDbfColumnTypeLength( allColumns[i] );
                }
            }

            Int32[] compactedRuns = CompactRuns( runs );
            return compactedRuns;
        }

        public static Int32[] CompactRuns(Int32[] runs)
        {
            if( runs == null ) throw new ArgumentNullException(nameof(runs));

            Int32 compactedRunCount = 0;
            Boolean lastWasNegative = false;

            for( Int32 i = 0; i < runs.Length; i++ )
            {
                if( runs[i] < 0 )
                {
                    if( !lastWasNegative ) compactedRunCount++;
                    lastWasNegative = true;
                }
                else
                {
                    compactedRunCount++;
                    lastWasNegative = false;
                }
            }

            Int32[] compactedRuns = new Int32[ compactedRunCount ];

            Int32 ciIfNegative = 0;
            Int32 ciIfPostive  = 0;

            for( Int32 i = 0; i < runs.Length; i++ )
            {
                if( runs[i] < 0 )
                {
                    compactedRuns[ ciIfNegative ] += runs[i];
                    ciIfPostive = ciIfNegative + 1;
                }
                else
                {
                    compactedRuns[ ciIfPostive ] = runs[i];

                    ciIfPostive++;
                    ciIfNegative = ciIfPostive;
                }
            }

            return compactedRuns;
        }
    }
}
