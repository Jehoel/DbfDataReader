using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbf.Argh
{
	public static class ConsoleUtility
	{
		public static void PrintArray(IList<Object[]> table)
		{
			Int32[] colWidths = GetColumnWidths( table );
			String lineFormat = GetLineFormat( colWidths );

			///////////////////////////////////

			Boolean first = true;
			foreach( Object[] row in table )
			{
				for( Int32 c = 0; c < row.Length; c++ )
				{
					if( row[c] is IConvertible convVal && !(row[c] is String) )
					{
						row[c] = convVal.ToString( CultureInfo.CurrentCulture ).PadLeft( colWidths[c] );
					}
				}

				Console.WriteLine( lineFormat, arg: row );

				if( first )
				{
					Int32 totalWidth = ( ( colWidths.Length - 1 ) * 3 ) + colWidths.Sum();
					Console.WriteLine( "".PadRight( totalWidth, '-' ) );
					first = false;
				}
			}

			Console.WriteLine();
		}

		private static Int32[] GetColumnWidths(IList<Object[]> table)
		{
			Int32[] colWidths = new Int32[ table.First().Length ];
			foreach( Object[] row in table )
			{
				for( Int32 c = 0; c < row.Length; c++ )
				{
					String stringValue = row[c]?.ToString() ?? String.Empty;

					Int32 l = stringValue.Length;
					if( l > colWidths[c] ) colWidths[c] = l;
				}
			}
			return colWidths;
		}

		private static String GetLineFormat(Int32[] colWidths)
		{
			StringBuilder sb = new StringBuilder( 1024 );
			for( Int32 i = 0; i < colWidths.Length; i++ )
			{
				if( i > 0 ) sb.Append(" | ");

				sb.Append("{");
				sb.AppendFormat("{0},{1}", i, -colWidths[i] );
				sb.Append("}");
			}

			String lineFormat = sb.ToString();
			return lineFormat;
		}

		public static Int32 ReadUInt32(String prompt)
		{
			do
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine( prompt );
				Console.ResetColor();

				Console.ForegroundColor = ConsoleColor.Cyan;
				String input = Console.ReadLine();
				Console.ResetColor();

				Int32 output;
				if( Int32.TryParse( input, NumberStyles.Integer, CultureInfo.CurrentCulture, out output ) ) return output;

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine( "Could not parse \"{0}\" as UInt32.", input );
				Console.ResetColor();

			} while( true );
		}

		public static String ReadLine(String prompt)
		{
			do
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine( prompt );
				Console.ResetColor();

				Console.ForegroundColor = ConsoleColor.Cyan;
				String input = Console.ReadLine();
				Console.ResetColor();

				if( !String.IsNullOrWhiteSpace( input ) ) return input;

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Empty input.");
				Console.ResetColor();

			} while( true );
		}
	}
}
