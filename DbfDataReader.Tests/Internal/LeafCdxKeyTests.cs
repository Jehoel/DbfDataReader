using System;
using System.Diagnostics;
using Dbf.Cdx;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Dbf.Tests
{
    [Collection( "LeafCdxKey" )]
    public class LeafCdxKeyTests
    {
        private readonly ITestOutputHelper output;

        public LeafCdxKeyTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Should_read_packed_entries_correctly()
        {
            Stopwatch sw = Stopwatch.StartNew();

            Byte[] buffer = new Byte[488];
            for( Int32 i = 0; i < buffer.Length; i++ )
            {
                buffer[i] = (Byte)( i % 256 );
            }

            {
                Int64 expected = 0x00_00_00_00_00_00_00_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, startIndex: 0, recordLength: 0 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_00_00_00_00_00_00_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 0, 1 );
                expected.ShouldBe( actual );
            }

            {
                //                                    1  0
                Int64 expected = 0x00_00_00_00_00_00_01_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 0, 2 );
                expected.ShouldBe( actual );
            }

            {
                //                                 2  1  0
                Int64 expected = 0x00_00_00_00_00_02_01_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 0, 3 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_00_00_00_03_02_01_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 0, 4 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_00_00_04_03_02_01_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 0, 5 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_00_05_04_03_02_01_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 0, 6 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_06_05_04_03_02_01_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 0, 7 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x07_06_05_04_03_02_01_00;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 0, 8 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x08_07_06_05_04_03_02_01;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 1, 8 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x09_08_07_06_05_04_03_02;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 2, 8 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x0A_09_08_07_06_05_04_03;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 3, 8 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_09_08_07_06_05_04_03;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 3, 7 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_00_08_07_06_05_04_03;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 3, 6 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_00_00_07_06_05_04_03;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 3, 5 );
                expected.ShouldBe( actual );
            }

            {
                Int64 expected = 0x00_00_00_00_06_05_04_03;
                Int64 actual   = LeafCdxKeyUtility.GetPackedEntryAsInt64( buffer, 3, 4 );
                expected.ShouldBe( actual );
            }

            sw.Stop();
            //Debug.WriteLine( sw.Elapsed );
            this.output.WriteLine( "Test time: " + sw.ElapsedMilliseconds + "ms." );
        }

        /*
        private delegate Int64 GetPackedEntryAsLongDelegate(Byte[] buffer, Int32 startIndex, Int32 recordLength);

        private static readonly GetPackedEntryAsLongDelegate _getPackedEntryAsLong = GetDelegate();

        private static GetPackedEntryAsLongDelegate GetDelegate()
        {
            // https://msdn.microsoft.com/en-us/library/53cz7sc6(v=vs.110).aspx
        }*/
    }
}
