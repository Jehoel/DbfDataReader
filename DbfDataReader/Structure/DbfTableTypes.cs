using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbf
{
    public interface IDbfTableType
    {
        DbfActualColumnType GetActualColumnType(DbfColumnType columnType);

        DbfMemoFile OpenMemoFile(String tableName);
    }

    public class DbfTableType : IDbfTableType
    {
        public static DbfTableType Instance { get; } = new DbfTableType();

        public static IDbfTableType GetDbfTableType(DbfHeader tableHeader)
        {
            if( tableHeader == null ) throw new ArgumentNullException(nameof(tableHeader));

            if( tableHeader.IsFoxPro )
            {
                return FoxProTableType.Instance;
            }
            else if( tableHeader.VersionFamily == DbfVersionFamily.DBase3 || tableHeader.VersionFamily == DbfVersionFamily.DBase4 || tableHeader.VersionFamily == DbfVersionFamily.DBase5 )
            {
                return DBaseTableType.Instance;
            }
            else
            {
                return DbfTableType.Instance;
            }
        }

        public virtual DbfActualColumnType GetActualColumnType(DbfColumnType columnType)
        {
            switch( columnType )
            {
                // Baseline DBF column types:
                // http://devzone.advantagedatabase.com/dz/webhelp/advantage9.0/server1/dbf_field_types_and_specifications.htm

                case DbfColumnType.Character:
                    return DbfActualColumnType.Text;
                
                case DbfColumnType.Number:
                    return DbfActualColumnType.NumberText;
                
                case DbfColumnType.Date:
                    return DbfActualColumnType.DateText;
                
                case DbfColumnType.Logical:
                    return DbfActualColumnType.BooleanText;
                
                case DbfColumnType.Memo:
                    return DbfActualColumnType.MemoText;
                
                // "Extended" BDF column types:
                case DbfColumnType.Double:
                    return DbfActualColumnType.FloatDouble;
                
                case DbfColumnType.SignedLong:
                    return DbfActualColumnType.Int32;
                
                // The AdvantageDB documentation mentions "ShortDate', "Image" and "Binary" - but I can't find exact documentation on their format. I assume 'Image' and 'Binary' refer to General?
                case DbfColumnType.General:
                    return DbfActualColumnType.MemoByteArray;

                default:
                    throw new ArgumentOutOfRangeException( nameof(columnType), columnType, "This DBF Table Type does not support the specified Column Type." ); 
            }
        }

        public virtual DbfMemoFile OpenMemoFile(String tableName)
        {
            throw new NotImplementedException();
        }
    }

    public class FoxProTableType : DbfTableType
    {
        public static new FoxProTableType Instance { get; } = new FoxProTableType();

        public override DbfActualColumnType GetActualColumnType(DbfColumnType columnType)
        {
            // https://docs.microsoft.com/en-us/sql/odbc/microsoft/visual-foxpro-field-data-types

            switch( columnType )
            {
                case DbfColumnType.B:
                    return DbfActualColumnType.Int16;
                
                case DbfColumnType.Character:
                    return DbfActualColumnType.TextLong;
                
                case DbfColumnType.Currency:
                    return DbfActualColumnType.CurrencyInt64; // 64-bit integer with 4 implied decimal digits, so divide by 1000 to get decimal currency.
                
                case DbfColumnType.DateTime:
                    return DbfActualColumnType.DateTimeBinaryJulian;

                case DbfColumnType.Timestamp:
                    return DbfActualColumnType.DateTimeBinaryJulian;
                    
                case DbfColumnType.AutoIncrement:
                    return DbfActualColumnType.UInt32;
                
                case DbfColumnType.VarChar:
                    return DbfActualColumnType.Text; // exact the text has trailing padding removed. TODO: Implement this.
                
                case DbfColumnType.VarBinary:
                    return DbfActualColumnType.ByteArray;

                case DbfColumnType.NullFlags:
                    return DbfActualColumnType.NullFlags;
            }

            return base.GetActualColumnType( columnType );
        }

        public override DbfMemoFile OpenMemoFile(String tableName)
        {
            throw new NotImplementedException();
        }
    }

    public class DBaseTableType : DbfTableType
    {
        public static new DBaseTableType Instance { get; } = new DBaseTableType();

        public override DbfActualColumnType GetActualColumnType(DbfColumnType columnType)
        {
            switch( columnType )
            {
                case DbfColumnType.B:
                    return DbfActualColumnType.MemoByteArray;

                case DbfColumnType.DateTime:
                    return DbfActualColumnType.DateTimeBinaryJulian;
            }

            return base.GetActualColumnType( columnType );
        }

        public override DbfMemoFile OpenMemoFile(String tableName)
        {
            throw new NotImplementedException();
        }
    }
}
