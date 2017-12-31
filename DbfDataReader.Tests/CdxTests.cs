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

            foreach( CdxKeyEntry key in rootNode.IndexKeys )
            {
                if( key.StringKey != "KEY" ) continue;
                
                InteriorCdxNode root2Node = (InteriorCdxNode)index.ReadCompactIndex( key.RecordNumber );

                Int32 keyLength = root2Node.IndexHeader.KeyLength;
                Int32 recordLength = keyLength + 4;

                List<IndexEntry> indexKeyEntries = new List<IndexEntry>( root2Node.KeyCount );
                for( Int32 i = 0; i < root2Node.KeyCount; i++ )
                {
                    IndexEntry entry = IndexEntry.Read( root2Node.KeyValues, keyLength, i );
                    indexKeyEntries.Add( entry );
                }
                
                String y = "bar";
            }

            String x = "foo";
        }

        class IndexEntry
        {
            //public Int64 Offset { get; }

            public Byte[] Key { get; }
            public Byte[] Hex { get; }

            public IndexEntry(/*Int64 offset,*/ Byte[] key, Byte[] hex)
            {
                //this.Offset = offset;
                this.Key    = key;
                this.Hex    = hex;
            }

            public static IndexEntry Read(Byte[] keyBuffer, Int32 keyLength, Int32 indexEntryIdx)
            {
                Int32 startIdx = (keyLength + 4) * indexEntryIdx;

                Byte[] key = new Byte[ keyLength ];
                Array.Copy( keyBuffer, startIdx, key, 0, keyLength );

                Byte[] hex = new Byte[4];
                Array.Copy( keyBuffer, startIdx + keyLength, hex, 0, 4 );

                return new IndexEntry( key, hex );
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
                .Cast<ExteriorCdxNode>()
                .SelectMany( node => node.IndexKeys )
                .All( keyEntry => keyEntry.StringKey.IndexOf('\0') == -1 );

            Assert.True( noNullsInTagNames );

            String x = "foo";
        }

        [Fact]
        public void Cdx_reader_should_work_3()
        {
            const String prefix = @"C:\git\rss\DBD-XBase\t";

            var cdxFiles = new DirectoryInfo( prefix )
                .GetFiles("*.cdx")
                .Select( fi => CdxFile.Open( fi.FullName ) )
                .ToList();

            String x = "Foo";
        }
    }
}
