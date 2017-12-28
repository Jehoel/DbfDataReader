using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Dbf.Cdx;

using Xunit;

namespace DbfDataReader.NetFx.Tests
{
    public class CdxTests
    {
        [Fact]
        public void Cdx_reader_should_work()
        {
            CdxFile index = CdxFile.Open( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" );
            ExteriorCdxNode rootNode = (ExteriorCdxNode)index.RootNode;

            Tuple<String,Int32>[] expectedIndexTags = new Tuple<String,Int32>[]
            {
                Tuple.Create("APPBAL", 3956224 ), 
                Tuple.Create("APPCODE", 3952128 ), 
                Tuple.Create("APPNAME", 3929600 ), 
                Tuple.Create("APPROVED", 1224192 ), 
                Tuple.Create("BALANCE", 1250304 ), 
                Tuple.Create("CODE", 76288 ), 
                Tuple.Create("EMAIL", 3596800 ), 
                Tuple.Create("F_PHONE", 962560 ), 
                Tuple.Create("GLPOST", 2051584 ), 
                Tuple.Create("H_PHONE", 743936 ), 
                Tuple.Create("KEY", 1536 ), 
                Tuple.Create("M_PHONE", 1042944 ), 
                Tuple.Create("NAME", 134144 ), 
                Tuple.Create("NAMEBAL", 1246720 ), 
                Tuple.Create("NAMEIA", 2851328 ), 
                Tuple.Create("SM_KEY", 1251840 ), 
                Tuple.Create("SRCDATE", 2865664 ), 
                Tuple.Create("STORENAME", 2960896 ), 
                Tuple.Create("W_PHONE", 864256 )
            };

            Assert.Equal( expectedIndexTags.Select( t => t.Item1 ), rootNode.IndexKeys.Select( key => key.StringKey ) );
            Assert.Equal( expectedIndexTags.Select( t => t.Item2 ), rootNode.IndexKeys.Select( key => (Int32)key.RecordNumber ) );

            String x = "foo";
        }

        [Fact]
        public void Cdx_reader_should_work_2()
        {
            const String prefix = @"C:\git\rss\DbfDataReader\DbfDataReader\DbfDataReader.Tests\TestData\foxprodb\";

            var cdxFiles = new DirectoryInfo( prefix )
                .GetFiles("*.cdx")
                .Select( fi => CdxFile.Open( fi.FullName ) )
                .ToList();

            Boolean noNullsInTagNames = cdxFiles
                .Select( cdx => cdx.RootNode )
                .Cast<ExteriorCdxNode>()
                .SelectMany( node => node.IndexKeys )
                .All( keyEntry => keyEntry.StringKey.IndexOf('\0') == -1 );

            Assert.True( noNullsInTagNames );

            String x = "foo";
        }
    }
}
