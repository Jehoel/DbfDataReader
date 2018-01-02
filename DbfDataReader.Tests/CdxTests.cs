using System;
using System.IO;
using System.Linq;

using Dbf.Cdx;

using Xunit;

namespace DbfDataReader.NetFx.Tests
{
    public class CdxTests
    {
        [Fact]
        public void Cdx_reader_should_read_tags_correctly()
        {
            CdxFile index = CdxFile.Open( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" );

            LeafCdxNode rootNode = (LeafCdxNode)index.RootNode;

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
            Assert.Equal( expectedIndexTags.Select( t => t.Item2 ), rootNode.IndexKeys.Select( key => (Int32)key.DbfRecordNumber ) );
        }

        private static FileInfo[] _testCdxFiles = new FileInfo[]
        {
            new FileInfo( @"C:\git\cdx\DBD-XBase\t\rooms.cdx" ),
            new FileInfo( @"C:\git\rss\DbfDataReader\Data\CUSTOMER-dbfMan.cdx" ),
            new FileInfo( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" ),
            new FileInfo( @"C:\git\rss\DbfDataReader\Data\ORDER.CDX" ),
            new FileInfo( @"C:\git\rss\DbfDataReader\Data\VEHICLE.CDX" ),
            new FileInfo( @"C:\git\rss\DbfDataReader\DbfDataReader\DbfDataReader.Tests\TestData\foxprodb\calls.CDX" ),
            new FileInfo( @"C:\git\rss\DbfDataReader\DbfDataReader\DbfDataReader.Tests\TestData\foxprodb\contacts.CDX" ),
            new FileInfo( @"C:\git\rss\DbfDataReader\DbfDataReader\DbfDataReader.Tests\TestData\foxprodb\setup.CDX" ),
            new FileInfo( @"C:\git\rss\DbfDataReader\DbfDataReader\DbfDataReader.Tests\TestData\foxprodb\types.CDX" )
        };

        [Fact]
        public void Cdx_reader_should_work()
        {
            foreach( FileInfo file in _testCdxFiles )
            {
                CdxFile cdxFile = CdxFile.Open( file.FullName );

                LeafCdxNode cdxFileRootNode = (LeafCdxNode)cdxFile.RootNode; // TODO NOTE: We assume all tag indexes are Leaf Nodes for now. In future I need to expose tag-names and their offsets better.

                Int32 keyCount = 0;
                Int32 nodeCount = 0;

                foreach( LeafCdxKeyEntry tagNameKey in cdxFileRootNode.IndexKeys )
                {
                    CdxIndex taggedIndex = cdxFile.ReadIndex( tagNameKey.DbfRecordNumber ); // TODO: Don't use `DbfRecordNumber` to describe a tagged-index offset.

                    // Read all nodes:
                    BaseCdxNode taggedIndexRootNode = taggedIndex.RootNode;
                    if( taggedIndexRootNode is InteriorCdxNode rootNodeIsInteriorNode )
                    {
                        ReadInteriorNode( taggedIndex, rootNodeIsInteriorNode, ref keyCount, ref nodeCount );
                    }
                }
            }
        }

        private static void ReadInteriorNode(CdxIndex index, InteriorCdxNode interiorNode, ref Int32 keyCount, ref Int32 nodeCount)
        {
            keyCount += interiorNode.KeyCount;

            foreach( InteriorIndexKeyEntry key in interiorNode.KeyEntries )
            {
                BaseCdxNode pointeeNode = index.ReadNode( key.NodePointer );
                nodeCount++;

                if( pointeeNode is InteriorCdxNode pointeeNodeIsInteriorNode )
                {
                    ReadInteriorNode( index, pointeeNodeIsInteriorNode, ref keyCount, ref nodeCount );
                }
            }
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
                .Cast<LeafCdxNode>()
                .SelectMany( node => node.IndexKeys )
                .All( keyEntry => keyEntry.StringKey.IndexOf('\0') == -1 );

            Assert.True( noNullsInTagNames );
        }

        [Fact]
        public void Cdx_reader_should_work_3()
        {
            const String prefix = @"C:\git\cdx\DBD-XBase\t";

            var cdxFiles = new DirectoryInfo( prefix )
                .GetFiles("*.cdx")
                .Select( fi => CdxFile.Open( fi.FullName ) )
                .ToList();
        }
    }
}
