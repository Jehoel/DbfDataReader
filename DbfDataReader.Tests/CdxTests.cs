using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Dbf;
using Dbf.Cdx;

using Xunit;

namespace DbfDataReader.NetFx.Tests
{
    public class CdxTests
    {
        [Fact]
        public void Cdx_reader_should_read_tags_correctly()
        {
            using( CdxFile index = CdxFile.Open( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" ) )
            {
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

                Assert.Equal( expectedIndexTags.Select( t => t.Item1 ), rootNode.IndexKeys.Select( key => key.KeyAsString ) );
                Assert.Equal( expectedIndexTags.Select( t => t.Item2 ), rootNode.IndexKeys.Select( key => (Int32)key.DbfRecordNumber ) );
            }
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
        public void Cdx_reader_should_read_all_nodes()
        {
            Cdx_reader_should_real_all_nodes_iter();
        }

        [Fact]
        public void Cdx_reader_should_read_all_nodes_5_times()
        {
            // Do a cold-start run:
            Stopwatch sw = Stopwatch.StartNew();

            Cdx_reader_should_real_all_nodes_iter();

            TimeSpan coldStartTime = sw.Elapsed;
            sw.Restart();
            
            for( Int32 i = 0; i < 5; i++ )
            {
                Cdx_reader_should_real_all_nodes_iter();
            }
            sw.Stop();

            TimeSpan warmStartTime5 = sw.Elapsed;
        }

        private void Cdx_reader_should_real_all_nodes_iter()
        {
            foreach( FileInfo file in _testCdxFiles )
            {
                using( CdxFile cdxFile = CdxFile.Open( file.FullName ) )
                {
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
                .All( keyEntry => keyEntry.KeyAsString.IndexOf('\0') == -1 );

            Assert.True( noNullsInTagNames );

            foreach( CdxFile file in cdxFiles ) file.Dispose();
        }

        [Fact]
        public void Cdx_reader_should_work_3()
        {
            const String prefix = @"C:\git\cdx\DBD-XBase\t";

            var cdxFiles = new DirectoryInfo( prefix )
                .GetFiles("*.cdx")
                .Select( fi => CdxFile.Open( fi.FullName ) )
                .ToList();

            foreach( CdxFile file in cdxFiles ) file.Dispose();
        }

        [Fact]
        public void Cdx_reader_should_return_nothing_for_nonexistant_keys()
        {
            // I found an infinite-loop when this customer is looked-up. It looks like the customer simply doesn't exist.

            Byte[] customerKey = new Byte[] { 0x54, 0x45, 0x4D, 0x50, 0x4F, 0x52, 0x41, 0x52, 0x59 };

            FileInfo customerCdxFI = new FileInfo( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" );
            using( CdxFile customerCdx = CdxFile.Open( customerCdxFI.FullName ) )
            {
                var customerCdxIndexes = customerCdx.ReadTaggedIndexes();
                CdxIndex keyIndex = customerCdxIndexes["KEY"];

                List<LeafCdxKeyEntry> entriesLoaded = new List<LeafCdxKeyEntry>();
                Int32 count = 0;
                IEnumerable<LeafCdxKeyEntry> keyIndexEntries = IndexSearcher.SearchIndex( keyIndex, customerKey );
                foreach( LeafCdxKeyEntry entry in keyIndexEntries )
                {
                    count++;
                    entriesLoaded.Add( entry );
                }

                Assert.Equal( 0, count );
            }
        }

        [Fact]
        public void Cdx_reader_should_be_fast()
        {
            // Find 1000 keys, 5 keys apart.

            Stopwatch sw = Stopwatch.StartNew();

            FileInfo customerCdxFI = new FileInfo( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" );
            using( CdxFile customerCdx = CdxFile.Open( customerCdxFI.FullName ) )
            {
                var customerCdxIndexes = customerCdx.ReadTaggedIndexes();
                CdxIndex keyIndex = customerCdxIndexes["KEY"];

                TimeSpan setupPart1 = sw.Elapsed;
                sw.Restart();

                List<LeafCdxKeyEntry> allCustomerKeys = IndexSearcher.GetAll( keyIndex ).ToList();

                TimeSpan setupPart2 = sw.Elapsed;

                ShuffleList( 123, allCustomerKeys );

                TimeSpan setupPart3 = sw.Elapsed;
                sw.Restart();

                for( Int32 i = 0; i < 500; i++ )
                {
                    Cdx_reader_should_be_fast_iter( keyIndex, allCustomerKeys );
                }
            
                TimeSpan searched1000x500 = sw.Elapsed;
                sw.Restart();

                // Results (on Dell XPS 15, 2017 model, NVMe SSD):
                // 1. Native key comparison, Release build, Strict checks: 12,907.0081 ms
                // 2. .NET key comparison, Release build, Strict checks  : 13,276.4800 ms
            }
        }

        private static TimeSpan Cdx_reader_should_be_fast_iter(CdxIndex index, List<LeafCdxKeyEntry> keys)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // As all these keys are from the original index, they should all exist, so it should return 1000 when using `Single()` or `FirstOrDefault()`.
            // We don't check if the same key returned multiple values, that's outside the scope of this test.

            List<LeafCdxKeyEntry> results = Enumerable
                .Range( 0, 1000 )
                .Select( i => keys[i] )
                .Select( customerKey => IndexSearcher.SearchIndex( index, customerKey.KeyBytes ).Single() )
                //.SelectMany( matches => matches )
                .ToList();

            Assert.Equal( 1000, results.Count );

            sw.Stop();
            return sw.Elapsed;
        }

        private static void ShuffleList<T>(Int32 seed, List<T> list)
        {
            // Fisher-Yates.
            // https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net

            Random rng = new Random( seed );

            Int32 n = list.Count;
            while( n > 1 )
            {
                Int32 k = rng.Next( n-- );
                T temp = list[n];
                list[n] = list[k];
                list[k] = temp;
            }
        }

        // My code in the main project was getting a DbfRecordNumber of 50391 from ORDER.CDX
        // But that was an immediate EOF. I wonder if record-numbers are off-by-1...

        [Fact]
        public static void Cdx_DbfRecordNumber_values_should_be_accurate()
        {
            FileInfo customerCdxFI = new FileInfo( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" );
            DbfTable customerDbf = DbfTable.Open( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.DBF" );
            using( SyncDbfDataReader dbfReader = customerDbf.OpenDataReader( randomAccess: false ) )
            using( CdxFile customerCdx = CdxFile.Open( customerCdxFI.FullName ) )
            {
                var customerCdxIndexes = customerCdx.ReadTaggedIndexes();
                CdxIndex keyIndex = customerCdxIndexes["KEY"];
                
                // Ready every Customer record, then look-up their keys, ensure it's accurate.

                UInt32 testRecordIndex = 0;
                while( dbfReader.Read() )
                {
                    Int32 claimedRecordIndex = dbfReader.Current.RecordIndex;
                    Assert.Equal( testRecordIndex, (UInt32)claimedRecordIndex ); // Assuming that no records are deleted!

                    // KEY Char(9)
                    String customerKey = dbfReader.GetString(0);
                    Byte[] customerKeyBytes = System.Text.Encoding.ASCII.GetBytes( customerKey );

                    LeafCdxKeyEntry indexEntry = IndexSearcher.SearchIndex( keyIndex, customerKeyBytes ).Single();
                    Assert.Equal( testRecordIndex + 1, indexEntry.DbfRecordNumber );

                    testRecordIndex++;
                }
            }
            
        }
    }
}
