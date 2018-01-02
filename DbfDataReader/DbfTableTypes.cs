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
            else if( tableHeader.VersionFamily == DbfVersionFamily.DBase5 )
            {
                return DBase5TableType.Instance;
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
                case DbfColumnType.AutoIncrement:
                case DbfColumnType.SignedLong:
                    return DbfActualColumnType.Int32;
                
                case DbfColumnType.Logical:
                    return DbfActualColumnType.BooleanText;
                
                case DbfColumnType.Character:
                    return DbfActualColumnType.Text;
                
                case DbfColumnType.Date:
                case DbfColumnType.DateTime:
                case DbfColumnType.Double:
                case DbfColumnType.DoubleOrBinary:
                case DbfColumnType.Float:
                case DbfColumnType.General:
                case DbfColumnType.Memo:
                case DbfColumnType.Number:
                case DbfColumnType.Timestamp:

                case DbfColumnType.Currency:
                default:
                    throw new ArgumentOutOfRangeException( nameof(columnType), columnType, "This DBF Table Type does not support the specified Ccolumn Type." );
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
                case DbfColumnType.DoubleOrBinary:
                    return DbfActualColumnType.Int16;
                case DbfColumnType.Character:
                    return DbfActualColumnType.TextLong;
            }

            return base.GetActualColumnType( columnType );
        }

        public override DbfMemoFile OpenMemoFile(String tableName)
        {
            throw new NotImplementedException();
        }
    }

    public class DBase5TableType : DbfTableType
    {
        public static new FoxProTableType Instance { get; } = new FoxProTableType();

        public override DbfActualColumnType GetActualColumnType(DbfColumnType columnType)
        {
            switch( columnType )
            {
                case DbfColumnType.DoubleOrBinary:
                    return DbfActualColumnType.MemoByteArray;
            }

            return base.GetActualColumnType( columnType );
        }

        public override DbfMemoFile OpenMemoFile(String tableName)
        {
            throw new NotImplementedException();
        }
    }
}
