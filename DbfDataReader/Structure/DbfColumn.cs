﻿using System.IO; 
 
namespace DbfDataReader 
{ 
    public class DbfColumn 
    { 
        public int           Index        { get; } 
        public string        Name         { get; } 
        public DbfColumnType ColumnType   { get; } 
        public int           Length       { get; } 
        public int           DecimalCount { get; } 
 
        public DbfColumn(BinaryReader binaryReader, int index) 
        { 
            this.Index            = index; 
            this.Name             = new string(binaryReader.ReadChars(11)).TrimEnd((char)0); 
            this.ColumnType       = (DbfColumnType)binaryReader.ReadByte(); 
             
            uint fieldDataAddress = binaryReader.ReadUInt32(); // ignore field data address 
 
            this.Length           = binaryReader.ReadByte(); 
            this.DecimalCount     = binaryReader.ReadByte(); 
                                   
            byte[] reserved       = binaryReader.ReadBytes(14);  // skip the reserved bytes 
        } 
    } 
}