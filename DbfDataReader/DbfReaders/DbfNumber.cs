using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbf.DbfReaders
{
#if NOT_NOW
    public struct DbfNumber : IEquatable<DbfNumber>, IComparable<DbfNumber>
    {
        private readonly Boolean hasInt;
        private readonly Decimal asDec;
        private readonly Int32   asInt;

        public DbfNumber(Decimal value)
        {
            this.hasInt = false;
            this.asDec  = value;
            this.asInt  = 0;
        }

        public DbfNumber(Int32 int32Value, Decimal decimalValue)
        {
            this.hasInt = true;
            this.asDec  = decimalValue;
            this.asInt  = int32Value;
        }

        public Boolean HasInt32 => this.hasInt;
        public Int32   Int32    => this.asInt;
        public Decimal Decimal  => this.asDec;

        #region Conversion

        public static implicit operator Decimal(DbfNumber value)
        {
            return value.asDec;
        }

        public static explicit operator Int32(DbfNumber value)
        {
            if( !value.hasInt ) throw new InvalidOperationException("The current value does not have an integer representation.");
            return value.asInt;
        }

        public static 

        #endregion

        #region Equalty

        public override Boolean Equals(Object obj)
        {
            if( obj is DbfNumber )
            {
                return this.Equals( (DbfNumber)obj );
            }
            else
            {
                return false;
            }
        }

        public Boolean Equals(DbfNumber other)
        {
            return this.asDec.Equals( other.asDec );
        }

        public override Int32 GetHashCode()
        {
            return this.asDec.GetHashCode();
        }

        public Int32 CompareTo(DbfNumber other)
        {
            return this.asDec.CompareTo( other.asDec );
        }

        public static Boolean operator==(DbfNumber first, DbfNumber second)
        {
            return first.asDec.Equals( second.asDec );
        }

        public static Boolean operator!=(DbfNumber first, DbfNumber second)
        {
            return !first.asDec.Equals( second.asDec );
        }

        public static Boolean operator<(DbfNumber first, DbfNumber second)
        {
            return first.asDec.CompareTo( second.asDec ) < 0;
        }

        public static Boolean operator>(DbfNumber first, DbfNumber second)
        {
            return first.asDec.CompareTo( second.asDec ) > 0;
        }

        #endregion
    }
#endif
}
