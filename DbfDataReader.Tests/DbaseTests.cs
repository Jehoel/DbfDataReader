using CsvHelper;
using Shouldly;
using System;
using System.IO;

namespace DbfDataReader.Tests
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
            this.dbfTable = DbfTable.Open( this.FixturePath );
        }

        public void Dispose()
        {
            this.dbfTable = null;
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
        protected void ValidateRowValues(String csvPath)
        {
            csvPath = GetFullPath( csvPath );

            using( DbfDataReader dbfReader = this.dbfTable.OpenDataReader(randomAccess: false) )
            using( StreamReader textReader = File.OpenText(csvPath) )
            using( CsvParser csvParser = new CsvParser(textReader) )
            {
                String[] columnNames = csvParser.Read();
                
                Int32 row = 1;
                while( dbfReader.Read() ) 
                {
                    columnNames.Length.ShouldBe( dbfReader.FieldCount );

                    String[] csvValues = csvParser.Read();

                    dbfReader.FieldCount.ShouldBe( csvValues.Length );

                    for( Int32 i = 0; i < dbfReader.FieldCount; i++ )
                    {
                        String csvValue = csvValues[i];
                        String dbfValue = dbfReader[i].ToString();

                        dbfValue.ShouldBe( csvValue, customMessage: $"Row: {row}, Column: {i}" );
                    }

                    row++;
                }
            }
        }
    }
}