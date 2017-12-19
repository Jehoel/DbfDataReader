using System;
using Shouldly;
using Xunit;

namespace DbfDataReader.Tests
{
    [Collection("dbase_03")]
    public class DbfDataReaderTests : IDisposable
    {
        private readonly String testFileName;

        public DbfDataReaderTests()
        {
            this.testFileName = DbaseTests.GetFullPath( "dbase_03.dbf" );
            this.DbfTable = DbfTable.Open( this.testFileName );
            this.DbfDataReader = this.DbfTable.OpenDataReader(randomAccess: false);
        }

        public void Dispose()
        {
            this.DbfDataReader.Dispose();
            this.DbfDataReader = null;
        }

        public DbfTable DbfTable { get; set; }
        public DbfDataReader DbfDataReader { get; set; }

        [Fact]
        public void Should_have_valid_first_row_values()
        {
            this.DbfDataReader.Read().ShouldBeTrue();
            
            this.DbfDataReader.GetString(0).ShouldBe("0507121");
            this.DbfDataReader.GetString(1).ShouldBe("CMP");
            this.DbfDataReader.GetString(2).ShouldBe("circular");
            this.DbfDataReader.GetString(3).ShouldBe("12");
            this.DbfDataReader.GetString(4).ShouldBe(string.Empty);
            this.DbfDataReader.GetString(5).ShouldBe("no");
            this.DbfDataReader.GetString(6).ShouldBe("Good");
            this.DbfDataReader.GetString(7).ShouldBe(string.Empty);
            this.DbfDataReader.GetDateTime(8).ShouldBe(new DateTime(2005,7,12));
            this.DbfDataReader.GetString(9).ShouldBe("10:56:30am");
            this.DbfDataReader.GetDecimal(10).ShouldBe(5.2m);
            this.DbfDataReader.GetDecimal(11).ShouldBe(2.0m);
            this.DbfDataReader.GetString(12).ShouldBe("Postprocessed Code");
            this.DbfDataReader.GetString(13).ShouldBe("GeoXT");
            this.DbfDataReader.GetDateTime(14).ShouldBe(new DateTime(2005,7,12));
            this.DbfDataReader.GetString(15).ShouldBe("10:56:52am");
            this.DbfDataReader.GetString(16).ShouldBe("New");
            this.DbfDataReader.GetString(17).ShouldBe("Driveway");
            this.DbfDataReader.GetString(18).ShouldBe("050712TR2819.cor");
            this.DbfDataReader.GetInt32(19).ShouldBe(2);
            this.DbfDataReader.GetInt32(20).ShouldBe(2);
            this.DbfDataReader.GetString(21).ShouldBe("MS4");
            this.DbfDataReader.GetInt32(22).ShouldBe(1331);
            this.DbfDataReader.GetDecimal(23).ShouldBe(226625.000m);
            this.DbfDataReader.GetDecimal(24).ShouldBe(1131.323m);
            this.DbfDataReader.GetDecimal(25).ShouldBe(3.1m);
            this.DbfDataReader.GetDecimal(26).ShouldBe(1.3m);
            this.DbfDataReader.GetDecimal(27).ShouldBe(0.897088m);
            this.DbfDataReader.GetDecimal(28).ShouldBe(557904.898m);
            this.DbfDataReader.GetDecimal(29).ShouldBe(2212577.192m);
            this.DbfDataReader.GetInt32(30).ShouldBe(401);
        }

        [Fact]
        public void Should_be_able_to_read_all_the_rows()
        {
            var rowCount = 0;
            while (this.DbfDataReader.Read())
            {
                rowCount++;

                var valueCol1 = this.DbfDataReader.GetString(0);
                var valueCol2 = this.DbfDataReader.GetDecimal(10);
            }

            rowCount.ShouldBe(14);
        }
    }
}