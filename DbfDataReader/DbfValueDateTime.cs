﻿using System;
using System.Globalization;
using System.IO;

namespace DbfDataReader
{
    public class DbfValueDateTime : DbfValue<DateTime?>
    {
        public DbfValueDateTime(int length) : base(length)
        {
        }

        public override void Read(BinaryReader binaryReader)
        {
            var value = new string(binaryReader.ReadChars(8));
            value = value.TrimEnd((char)0);

            if (string.IsNullOrWhiteSpace(value))
            {
                Value = null;
            }
            else
            {
                //Value = DateTime.ParseExact(value, "yyyyMMdd", null, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite);
                Value = null;
            }
        }
    }
}