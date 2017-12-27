﻿using System;
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
            var list = rootNode.GetIndexEntries( index ).ToList();

            for( Int32 i = 0; i < list.Count; i++ )
            {
                var keyInfo = list[i];

                String prefix = String.Empty;
                if( keyInfo.DuplicateBytes > 0 )
                {
                    Assert.True( i > 0 );

                    // Copy the first (duplicate) characters from the previous key as a prefix.
                    prefix = list[i-1].KeyValue.Substring( 0, keyInfo.DuplicateBytes );
                }

                String keyValue = Encoding.ASCII.GetString( rootNode.IndexKeys, keyInfo.KeyValueIndex0, keyInfo.KeyValueLength );
                keyValue = prefix + keyValue;

                keyInfo.KeyValue = keyValue;
            }



            String x = "foo";
        }
    }
}
