using System;
using System.IO;
using System.Security.AccessControl;

namespace DbfDataReader
{
    internal static class Utility
    {
        public static FileStream OpenFileForReading(String fileName, Boolean randomAccess, Boolean async)
        {
            FileOptions options = ( randomAccess ? FileOptions.RandomAccess : FileOptions.SequentialScan ) | ( async ? FileOptions.Asynchronous : FileOptions.None );

            return new FileStream( fileName, FileMode.Open, FileSystemRights.ReadData, FileShare.ReadWrite, 4096, options );
        }

        public static Int32 GetDbfColumnTypeLength(DbfColumnType type, Int32 declaredLength)
        {
            // https://www.clicketyclick.dk/databases/xbase/format/data_types.html

            switch( type )
            {
                case DbfColumnType.Boolean       : return 1;
                case DbfColumnType.Character     : return declaredLength;
                case DbfColumnType.Currency      : return declaredLength; // see original version of DbfValueCurrency.cs
                case DbfColumnType.Date          : return 8;
                case DbfColumnType.DateTime      : return 8;
                case DbfColumnType.DoubleOrBinary: throw new NotImplementedException(); // if FoxPro then 8, else declaredLength...
                case DbfColumnType.Float         : return 20;
                case DbfColumnType.General       : throw new NotImplementedException();
                case DbfColumnType.Memo          : return 10; // value is a pointer to a field in a memo file.
                case DbfColumnType.Number        : return 20; // FoxPro and Clipper: 20 chars, 18 in dBase.
                case DbfColumnType.SignedLong    : return 4;
                case DbfColumnType.AutoIncrement : return 4; // 'long' == 4
                case DbfColumnType.Timestamp     : return 8;
                case DbfColumnType.Double        : return 8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
