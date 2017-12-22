using System;
using System.Collections.Generic;

namespace Dbf
{
    internal static class Extensions
    {
        public static Boolean IsMonotonicallyIncreasing<T>(this IEnumerable<T> source)
            where T : IComparable<T>
        {
            return IsMonotonically( source, isIncreasing: true, allowEquals: false );
        }

        public static Boolean IsMonotonicallyDecreasing<T>(this IEnumerable<T> source)
            where T : IComparable<T>
        {
            return IsMonotonically( source, isIncreasing: false, allowEquals: false );
        }

        private static Boolean IsMonotonically<T>(IEnumerable<T> source, Boolean isIncreasing, Boolean allowEquals)
            where T : IComparable<T>
        {
            T last = default(T);

            Boolean first = true;
            foreach( T item in source )
            {
                if( first )
                {
                    first = false;
                }
                else
                {
                    if( isIncreasing )
                    {
                        if( allowEquals )
                        {
                            if( item.CompareTo( last ) < 0 ) return false;
                        }
                        else
                        {
                            if( item.CompareTo( last ) <= 0 ) return false;
                        }
                    }
                    else
                    {
                        if( allowEquals )
                        {
                            if( item.CompareTo( last ) > 0 ) return false;
                        }
                        else
                        {
                            if( item.CompareTo( last ) >= 0 ) return false;
                        }
                    }
                }

                last = item;
            }

            return true;
        }
    }
}
