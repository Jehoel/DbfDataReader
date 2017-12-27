using System;
using System.IO;

namespace Dbf.Cdx
{
    public class CdxFileHeader
    {
        private CdxFileHeader(UInt32 rootNodePointer, Int32 freeNodeListPointer, UInt32 reserved1, UInt16 keyLength, CompactIndexOptions options, Byte signature, Byte[] reserved2, IndexOrder order, UInt16 reserved3, UInt16 forExpressionPoolLength, UInt16 reserved4, UInt16 keyExpressionPoolLength, Byte[] keyExpressionPool)
        {
            this.RootNodePointer = rootNodePointer;
            this.FreeNodeListPointer = freeNodeListPointer;
            this.Reserved1 = reserved1;
            this.KeyLength = keyLength;
            this.Options = options;
            this.Signature = signature;
            this.Reserved2 = reserved2 ?? throw new ArgumentNullException( nameof( reserved2 ) );
            this.Order = order;
            this.Reserved3 = reserved3;
            this.ForExpressionPoolLength = forExpressionPoolLength;
            this.Reserved4 = reserved4;
            this.KeyExpressionPoolLength = keyExpressionPoolLength;
            this.KeyExpressionPool = keyExpressionPool;
        }

        /// <summary>Pointer to root node</summary>
        public UInt32 RootNodePointer { get; }
        
        /// <summary>Pointer to free node list ( -1 if not present)</summary>
        public Int32 FreeNodeListPointer { get; }

        /// <summary>Reserved for internal use</summary>
        public UInt32 Reserved1 { get; }

        /// <summary>Length of key</summary>
        public UInt16 KeyLength { get; }

        /// <summary>Index options</summary>
        public CompactIndexOptions Options { get; }

        /// <summary>Index signature</summary>
        public Byte Signature { get; }
        
        /// <summary>Bytes 16 through 501. Reserved for internal use.</summary>
        public Byte[] Reserved2 { get; }

        public IndexOrder Order { get; }

        /// <summary>Bytes 504-505.</summary>
        public UInt16 Reserved3 { get; }

        public UInt16 ForExpressionPoolLength { get; }

        /// <summary>Bytes 508-509.</summary>
        public UInt16 Reserved4 { get; }

        public UInt16 KeyExpressionPoolLength { get; }

        public Byte[] KeyExpressionPool { get; }

        /////////////////

        public static CdxFileHeader Read(BinaryReader reader)
        {
            Int64 start = reader.BaseStream.Position;

            UInt32 rootNodePointer         = reader.ReadUInt32();
            Int32  freeNodeListPointer     = reader.ReadInt32();
            UInt32 reserved1               = reader.ReadUInt32();
            UInt16 keyLength               = reader.ReadUInt16();
            CompactIndexOptions options    = (CompactIndexOptions)reader.ReadByte();
            Byte   signature               = reader.ReadByte();
            Byte[] reserved2               = reader.ReadBytes( 502 - 16 ); // 486 bytes
            IndexOrder order               = (IndexOrder)reader.ReadUInt16();
            UInt16 reserved3               = reader.ReadUInt16();
            UInt16 forExpressionPoolLength = reader.ReadUInt16();
            UInt16 reserved4               = reader.ReadUInt16();
            UInt16 keyExpressionPoolLength = reader.ReadUInt16();
            Byte[] keyExpression           = reader.ReadBytes( 512 );

#if DEBUG
            Int64 actualEnd = reader.BaseStream.Position;
            Int64 expectedEnd = start + 1024;
            if( actualEnd != expectedEnd ) throw new InvalidOperationException("Did not read 1024 bytes exactly.");
#endif
            
            return new CdxFileHeader(
                rootNodePointer,
                freeNodeListPointer,
                reserved1,
                keyLength,
                options,
                signature,
                reserved2,
                order,
                reserved3,
                forExpressionPoolLength,
                reserved4,
                keyExpressionPoolLength,
                keyExpression
            );
        }
    }
}
