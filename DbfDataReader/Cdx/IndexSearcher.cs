using System;
using System.Collections.Generic;

namespace Dbf.Cdx
{
    public static class IndexSearcher
    {
        /// <summary>Returns all matching DBF Record Number values that match the specified key. The key length must match the index.</summary>
        public static IEnumerable<UInt32> SearchIndex(CdxIndex index, Byte[] key)
        {
            if( key == null ) throw new ArgumentNullException(nameof(key));
            if( index.Header.KeyLength != key.Length ) throw new ArgumentException("The specified key value has a different length than the index key.", nameof(key));

            BaseCdxNode node = index.RootNode;

            throw new NotImplementedException();

            while( node != null )
            {
                IList<IKey> keys = node.GetKeys();

                
            }
        }

        // There is no built-in BinarySearch for IList<T>, surprisingly.
        public static Int32 BinarySearch<T>(IList<T> list, Func<T,Int32> comparison)
        {
            Int32 lower = 0;
            Int32 upper = list.Count - 1;

            while( lower <= upper )
            {
                Int32 middle = lower + (upper - lower) / 2;
                Int32 comparisonResult = comparison( list[middle] );
                if( comparisonResult == 0 )
                {
                    return middle;
                }
                else if( comparisonResult < 0 )
                {
                    upper = middle - 1;
                }
                else
                {
                    lower = middle + 1;
                }
            }

            return ~lower;
        }
    }

    public class SequentialByteArrayComparer : IComparer<Byte[]>
    {
        // I wonder if an unsafe-cast to UInt64* and comparing that way would be faster...

        public Int32 Compare(Byte[] x, Byte[] y)
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
    }
}
