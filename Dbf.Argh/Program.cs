using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dbf.Cdx;

namespace Dbf.Argh
{
	public class Program
	{
		public static void Main(string[] args)
		{
			//DumpIndex( @"C:\git\cdx\DBD-XBase\t\rooms.cdx" );

			//DumpIndex( @"C:\git\rss\DbfDataReader\DbfDataReader\DbfDataReader.Tests\TestData\foxprodb\calls.CDX" );

			//DumpIndex( @"C:\git\rss\DbfDataReader\Data\CUSTOMER.CDX" );

			DumpIndex( @"C:\git\rss\DbfDataReader\Data\CUSTOMER-dbfMan-1.cdx" );
		}

		public static void DumpIndex(String fileName)
		{
			CdxFile indexFile = CdxFile.Open( fileName );
			LeafCdxNode rootNode = (LeafCdxNode)indexFile.RootNode;

			///////////////////////////////////////

			var indexes = rootNode
				.IndexKeys
				.Select( ik => new { Key = ik, RootNode = indexFile.ReadCompactIndex( ik.RecordNumber ) } );

			Dictionary<Int64,BaseCdxNode> rootNodeHeaders = indexes.ToDictionary( pair => pair.RootNode.Offset, pair => pair.RootNode );

			{
				List<Object[]> output = new List<Object[]>();
				output.Add( _indexHeaders );
				output.AddRange( indexes.Select( pair => DumpIndex( indexFile, pair.Key, pair.RootNode ) ) );
				ConsoleUtility.PrintArray( output );
			}
			
			BaseCdxNode parentNode = null;

			while( true )
			{
				UInt32 nodeOffset = ConsoleUtility.ReadUInt32( "Open node at offset? Or 0 to quit. Do not specify an Index Header offset.\r\nOffset must be a child of the current node." );
				if( nodeOffset == 0 ) return;

				BaseCdxNode node;

				if( rootNodeHeaders.ContainsKey( nodeOffset ) )
				{
					parentNode = rootNodeHeaders[ nodeOffset ];

					Console.WriteLine("Specified node is a root node. Reading inner compact-index.");
					node = indexFile.ReadNode( parentNode, nodeOffset );
				}
				else
				{
					if( parentNode == null )
					{
						Console.WriteLine("Error. No current parent node.");
						return;
					}

					Console.WriteLine("Specified node is not a root node. Reading node directly.");
					node = indexFile.ReadNode( parentNode, nodeOffset );
				}
				
				parentNode = node;

				{
					List<Object[]> output = new List<Object[]>();
					output.Add( _nodeHeaders );
				
					if( node is InteriorCdxNode intNode )
					{
						output.Add( new Object[] { "Interior", intNode.Attributes, intNode.LeftSibling, intNode.RightSibling, intNode.KeyCount, intNode.KeyValues } );
					}
					else if( node is LeafCdxNode extNode )
					{
						output.Add( new Object[] { "Exterior", extNode.Attributes, extNode.LeftSibling, extNode.RightSibling, extNode.KeyCount, "" } );
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
			}

			Console.ReadLine();
		}

		private static Object[] _indexHeaders = new Object[] { "Index file", "Tag", "Header offset", "Root node offset", "Expression", "Order", "Unique", "Root type", "Key length", "Filter" };

		private static Object[] DumpIndex(CdxFile indexFile, CdxKeyEntry key, BaseCdxNode rootNode)
		{
			CdxFileHeader h = rootNode.IndexHeader;

			String file = indexFile.FileInfo.Name;
			String tag  = key.StringKey;
			Object hoff = h.Offset;
			Object roff = rootNode.Offset;
			String expr = Encoding.ASCII.GetString( h.KeyExpressionPool, 0, h.KeyExpressionPoolLength );
			String ordr = h.Order.ToString();
			String uniq = h.Options.HasFlag( CompactIndexOptions.Unique ) ? "True" : "False";
			String type = rootNode is LeafCdxNode ? "Exterior" : "Interior";
			Object klen = h.KeyLength;
			String filt = h.Options.HasFlag( CompactIndexOptions.HasForClause ) ?
				Encoding.ASCII.GetString( h.KeyExpressionPool, h.KeyExpressionPoolLength, h.ForExpressionPoolLength ) :
				String.Empty;

			return new Object[] { file, tag, hoff, roff, expr, ordr, uniq, type, klen, filt };
		}

		private static readonly Object[] _nodeHeaders = new Object[] { "Type", "Attributes", "Left", "Right", "KeyCount", "KeyValue" };

		private static readonly Object[] _interiorKeyHeaders = new Object[] { "Key ASCII", "Key bytes", "Recno", "nPage" };

		private static Object[] DumpInteriorKeyEntry(InteriorIndexKeyEntry entry)
		{
			String bytesAscii = Encoding.ASCII.GetString( entry.KeyBytes );
			String bytesHex = BitConverter.ToString( entry.KeyBytes ); // returns dash-separated uppercase hex, e.g.: "00-AA-BB-FE"

			return new Object[] { bytesAscii, bytesHex, entry.RecordNumber, entry.NPage };
		}

		private static readonly Object[] _exteriorKeyHeaders = new Object[] { "Key ASCII", "Key bytes", "Number" };

		private static Object[] DumpExteriorKeyEntry(CdxKeyEntry entry)
		{
			String keyAscii = SafeString( entry.StringKey );
			String keyBytes = BitConverter.ToString( entry.KeyBytes );

			return new Object[] { keyAscii, keyBytes, entry.RecordNumber };
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
	}
}
