using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Dbf.Cdx
{
    public class CdxIndexHeader
    {
        public static CdxIndexHeader Read(BinaryReader reader)
        {
            if( reader == null ) throw new ArgumentNullException(nameof(reader));

            Int64 start = reader.BaseStream.Position;

            UInt32 rootNodePointer         = reader.ReadUInt32();
            Int32  freeNodeListPointer     = reader.ReadInt32();
            UInt32 reserved1               = reader.ReadUInt32();
            UInt16 keyLength               = reader.ReadUInt16();
            CdxIndexOptions options    = (CdxIndexOptions)reader.ReadByte();
            Byte   signature               = reader.ReadByte();
            Byte[] reserved2               = reader.ReadBytes( 502 - 16 ); // 486 bytes
            CdxIndexOrder order               = (CdxIndexOrder)reader.ReadUInt16();
            UInt16 reserved3               = reader.ReadUInt16();
            UInt16 forExpressionPoolLength = reader.ReadUInt16();
            UInt16 reserved4               = reader.ReadUInt16();
            UInt16 keyExpressionPoolLength = reader.ReadUInt16();
            Byte[] keyExpression           = reader.ReadBytes( 512 );

#if DEBUG
            Int64 actualEnd = reader.BaseStream.Position;
            Int64 expectedEnd = start + 1024;
            if( actualEnd != expectedEnd ) throw new CdxException( CdxErrorCode.DidNotRead1024BytesInCdxIndexHeader );
            if( ( options | CdxIndexOptions.All ) != CdxIndexOptions.All ) throw new CdxException( CdxErrorCode.InvalidCdxIndexOptionsAttributes );
#endif
            
            return new CdxIndexHeader(
                start,

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

        private CdxIndexHeader(
            Int64 offset,

            UInt32 rootNodePointer,
            Int32 freeNodeListPointer,
            UInt32 reserved1,
            UInt16 keyLength,
            CdxIndexOptions options,
            Byte signature,
            Byte[] reserved2,
            CdxIndexOrder order,
            UInt16 reserved3,
            UInt16 forExpressionPoolLength,
            UInt16 reserved4,
            UInt16 keyExpressionPoolLength,
            Byte[] keyExpressionPool
        )
        {
            this.Offset = offset;

            this.RootNodePointer = rootNodePointer;
            this.FreeNodeListPointer = freeNodeListPointer;
            this.Reserved1 = reserved1;
            this.KeyLength = keyLength;
            this.Options = options;
            this.Signature = signature;
            this.Reserved2 = Array.AsReadOnly( reserved2 ?? throw new ArgumentNullException( nameof( reserved2 ) ) );
            this.Order = order;
            this.Reserved3 = reserved3;
            this.ForExpressionPoolLength = forExpressionPoolLength;
            this.Reserved4 = reserved4;
            this.KeyExpressionPoolLength = keyExpressionPoolLength;
            this.KeyExpressionPool = Array.AsReadOnly( keyExpressionPool );

            ///////////////

            this.KeyExpressionAsString = Encoding.ASCII.GetString( keyExpressionPool, 0, keyExpressionPoolLength );

            if( options.HasFlag( CdxIndexOptions.HasForClause ) )
            {
                this.ForExpressionAsString = Encoding.ASCII.GetString( keyExpressionPool, keyExpressionPoolLength, forExpressionPoolLength );
            }
            else
            {
                this.ForExpressionAsString = String.Empty;
            }
        }

        /// <summary>Offset in the index file this header was read at.</summary>
        public Int64 Offset { get; }

        /// <summary>Pointer to root node</summary>
        public UInt32 RootNodePointer { get; }
        
        /// <summary>Pointer to free node list ( -1 if not present)</summary>
        public Int32 FreeNodeListPointer { get; }

        /// <summary>Reserved for internal use</summary>
        public UInt32 Reserved1 { get; }

        /// <summary>Length of key</summary>
        public UInt16 KeyLength { get; }

        /// <summary>Index options</summary>
        public CdxIndexOptions Options { get; }

        /// <summary>Index signature</summary>
        public Byte Signature { get; }
        
        /// <summary>Bytes 16 through 501. Reserved for internal use.</summary>
        public ReadOnlyCollection<Byte> Reserved2 { get; }

        public CdxIndexOrder Order { get; }

        /// <summary>Bytes 504-505.</summary>
        public UInt16 Reserved3 { get; }

        public UInt16 ForExpressionPoolLength { get; }

        /// <summary>Bytes 508-509.</summary>
        public UInt16 Reserved4 { get; }

        public UInt16 KeyExpressionPoolLength { get; }

        public ReadOnlyCollection<Byte> KeyExpressionPool { get; }

        /////////////////////////
        
        public String KeyExpressionAsString { get; }

        public String ForExpressionAsString { get; }
    }
}
