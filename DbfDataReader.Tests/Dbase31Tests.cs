using Shouldly;
using Xunit;

namespace DbfDataReader.Tests
{
    [Collection("dbase_31")]
    public class Dbase31Tests : DbaseTests
    {
        private const string Dbase31FixturePath = "dbase_31.dbf";

        public Dbase31Tests()
            : base(Dbase31FixturePath)
        {
        }

        [Fact]
        public void Should_report_correct_record_count()
        {
            this.dbfTable.Header.RecordCount.ShouldBe(77);
        }

        [Fact]
        public void Should_report_correct_version_number()
        {
            this.dbfTable.Header.Version.ShouldBe( (byte)0x31 );
        }

        [Fact]
        public void Should_report_that_the_file_is_not_foxpro()
        {
            this.dbfTable.Header.IsFoxPro.ShouldBeTrue();
        }

        [Fact]
        public void Should_have_the_correct_number_of_columns()
        {
            this.dbfTable.Columns.Count.ShouldBe(11);
        }

        [Fact]
        public void Should_have_the_correct_column_schema()
        {
            ValidateColumnSchema("dbase_31_summary.txt");
        }

        [Fact]
        public void Should_have_correct_row_values()
        {
            ValidateRowValues("dbase_31.csv");
        }
    }
}