using System;
using System.IO;
using CsvHelper;
using Shouldly;
using Xunit;

namespace Dbf.Tests
{
    [Collection("dbase_03")]
    public class DbfDataReaderTests : DbaseTests, IDisposable
    {
        public DbfDataReaderTests()
            : base( "dbase_03.dbf" )
        {
            this.dbfDataReader = this.dbfTable.OpenDataReader(randomAccess: false);
        }

        protected override void Dispose(Boolean disposing)
        {
            this.dbfDataReader.Dispose();

            base.Dispose( disposing );
        }

        public readonly DbfDataReader dbfDataReader;

        [Fact]
        public void Should_have_valid_first_row_values()
        {
            this.dbfDataReader.Read().ShouldBeTrue();
            
            this.dbfDataReader.GetString(0).ShouldBe("0507121");
            this.dbfDataReader.GetString(1).ShouldBe("CMP");
            this.dbfDataReader.GetString(2).ShouldBe("circular");
            this.dbfDataReader.GetString(3).ShouldBe("12");
            this.dbfDataReader.GetString(4).ShouldBe(string.Empty);
            this.dbfDataReader.GetString(5).ShouldBe("no");
            this.dbfDataReader.GetString(6).ShouldBe("Good");
            this.dbfDataReader.GetString(7).ShouldBe(string.Empty);
            this.dbfDataReader.GetDateTime(8).ShouldBe(new DateTime(2005,7,12));
            this.dbfDataReader.GetString(9).ShouldBe("10:56:30am");
            this.dbfDataReader.GetDecimal(10).ShouldBe(5.2m);
            this.dbfDataReader.GetDecimal(11).ShouldBe(2.0m);
            this.dbfDataReader.GetString(12).ShouldBe("Postprocessed Code");
            this.dbfDataReader.GetString(13).ShouldBe("GeoXT");
            this.dbfDataReader.GetDateTime(14).ShouldBe(new DateTime(2005,7,12));
            this.dbfDataReader.GetString(15).ShouldBe("10:56:52am");
            this.dbfDataReader.GetString(16).ShouldBe("New");
            this.dbfDataReader.GetString(17).ShouldBe("Driveway");
            this.dbfDataReader.GetString(18).ShouldBe("050712TR2819.cor");
            this.dbfDataReader.GetInt32(19).ShouldBe(2);
            this.dbfDataReader.GetInt32(20).ShouldBe(2);
            this.dbfDataReader.GetString(21).ShouldBe("MS4");
            this.dbfDataReader.GetInt32(22).ShouldBe(1331);
            this.dbfDataReader.GetDecimal(23).ShouldBe(226625.000m);
            this.dbfDataReader.GetDecimal(24).ShouldBe(1131.323m);
            this.dbfDataReader.GetDecimal(25).ShouldBe(3.1m);
            this.dbfDataReader.GetDecimal(26).ShouldBe(1.3m);
            this.dbfDataReader.GetDecimal(27).ShouldBe(0.897088m);
            this.dbfDataReader.GetDecimal(28).ShouldBe(557904.898m);
            this.dbfDataReader.GetDecimal(29).ShouldBe(2212577.192m);
            this.dbfDataReader.GetInt32(30).ShouldBe(401);
        }

        [Fact]
        public void Should_be_able_to_read_all_the_rows()
        {
            var rowCount = 0;
            while (this.dbfDataReader.Read())
            {
                rowCount++;

                var valueCol1 = this.dbfDataReader.GetString(0);
                var valueCol2 = this.dbfDataReader.GetDecimal(10);
            }

            rowCount.ShouldBe(14);
        }

        [Fact]
        public void Shoud_be_able_to_read_subsets_first_and_last()
        {
            Int32[] selectedColumns = new Int32[]
            {
                 0, // Point_ID
                 7, // Comments
                30  // Point_ID
            };

            String csvPath = GetFullPath( "dbase_03.csv" );

            using( StreamReader textReader = File.OpenText(csvPath) )
            using( CsvParser csvParser = new CsvParser(textReader) )
            using( SubsetSyncDbfDataReader rdr = this.dbfTable.OpenSubsetDataReader( selectedColumns, randomAccess: false ) )
            {
                csvParser.Read(); // Skip headers

                Int32 recordIdx = 0;
                while( rdr.Read() )
                {
                    rdr.Current.FieldCount.ShouldBe( selectedColumns.Length );

                    String[] csvRecord = csvParser.Read();
                    rdr.Current.GetValue(0).ToString().ShouldBe( csvRecord[ 0] );
                    rdr.Current.GetValue(1).ToString().ShouldBe( csvRecord[ 7] );
                    rdr.Current.GetValue(2).ToString().ShouldBe( csvRecord[30] );

                    recordIdx++;
                }
            }
        }

        [Fact]
        public void Should_compact_runs_correctly()
        {
            {
                Int32[] runs = new Int32[] { 0, 1, 2, 3, 4, 5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { 0, 1, 2, 3, 4, 5 } );
            }

            {
                Int32[] runs = new Int32[] { -1, 1, 2, 3, 4, 5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { -1, 1, 2, 3, 4, 5 } );
            }

            {
                Int32[] runs = new Int32[] { -1, -2, 2, 3, 4, 5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { -3    , 2, 3, 4, 5 } );
            }

            {
                Int32[] runs = new Int32[] { -1, -2, -3, 3, 4, 5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { -6        , 3, 4, 5 } );
            }

            {
                Int32[] runs = new Int32[] { 0, 1, 2, 3, 4, -5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { 0, 1, 2, 3, 4, -5 } );
            }

            {
                Int32[] runs = new Int32[] { 0, 1, 2, 3, -4, -5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { 0, 1, 2, 3, -9 } );
            }

            {
                Int32[] runs = new Int32[] { 0, -1, 2, -3, 4, -5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { 0, -1, 2, -3, 4, -5 } );
            }

            {
                Int32[] runs = new Int32[] { 0, -1, -2, -3, 4, -5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { 0, -6, 4, -5 } );
            }

            {
                Int32[] runs = new Int32[] { 0, -1, -2, -3, 4, 5 };
                Int32[] comp = SubsetSyncDbfDataReader.CompactRuns( runs );
                comp.ShouldBe( new Int32[] { 0, -6, 4, 5 } );
            }
        }

        [Fact]
        public void Should_compute_subset_runs_correctly()
        {
            DbfColumn[] realColumns = new DbfColumn[]
            {
                new DbfColumn( 0, "ColA", DbfColumnType.Number   ,  7, 0 ),
                new DbfColumn( 1, "ColB", DbfColumnType.Character, 10, 0 ),
                new DbfColumn( 2, "ColC", DbfColumnType.Boolean  ,  1, 0 ),
                new DbfColumn( 3, "ColD", DbfColumnType.DateTime ,  8, 0 ),
                new DbfColumn( 4, "ColE", DbfColumnType.Number   ,  7, 0 ),
                new DbfColumn( 5, "ColF", DbfColumnType.Character, 10, 0 ),
                new DbfColumn( 6, "ColG", DbfColumnType.Boolean  ,  1, 0 ),
                new DbfColumn( 7, "ColH", DbfColumnType.DateTime ,  8, 0 )
            };

            {
                // Case 1: Select all columns
                Int32[] selectedColumns = new Int32[] { 0, 1, 2, 3, 4, 5, 6, 7 };

                Int32[] runs = SubsetSyncDbfDataReader.GetRuns( realColumns, selectedColumns );

                Int32[] expectedRuns = new Int32[] { 0, 1, 2, 3, 4, 5, 6, 7 };

                runs.ShouldBe( expectedRuns );
            }

            {
                // Case 2: Select all but the last column
                Int32[] selectedColumns = new Int32[] { 0, 1, 2, 3, 4, 5, 6 };

                Int32[] runs = SubsetSyncDbfDataReader.GetRuns( realColumns, selectedColumns );

                Int32[] expectedRuns = new Int32[] { 0, 1, 2, 3, 4, 5, 6, -8 };

                runs.ShouldBe( expectedRuns );
            }

            {
                // Case 3: Select all but the last 2 columns
                Int32[] selectedColumns = new Int32[] { 0, 1, 2, 3, 4, 5 };

                Int32[] runs = SubsetSyncDbfDataReader.GetRuns( realColumns, selectedColumns );

                Int32[] expectedRuns = new Int32[] { 0, 1, 2, 3, 4, 5, -9 };

                runs.ShouldBe( expectedRuns );
            }

            ///

            {
                // Case 4: Select all but the first column
                Int32[] selectedColumns = new Int32[] { 1, 2, 3, 4, 5, 6, 7 };

                Int32[] runs = SubsetSyncDbfDataReader.GetRuns( realColumns, selectedColumns );

                Int32[] expectedRuns = new Int32[] { -7, 1, 2, 3, 4, 5, 6, 7 };

                runs.ShouldBe( expectedRuns );
            }

            {
                // Case 5: Select all but the first two columns
                Int32[] selectedColumns = new Int32[] { 2, 3, 4, 5, 6, 7 };

                Int32[] runs = SubsetSyncDbfDataReader.GetRuns( realColumns, selectedColumns );

                Int32[] expectedRuns = new Int32[] { -17, 2, 3, 4, 5, 6, 7 };

                runs.ShouldBe( expectedRuns );
            }

            {
                // Case 6: Select all but the first two and last two columns
                Int32[] selectedColumns = new Int32[] { 2, 3, 4, 5 };

                Int32[] runs = SubsetSyncDbfDataReader.GetRuns( realColumns, selectedColumns );

                Int32[] expectedRuns = new Int32[] { -17, 2, 3, 4, 5, -9 };

                runs.ShouldBe( expectedRuns );
            }
        }
    }
}