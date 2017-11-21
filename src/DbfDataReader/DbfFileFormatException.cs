using System;
using System.Runtime.Serialization;

namespace DbfDataReader
{
    [Serializable]
    public class DbfFileFormatException : Exception
    {
        public DbfFileFormatException() { }
        public DbfFileFormatException(string message) : base( message ) { }
        public DbfFileFormatException(string message, Exception inner) : base( message, inner ) { }
        protected DbfFileFormatException(SerializationInfo info, StreamingContext context) : base( info, context ) { }
    }
}