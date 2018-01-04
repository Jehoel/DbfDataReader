using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbf
{
    public sealed class DbfTable
    {
        private const byte Terminator = 0x0d;
        private const int HeaderMetaDataSize = 33;
        private const int ColumnMetaDataSize = 32;

        internal DbfTable(FileInfo file, DbfHeader header, IList<DbfColumn> columns, Encoding textEncoding)
        {
            this.File          = file;
            this.Header        = header;
            this.Columns       = new ReadOnlyCollection<DbfColumn>( columns );
            this.TextEncoding  = textEncoding;

            this.ColumnsByName = columns
                .GroupBy( c => c.Name )
                .Select( grp => new ReadOnlyCollection<DbfColumn>( grp.ToList() ) )
                .ToDictionary( cols => cols.First().Name ); // xBase allows columns to share the same name, apparently.
        }

        public FileInfo File { get; }

        public DbfHeader                             Header  { get; }
        public ReadOnlyCollection<DbfColumn>         Columns { get; }
        public IReadOnlyDictionary<String,ReadOnlyCollection<DbfColumn>> ColumnsByName { get; }

        public Encoding TextEncoding { get; }

        public static DbfTable Open(String fileName) => Open( fileName, Encoding.ASCII );

        public static DbfTable Open(String fileName, Encoding textEncoding)
        {
            if( textEncoding == null ) throw new ArgumentNullException(nameof(textEncoding));

            using( FileStream fs = Utility.OpenFileForReading( fileName, randomAccess: false, async: false ) )
            using( BinaryReader reader = new BinaryReader( fs, Encoding.ASCII ) ) // Use ASCII encoding to read column names, they're always ASCII.
            {
                DbfHeader header = DbfHeader.Read( reader );

                IDbfTableType tableType = DbfTableType.GetDbfTableType( header );
                
                List<DbfColumn> columns = new List<DbfColumn>();
                DbfColumn lastColumn = null;
                Int32 index = 0;
                do
                {
                    lastColumn = DbfColumn.Read( tableType, reader, index );
                    index++;
                    if( lastColumn != null ) columns.Add( lastColumn );
                }
                while( lastColumn != null );

                /////

                return new DbfTable( new FileInfo( fileName ), header, columns, textEncoding );
            }
        }

        public static Task<DbfTable> OpenAsync(String fileName) => OpenAsync( fileName, Encoding.ASCII );

        public static async Task<DbfTable> OpenAsync(String fileName, Encoding textEncoding)
        {
            if( textEncoding == null ) throw new ArgumentNullException(nameof(textEncoding));

            using( FileStream fs = Utility.OpenFileForReading( fileName, randomAccess: false, async: true ) )
            using( AsyncBinaryReader reader = new AsyncBinaryReader( fs, Encoding.ASCII ) ) // Use ASCII encoding to read column names, they're always ASCII.
            {
                DbfHeader header = await DbfHeader.ReadAsync( reader ).ConfigureAwait(false);
                IDbfTableType tableType = DbfTableType.GetDbfTableType( header );

                List<DbfColumn> columns = new List<DbfColumn>();
                DbfColumn lastColumn = null;
                Int32 index = 0;
                do
                {
                    lastColumn = await DbfColumn.ReadAsync( tableType, reader, index ).ConfigureAwait(false);
                    index++;
                    columns.Add( lastColumn );
                }
                while( lastColumn != null );

                /////

                return new DbfTable( new FileInfo( fileName ), header, columns, textEncoding );
            }
        }

        public SyncDbfDataReader OpenDataReader(Boolean randomAccess) => this.OpenDataReader( randomAccess, DbfDataReaderOptions.None );

        public SyncDbfDataReader OpenDataReader(Boolean randomAccess, DbfDataReaderOptions options)
        {
            SyncDbfDataReader reader = new SyncDbfDataReader( this, randomAccess, this.TextEncoding, options );
            try
            {
                reader.Seek( zeroBasedRecordIndex: 0 ); // Move to first record.
                return reader;
            }
            catch
            {
                ((IDisposable)reader).Dispose();
                throw;
            }
        }

        public SubsetSyncDbfDataReader OpenSubsetDataReader(Int32[] selectColumnIndexes, Boolean randomAccess) => this.OpenSubsetDataReader( selectColumnIndexes, randomAccess, DbfDataReaderOptions.None );

        public SubsetSyncDbfDataReader OpenSubsetDataReader(Int32[] selectColumnIndexes, Boolean randomAccess, DbfDataReaderOptions options)
        {
            SubsetSyncDbfDataReader reader = new SubsetSyncDbfDataReader( this, selectColumnIndexes, randomAccess, this.TextEncoding, options );
            try
            {
                reader.Seek( zeroBasedRecordIndex: 0 ); // Move to first record.
                return reader;
            }
            catch
            {
                ((IDisposable)reader).Dispose();
                throw;
            }
        }
        
        public AsyncDbfDataReader OpenDataReaderAsync(Boolean randomAccess) => this.OpenDataReaderAsync( randomAccess, DbfDataReaderOptions.None );

        public AsyncDbfDataReader OpenDataReaderAsync(Boolean randomAccess, DbfDataReaderOptions options)
        {
            AsyncDbfDataReader reader = new AsyncDbfDataReader( this, randomAccess, this.TextEncoding, options );
            try
            {
                reader.Seek( zeroBasedRecordIndex: 0 ); // Move to first record.
                return reader;
            }
            catch
            {
                ((IDisposable)reader).Dispose();
                throw;
            }
        }

        #if MEMO_SUPPORT

        public DbfMemoFile Memo { get; private set; } // replace with an OpenMemoFile, but we don't need memo support for now.

        public string MemoPath()
        {
            var paths = new[]
            {
                System.IO.Path.ChangeExtension(Path, "fpt"),
                System.IO.Path.ChangeExtension(Path, "FPT"),
                System.IO.Path.ChangeExtension(Path, "dbt"),
                System.IO.Path.ChangeExtension(Path, "DBT")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        public DbfMemoFile CreateMemo(string path)
        {
            DbfMemoFile memo;

            if (Header.IsFoxPro)
            {
                memo = new DbfMemoFoxPro(path);
            }
            else
            {
                if (Header.Version == 0x83)
                {
                    memo = new DbfMemoDbase3(path);
                }
                else
                {
                    memo = new DbfMemoDbase4(path);
                }
            }

            return memo;
        }

        #endif
    }
}