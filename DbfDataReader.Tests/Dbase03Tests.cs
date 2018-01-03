﻿using Shouldly;
using Xunit;

namespace Dbf.Tests
{
    [Collection("dbase_03")]
    public class Dbase03Tests : DbaseTests
    {
        private const string Dbase03FixturePath = "dbase_03.dbf";

        public Dbase03Tests() : base(Dbase03FixturePath)
        {
        }

        [Fact]
        public void Should_report_correct_record_count()
        {
            this.dbfTable.Header.RecordCount.ShouldBe(14);
        }

        [Fact]
        public void Should_report_correct_version_number()
        {
            this.dbfTable.Header.Version.ShouldBe( (byte)0x03 );
        }

        [Fact]
        public void Should_report_that_the_file_is_not_foxpro()
        {
            this.dbfTable.Header.IsFoxPro.ShouldBeFalse();
        }

        [Fact]
        public void Should_have_the_correct_number_of_columns()
        {
            this.dbfTable.Columns.Count.ShouldBe(31);
        }

        [Fact]
        public void Should_have_the_correct_column_schema()
        {
            ValidateColumnSchema("dbase_03_summary.txt");
        }

        [Fact]
        public void Should_have_correct_row_values()
        {
            ValidateRowValues( "dbase_03.csv", trimTextFromCsvFile: false );
        }

        [Fact]
        public void Should_have_correct_row_values_for_subsets()
        {
            ValidateRowValuesSubset( "dbase_03.csv", trimTextFromCsvFile: true );
        }
    }
}
