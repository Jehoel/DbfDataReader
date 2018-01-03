using System;

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
                
                // "Extended" BDF column types:
                case DbfColumnType.Double:
                    return DbfActualColumnType.FloatDouble;
                
                case DbfColumnType.SignedLong:
                    return DbfActualColumnType.Int32;

                default:
                    throw new ArgumentOutOfRangeException( nameof(columnType), columnType, "This DBF Table Type does not support the specified Column Type." ); 
            }
        }

        public virtual DbfMemoFile OpenMemoFile(String tableName)
        {
            throw new NotImplementedException();
        }

        /// <summary>Returns -1 if a column does not have a predefined fixed length.</summary>
        public static Int32 GetFixedColumnLength(DbfActualColumnType columnType)
        {
            switch( columnType )
            {
                case DbfActualColumnType.BooleanText         : return DbfActualColumnTypeLengths.BooleanText;
                case DbfActualColumnType.CurrencyInt64       : return DbfActualColumnTypeLengths.Int64;
                case DbfActualColumnType.DateText            : return DbfActualColumnTypeLengths.DateText;
                case DbfActualColumnType.DateTimeBinaryJulian: return DbfActualColumnTypeLengths.DateTimeBinaryJulian;
                case DbfActualColumnType.FloatDouble         : return DbfActualColumnTypeLengths.FloatDouble;
                case DbfActualColumnType.FloatSingle         : return DbfActualColumnTypeLengths.FloatSingle;
                case DbfActualColumnType.Int16               : return DbfActualColumnTypeLengths.Int16;
                case DbfActualColumnType.Int32               : return DbfActualColumnTypeLengths.Int32;
                case DbfActualColumnType.Int64               : return DbfActualColumnTypeLengths.Int64;
                case DbfActualColumnType.Memo10ByteArray     : return DbfActualColumnTypeLengths.Memo10;
                case DbfActualColumnType.Memo10Text          : return DbfActualColumnTypeLengths.Memo10;
                case DbfActualColumnType.Memo4ByteArray      : return DbfActualColumnTypeLengths.Memo4;
                case DbfActualColumnType.Memo4Text           : return DbfActualColumnTypeLengths.Memo4;
                case DbfActualColumnType.UInt16              : return DbfActualColumnTypeLengths.Int16;
                case DbfActualColumnType.UInt32              : return DbfActualColumnTypeLengths.Int32;
                case DbfActualColumnType.UInt64              : return DbfActualColumnTypeLengths.Int64;
                
                default:
                    return DbfActualColumnTypeLengths.Variable;
            }
        }

        public static Int32 GetColumnLength(DbfActualColumnType columnType, Int32 declaredLength)
        {
            Int32 fixedWidth = GetFixedColumnLength( columnType );
            if( fixedWidth != DbfActualColumnTypeLengths.Variable ) return fixedWidth;

            // TODO: Throw an exception if columnType is variable and declaredLength is <= 0 or > 256?
            return declaredLength;
        }
    }

    public static class DbfActualColumnTypeLengths
    {
        public const Int32 BooleanText          =  1;
        public const Int32 DateText             =  8;
        public const Int32 DateTimeBinaryJulian =  8;
        public const Int32 FloatDouble          =  8;
        public const Int32 FloatSingle          =  4;
        public const Int32 Memo4                =  4;
        public const Int32 Memo10               = 10;
        public const Int32 Int16                =  2;
        public const Int32 Int32                =  4;
        public const Int32 Int64                =  8;

        public const Int32 Variable             = -1;
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

                case DbfColumnType.Memo:
                    return DbfActualColumnType.Memo4Text;

                case DbfColumnType.General:
                    return DbfActualColumnType.Memo4ByteArray;
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
                    return DbfActualColumnType.Memo10ByteArray;
                
                case DbfColumnType.Memo:
                    return DbfActualColumnType.Memo10Text;

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
