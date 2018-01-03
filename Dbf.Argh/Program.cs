using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Dbf.Cdx;

namespace Dbf.Argh
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			//DumpIndexFile( @"C:\git\cdx\DBD-XBase\t\rooms.cdx" );

			//DumpIndexFile( @"C:\git\rss\DbfDataReader\DbfDataReader\DbfDataReader.Tests\TestData\foxprodb\calls.CDX" );

			DumpIndexFile( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" );

			//DumpIndexFile( @"C:\git\rss\DbfDataReader\Data\CUSTOMER-dbfMan-1.cdx" );

			//DumpIndexFile( @"C:\git\rss\DbfDataReader\Data\ORDER.CDX" );

			Console.ReadLine();
		}

		public class CdxIndexWithTag
		{
			public CdxIndex CdxIndex { get; }
			public String   TagName { get; }

			public CdxIndexWithTag(CdxIndex index, String tag)
			{
				this.CdxIndex = index;
				this.TagName = tag;
			}
		}

		public static void DumpIndexFile(String fileName)
		{
			CdxIndex index = OpenCdxFileAndPromptUserForCdxIndexTag( fileName );

			String option = ConsoleUtility.ReadAny( "[B]rowse, [C]ount or [S]earch index?", "B", "C", "S" ).ToUpperInvariant();
			if( option == "B" )
			{
				BrowseCdxIndex( index );
			}
			else if( option == "S" )
			{
				SearchCdxIndex( index );
			}
			else if( option == "C" )
			{
				CountCdxIndex( index );
			}
		}

		private static CdxIndex OpenCdxFileAndPromptUserForCdxIndexTag(String fileName)
		{
			CdxFile indexFile = CdxFile.Open( fileName );

			IDictionary<String,CdxIndex> taggedIndexes = indexFile.ReadTaggedIndexes();
			
			{
				List<Object[]> output = new List<Object[]>();
				output.Add( _indexHeaders );
				output.AddRange( taggedIndexes.Select( kvp => DumpIndexObject( indexFile, kvp.Key, kvp.Value.RootNode ) ) );
				ConsoleUtility.PrintArray( output );
			}

			// Special-case: select a tag.
			CdxIndex currentIndex = null;
			do
			{
				String tagName = ConsoleUtility.ReadLine( "Specify tag name of index to open." );
				if( taggedIndexes.TryGetValue( tagName, out CdxIndex index ) )
				{
					Console.WriteLine("Selected index \"{0}\".", tagName );
					currentIndex = index;
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine( "Index with tag \"{0}\" not found. Please specify an extant tag name.", tagName );
					Console.ResetColor();
				}
			}
			while( currentIndex == null );

			return currentIndex;
		}

		#region Browse CDX file

		private static void BrowseCdxIndex(CdxIndex index)
		{
			BaseCdxNode node = index.RootNode;

			while( true )
			{
				{
					List<Object[]> output = new List<Object[]>();
					output.Add( _nodeHeaders );
				
					if( node is InteriorCdxNode intNode )
					{
						output.Add( new Object[] { "Interior", intNode.Attributes, intNode.LeftSibling, intNode.RightSibling, intNode.KeyCount } );
					}
					else if( node is LeafCdxNode extNode )
					{
						output.Add( new Object[] { "Exterior", extNode.Attributes, extNode.LeftSibling, extNode.RightSibling, extNode.KeyCount } );
					}

					ConsoleUtility.PrintArray( output );
				}

				{
					if( node is InteriorCdxNode intNode )
					{
						Console.WriteLine("Interior node keys:");
						Console.WriteLine();

						List<Object[]> output = new List<Object[]>();
						output.Add( _interiorKeyHeaders );
						output.AddRange( intNode.KeyEntries.Select( ke => DumpInteriorKeyEntry( ke ) ) );
						ConsoleUtility.PrintArray( output );
					}
					else if( node is LeafCdxNode extNode )
					{
						Console.WriteLine("Exterior node keys:");
						Console.WriteLine();

						List<Object[]> output = new List<Object[]>();
						output.Add( _exteriorKeyHeaders );
						output.AddRange( extNode.IndexKeys.Select( ke => DumpExteriorKeyEntry( ke ) ) );
						ConsoleUtility.PrintArray( output );
					}
				}

				Int32 nodeOffset = ConsoleUtility.ReadUInt32( "Open node at offset? Or 0 to quit. Do not specify an Index Header offset." );
				if( nodeOffset == 0 ) return;

				node = index.ReadNode( nodeOffset );
			}
		}

		private static Object[] _indexHeaders = new Object[] { "Index file", "Tag", "Header offset", "Root node offset", "Expression", "Order", "Unique", "Root type", "Key length", "Filter" };

		private static Object[] DumpIndexObject(CdxFile indexFile, String tagName, BaseCdxNode rootNode)
		{
			CdxIndexHeader h = rootNode.IndexHeader;

			String file = indexFile.FileInfo.Name;
			String tag  = tagName;
			Object hoff = h.Offset;
			Object roff = rootNode.Offset;
			String expr = h.ForExpressionAsString;
			String ordr = h.Order.ToString();
			String uniq = h.Options.HasFlag( CdxIndexOptions.Unique ) ? "True" : "False";
			String type = rootNode is LeafCdxNode ? "Exterior" : "Interior";
			Object klen = h.KeyLength;
			String filt = h.ForExpressionAsString;

			return new Object[] { file, tag, hoff, roff, expr, ordr, uniq, type, klen, filt };
		}

		private static readonly Object[] _nodeHeaders = new Object[] { "Type", "Attributes", "Left", "Right", "KeyCount" };

		private static readonly Object[] _interiorKeyHeaders = new Object[] { "Key ASCII", "Key bytes", "Recno", "Node pointer" };

		private static Object[] DumpInteriorKeyEntry(InteriorIndexKeyEntry entry)
		{
			String bytesAscii = Encoding.ASCII.GetString( entry.KeyBytes );
			String bytesHex = BitConverter.ToString( entry.KeyBytes ); // returns dash-separated uppercase hex, e.g.: "00-AA-BB-FE"

			return new Object[] { bytesAscii, bytesHex, entry.DbfRecordNumber, entry.NodePointer };
		}

		private static readonly Object[] _exteriorKeyHeaders = new Object[] { "Key ASCII", "Key bytes", "Number" };

		private static Object[] DumpExteriorKeyEntry(LeafCdxKeyEntry entry)
		{
			String keyAscii = SafeString( entry.KeyAsString );
			String keyBytes = BitConverter.ToString( entry.KeyBytes );

			return new Object[] { keyAscii, keyBytes, entry.DbfRecordNumber };
		}

		private static String SafeString(String value)
		{
			if( value.All( c => !Char.IsControl( c ) ) ) return value;

			StringBuilder sb = new StringBuilder( value.Length );
			foreach( Char c in value )
			{
				if( Char.IsControl( c ) ) sb.Append( '_' );
				else sb.Append( c );
			}

			String safe = sb.ToString();
			return safe;
		}

		#endregion

		#region Search CDX index

		private static void SearchCdxIndex(CdxIndex index)
		{
			while( true )
			{
				String keyHex = ConsoleUtility.ReadLine( "Target key bytes, in dash-separated hexadecimal, e.g. DE-AD-BE-EF-12-34." );
				Byte[] keyBytes = keyHex
					.Split('-')
					.Select( s => Convert.ToByte( s, 16 ) )
					.ToArray();

				Stopwatch sw = Stopwatch.StartNew();;

				List<UInt32> dbfRecordNumbers = IndexSearcher.SearchIndex( index, keyBytes ).Select( keyEntry => keyEntry.DbfRecordNumber ).ToList();

				sw.Stop();

				foreach( UInt32 dbfRecordNumber in dbfRecordNumbers )
				{
					Console.WriteLine( dbfRecordNumber );
				}

				Console.WriteLine( "Took {0}ms.", sw.ElapsedMilliseconds );
			}
		}

		#endregion

		#region Count CDX index

		private static void CountCdxIndex(CdxIndex index)
		{
			Stopwatch sw = Stopwatch.StartNew();

			Int32 count = IndexSearcher.CountAll( index );

			sw.Stop();

			Console.WriteLine( "Index has {0} keys.", count );
			Console.WriteLine( "Took {0}ms.", sw.ElapsedMilliseconds );
		}

		#endregion
	}
}
