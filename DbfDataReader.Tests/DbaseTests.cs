using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;

using Shouldly;

namespace Dbf.Tests
{
    public abstract class DbaseTests : IDisposable
    {
        public static String GetFullPath(String testDataFile)
        {
            // current directory is `DbfDataReader.Tests\bin\Debug`
			// test data is         `DbfDataReader.Tests\TestData`

			DirectoryInfo current = new DirectoryInfo( Environment.CurrentDirectory ); // or `AppDomain.CurrentDomain.BaseDirectory`.
			DirectoryInfo testData = new DirectoryInfo( Path.Combine( current.Parent.Parent.FullName, "TestData" ) );
			testData.Exists.ShouldBeTrue();

            String resolved = Path.Combine( testData.FullName, testDataFile );
            return resolved;
        }

        protected DbaseTests(string fixturePath)
        {
            this.FixturePath = GetFullPath( fixturePath );
            this.dbfTable = DbfTable.Open( this.FixturePath, Encoding.GetEncoding(1252) );
        }

        public void Dispose()
        {
            this.Dispose( disposing: true );
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if( disposing )
            {
                this.dbfTable = null;
            }
        }

        public string FixturePath { get; }

        protected DbfTable dbfTable;

        protected void ValidateColumnSchema(string path)
        {
            path = GetFullPath( path );

            using( FileStream stream = new FileStream(path, FileMode.Open) )
            using( StreamReader summaryFile = new StreamReader(stream) )
            {
                String line = summaryFile.ReadLine();
                while (!line.StartsWith("---"))
                {
                    line = summaryFile.ReadLine();
                }

                foreach (DbfColumn dbfColumn in this.dbfTable.Columns)
                {
                    line = summaryFile.ReadLine();
                    ValidateColumn( dbfColumn, line );
                }
            }
        }

        /// <summary>Compares the provided <paramref name="dbfColumn"/> with the column's definition <paramref name="line"/> in the text file.</summary>
        protected void ValidateColumn(DbfColumn dbfColumn, String line)
        {
            String        expectedName         = line.Substring( 0, 16 ).Trim();
            DbfColumnType expectedColumnType   = (DbfColumnType)line.Substring( 17, 1 )[0];
            Byte          expectedLength       = Byte.Parse( line.Substring( 28, 10 ) );
            Byte          expectedDecimalCount = Byte.Parse( line.Substring( 39) );

            dbfColumn.Name        .ShouldBe( expectedName );
            dbfColumn.ColumnType  .ShouldBe( expectedColumnType );
            dbfColumn.Length      .ShouldBe( expectedLength );
            dbfColumn.DecimalCount.ShouldBe( expectedDecimalCount );
        }

        /// <summary>Compares data returned from the specified CSV file with the data returned from the Dbf library.</summary>
        protected void ValidateRowValues(String csvPath, Boolean trimTextFromCsvFile)
        {
            csvPath = GetFullPath( csvPath );

            using( DbfDataReader dbfReader = this.dbfTable.OpenDataReader(randomAccess: false) )
            using( StreamReader textReader = new StreamReader( csvPath, Encoding.GetEncoding( 1252 ) ) ) // dbase_31.dbf has a string with Codepage 1252 characters.
            using( CsvParser csvParser = new CsvParser(textReader) )
            {
                String[] csvColumnNames = csvParser.Read();
                Int32 csvDelta = csvColumnNames.Last() == "deleted" ? -1 : 0;
                
                Int32 row = 1;
                while( dbfReader.Read() ) 
                {
                    String[] csvValues = csvParser.Read();

                    ValidateRow( dbfReader, csvColumnNames, csvValues, csvDelta, trimTextFromCsvFile, row );

                    row++;
                }
            }
        }

        protected void ValidateRowValuesSubset(String csvPath, Boolean trimTextFromCsvFile)
        {
            csvPath = GetFullPath( csvPath );

            using( StreamReader textReader = new StreamReader( csvPath, Encoding.GetEncoding( 1252 ) ) )
            using( CsvParser csvParser = new CsvParser(textReader) )
            {
                String[] csvColumnNames = csvParser.Read();
                Int32 csvDelta = csvColumnNames.Last() == "deleted" ? -1 : 0;
                if( csvColumnNames.Length % 2 == 0 ) csvDelta = 0;

                // Select half the columns as a rough test.
                Int32[] subsetColumns = new Int32[ csvColumnNames.Length / 2 ];
                for( Int32 i = 0; i < subsetColumns.Length; i++ ) subsetColumns[i] = i * 2;

                String[] subsetCsvColumnNames = new String[ subsetColumns.Length ];
                for( Int32 i = 0; i < subsetColumns.Length; i++ ) subsetCsvColumnNames[i] = csvColumnNames[ subsetColumns[i] ];

                using( DbfDataReader dbfReader = this.dbfTable.OpenSubsetDataReader( subsetColumns, randomAccess: false ) )
                {
                    Int32 row = 1;
                    while( dbfReader.Read() ) 
                    {
                        String[] csvValues = csvParser.Read();
                        // Get the subset of csvValues:
                        String[] subsetCsvValues = new String[ subsetColumns.Length ];
                        for( Int32 i = 0; i < subsetColumns.Length; i++ ) subsetCsvValues[i] = csvValues[ subsetColumns[i] ];

                        ValidateRow( dbfReader, subsetCsvColumnNames, subsetCsvValues, csvDelta, trimTextFromCsvFile, row );

                        row++;
                    }
                }
            }
        }

        protected void ValidateRow(DbfDataReader dbfReader, String[] csvColumnNames, String[] csvValues, Int32 csvDelta, Boolean trimTextFromCsvFile, Int32 row)
        {
            dbfReader.FieldCount.ShouldBe( csvColumnNames.Length + csvDelta );

            dbfReader.FieldCount.ShouldBe( csvValues.Length + csvDelta );

            Int32 maxCols = Math.Min( csvValues.Length + csvDelta, dbfReader.FieldCount );
            for( Int32 i = 0; i < maxCols; i++ )
            {
                if( dbfReader.Table.Columns[i].ColumnType == DbfColumnType.NullFlags ) continue;

                String csvValue = csvValues[i];
                if( trimTextFromCsvFile ) csvValue = csvValue.Trim();

                if( dbfReader.Table.Columns[i].ActualColumnType == DbfActualColumnType.DateTimeBinaryJulian )
                {
                    // the CSV file contains raw bytes, not human-readable text versions of the date.
                    // No point reimplementing a parser again, skip verification here.
                            
                }
                else if( dbfReader[i] is DateTime dt )
                {
                    String dbfValueDt = dt.ToString( "yyyyMMdd", CultureInfo.InvariantCulture );
                    dbfValueDt.ShouldBe( csvValue, customMessage: $"DateTime. Row: {row}, Column: {i}" );
                }
                else if( dbfReader[i] is Decimal )
                {
                    Decimal expected = csvValue == "" ? 0 : Decimal.Parse( csvValue, CultureInfo.InvariantCulture );

                    Decimal dbfDecimal = (Decimal)dbfReader[i];
                    dbfDecimal.ShouldBe( expected, customMessage: $"Decimal. Row: {row}, Column: {i}" );
                }
                else if( dbfReader[i] is Boolean )
                {
                    Boolean expected;
                    if( csvValue.ToUpperInvariant() == "F" ) expected = false;
                    else expected = true;

                    Boolean dbfValue = (Boolean)dbfReader[i];
                    dbfValue.ShouldBe( expected, customMessage: $"Boolean. Row: {row}, Column: {i}" );
                }
                else if( dbfReader[i] is Int32 )
                {
                    Int32 expected = csvValue == "" ? 0 : Int32.Parse( csvValue, CultureInfo.InvariantCulture );
                            
                    Int32 dbfValue = (Int32)dbfReader[i];
                    dbfValue.ShouldBe( expected, customMessage: $"Int32. Row: {row}, Column: {i}" );
                }
                else if( dbfReader[i] is MemoBlock )
                {
                    // Skip verification, we don't support memo values yet.
                }
                else
                {
                    String dbfValue = dbfReader[i].ToString();
                    if( trimTextFromCsvFile ) dbfValue = dbfValue.Trim();

                    dbfValue.ShouldBe( csvValue, customMessage: $"String. Row: {row}, Column: {i}" );
                }
            }//for
        }
    }
}