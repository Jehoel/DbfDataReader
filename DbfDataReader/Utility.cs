using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;

namespace Dbf
{
    internal static class Utility
    {
        public static FileStream OpenFileForReading(String fileName, Boolean randomAccess, Boolean async)
        {
            FileOptions options = ( randomAccess ? FileOptions.RandomAccess : FileOptions.SequentialScan ) | ( async ? FileOptions.Asynchronous : FileOptions.None );

            return new FileStream( fileName, FileMode.Open, FileSystemRights.ReadData, FileShare.ReadWrite, 4096, options );
        }
    }

    internal static class BuildOptions
    {
        public const Boolean StrictChecks = true;
    }

    public class SequentialByteArrayComparer : IComparer<Byte[]>
    {
        // I wonder if an unsafe-cast to UInt64* and comparing that way would be faster...

        Int32 IComparer<Byte[]>.Compare(Byte[] x, Byte[] y)
        {
            //return Compare( x, y );
            return CompareWithoutChecks( x, y );
        }

        public static Int32 Compare(Byte[] x, Byte[] y)
        {
            if( x == null ) throw new ArgumentNullException(nameof(x));
            if( y == null ) throw new ArgumentNullException(nameof(y));
            if( x.Length != y.Length ) throw new ArgumentException("Argument arrays have different lengths.");

            for( Int32 i = 0; i < x.Length; i++ )
            {
                Int32 cmp = x[i].CompareTo( y[i] );
                if( cmp != 0 ) return cmp;
            }

            return 0;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0" )]
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1" )]
        public static Int32 CompareWithoutChecks(Byte[] x, Byte[] y)
        {
            for( Int32 i = 0; i < x.Length; i++ )
            {
                Int32 cmp = x[i].CompareTo( y[i] );
                if( cmp != 0 ) return cmp;
            }

            return 0;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0" )]
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1" )]
        public static Int32 CompareWithoutChecks(Byte[] x, Byte[] y, Int32 xStartIndex, Int32 yStartIndex, Int32 length)
        {
            for( Int32 i = 0; i < length; i++ )
            {
                Byte xB = x[ xStartIndex + i ];
                Byte yB = y[ yStartIndex + i ];

                Int32 cmp = xB.CompareTo( yB );
                if( cmp != 0 ) return cmp;
            }

            return 0;
        }

        public static SequentialByteArrayComparer Instance { get; } = new SequentialByteArrayComparer();
    }
}
