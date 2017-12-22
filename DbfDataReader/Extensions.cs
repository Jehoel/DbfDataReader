using System;
using System.Collections.Generic;
using System.Globalization;

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

        public static String FormatInvariant(this String format, params Object[] args)
        {
            return String.Format( CultureInfo.InvariantCulture, format, args );
        }

        public static String FormatCurrent(this String format, params Object[] args)
        {
            return String.Format( CultureInfo.CurrentCulture, format, args );
        }

        public static String ToStringInvariant<T>(this T value, String format)
            where T : IFormattable
        {
            return value.ToString( format, CultureInfo.InvariantCulture );
        }

        public static String ToStringCurrent<T>(this T value, String format)
            where T : IFormattable
        {
            return value.ToString( format, CultureInfo.CurrentCulture );
        }

        public static String ToStringInvariant<T>(this T value)
            where T : IConvertible
        {
            return value.ToString( CultureInfo.InvariantCulture );
        }

        public static String ToStringCurrent<T>(this T value)
            where T : IConvertible
        {
            return value.ToString( CultureInfo.CurrentCulture );
        }
    }
}
