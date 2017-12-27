using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbf.CdxReaders
{
    // https://msdn.microsoft.com/en-us/library/s8tb8f47(v=vs.80).aspx
    // Compact Index *.idx and Compound Index *.cdx both share the same file structure.

    public sealed class CompactIndex : IDisposable
    {
        private readonly BinaryReader reader;

        private CompactIndex(CompactIndexHeader header, CompactIndexNode rootNode, BinaryReader reader)
        {
            this.Header   = header;
            this.RootNode = rootNode;
            this.reader   = reader;
        }

        public void Dispose()
        {
            this.reader.Dispose();
        }

        public BinaryReader Reader => this.reader;

        public CompactIndexHeader Header { get; }

        public CompactIndexNode RootNode { get; }

        public static CompactIndex Open(String fileName)
        {
            FileStream fs = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read );
            BinaryReader rdr = new BinaryReader( fs );

            CompactIndexHeader header = CompactIndexHeader.Read( rdr );

            rdr.BaseStream.Seek( header.RootNodePointer, SeekOrigin.Begin );

            CompactIndexNode rootNode = CompactIndexNode.Read( rdr );

            return new CompactIndex( header, rootNode, rdr );
        }
    }


    public class CompactIndexHeader
    {
        public CompactIndexHeader(UInt32 rootNodePointer, Int32 freeNodeListPointer, UInt32 reserved1, UInt16 keyLength, CompactIndexOptions options, Byte signature, Byte[] reserved2, IndexOrder order, UInt16 reserved3, UInt16 forExpressionPoolLength, UInt16 reserved4, UInt16 keyExpressionPoolLength, Byte[] keyExpressionPool)
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

        public static CompactIndexHeader Read(BinaryReader reader)
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

            Int64 actualEnd = reader.BaseStream.Position;
            Int64 expectedEnd = start + 1024;
            if( actualEnd != expectedEnd ) throw new InvalidOperationException("Did not read 1024 bytes exactly.");
            
            return new CompactIndexHeader(
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

    [Flags]
    public enum CompactIndexOptions : byte
    {
        None            =  0,
        Unique          =  1,
        HasForClause    =  8,
        IsCompactIndex  = 32,
        IsCompoundIndex = 64
    }

    public enum IndexOrder : ushort
    {
        Ascending  = 0,
        Descending = 1
    }

    public abstract class CompactIndexNode
    {
        protected CompactIndexNode(
            Int64 offset,
            CompactIndexNodeAttributes attributes,
            UInt16 keyCount,
            Int32 leftSibling,
            Int32 rightSibling
        )
        {
            this.Offset       = offset;
            this.Attributes   = attributes;
            this.KeyCount     = keyCount;
            this.LeftSibling  = leftSibling;
            this.RightSibling = rightSibling;
        }

        public Int64                      Offset       { get; }
        public CompactIndexNodeAttributes Attributes   { get; }
        public UInt16                     KeyCount     { get; }
        public Int32                      LeftSibling  { get; }
        public Int32                      RightSibling { get; }

        public static CompactIndexNode Read(BinaryReader reader)
        {
            // TODO: Confirm that Leaf Node == External Node, and !LeafNode == Interior Node...
            Int64 offset = reader.BaseStream.Position;
            CompactIndexNodeAttributes attributes = (CompactIndexNodeAttributes)reader.ReadUInt16();
            if( attributes.HasFlag( CompactIndexNodeAttributes.LeafNode ) )
            {
                return CompactIndexExteriorNode.Read( offset, attributes, reader );
            }
            else
            {
                return CompactIndexInteriorNode.Read( offset, attributes, reader );
            }
        }
    }

    public class CompactIndexInteriorNode : CompactIndexNode
    {
        public CompactIndexInteriorNode(Int64 offset, CompactIndexNodeAttributes attributes, UInt16 keyCount, Int32 leftSibling, Int32 rightSibling, Byte[] keyValue)
            : base( offset, attributes, keyCount, leftSibling, rightSibling )
        {
            this.KeyValue = keyValue ?? throw new ArgumentNullException( nameof( keyValue ) );
        }
        
        public Byte[] KeyValue { get; }

        public static CompactIndexInteriorNode Read(Int64 offset, CompactIndexNodeAttributes attributes, BinaryReader reader)
        {
            //CompactIndexNodeAttributes attributes = (CompactIndexNodeAttributes)reader.ReadUInt16();
            UInt16 keyCount     = reader.ReadUInt16();
            Int32 leftSibling   = reader.ReadInt32();
            Int32 rightSibling  = reader.ReadInt32();
            Byte[] keyValue     = reader.ReadBytes(500);

            return new CompactIndexInteriorNode(
                offset,
                attributes,
                keyCount,
                leftSibling,
                rightSibling,
                keyValue
            );
        }
    }

    public class CompactIndexExteriorNode : CompactIndexNode
    {
        public CompactIndexExteriorNode(Int64 offset, CompactIndexNodeAttributes attributes, UInt16 keyCount, Int32 leftSibling, Int32 rightSibling, UInt16 freeSpace, UInt32 recordNumberMask, Byte duplicateByteCountMask, Byte trailingByteCountMask, Byte recordNumberBitsCount, Byte duplicateCountBitsCount, Byte trailCountBitsCount, Byte recordNumberDuplicateCountTrailingCountBytes, Byte[] indexKeys)
            : base( offset, attributes, keyCount, leftSibling, rightSibling )
        {
            this.FreeSpace = freeSpace;
            this.RecordNumberMask = recordNumberMask;
            this.DuplicateByteCountMask = duplicateByteCountMask;
            this.TrailingByteCountMask = trailingByteCountMask;
            this.RecordNumberBitsCount = recordNumberBitsCount;
            this.DuplicateCountBitsCount = duplicateCountBitsCount;
            this.TrailCountBitsCount = trailCountBitsCount;
            this.IndexKeyEntryLength = recordNumberDuplicateCountTrailingCountBytes;
            this.IndexKeys = indexKeys ?? throw new ArgumentNullException( nameof( indexKeys ) );
        }

        public UInt16 FreeSpace { get; }
        public UInt32 RecordNumberMask { get; }
        public Byte   DuplicateByteCountMask { get; }
        public Byte   TrailingByteCountMask { get; }
        public Byte   RecordNumberBitsCount { get; }
        public Byte   DuplicateCountBitsCount { get; }
        public Byte   TrailCountBitsCount { get; }
        /// <summary>Number of bytes holding record number, duplicate count and trailing count</summary>
        public Byte   IndexKeyEntryLength { get; }
        public Byte[] IndexKeys { get; }

        private const Int32 IndexKeyBufferLength = 488;

        public static CompactIndexExteriorNode Read(Int64 offset, CompactIndexNodeAttributes attributes, BinaryReader reader)
        {
            //CompactIndexNodeAttributes attributes = (CompactIndexNodeAttributes)reader.ReadUInt16();
            UInt16 keyCount                = reader.ReadUInt16();
            Int32  leftSibling             = reader.ReadInt32();
            Int32  rightSibling            = reader.ReadInt32();
            UInt16 freeSpace               = reader.ReadUInt16();
            UInt32 recordNumberMask        = reader.ReadUInt32();
            Byte   duplicateByteCountMask  = reader.ReadByte();
            Byte   trailingByteCountMask   = reader.ReadByte();
            Byte   recordNumberBitsCount   = reader.ReadByte();
            Byte   duplicateCountBitsCount = reader.ReadByte();
            Byte   trailCountBitsCount     = reader.ReadByte();
            Byte   recordNumberDuplicateCountTrailingCountBytes = reader.ReadByte();
            Byte[] indexKeys               = reader.ReadBytes( IndexKeyBufferLength );

            Int64 posActual = reader.BaseStream.Position;
            Int64 posExpected = offset + 512;
            if( posActual != posExpected ) throw new InvalidOperationException("Didn't read expected number of bytes in CompactIndexExteriorNode.");

            return new CompactIndexExteriorNode(
                offset,
                attributes,
                keyCount,
                leftSibling,            
                rightSibling,           
                freeSpace,              
                recordNumberMask,       
                duplicateByteCountMask, 
                trailingByteCountMask,  
                recordNumberBitsCount,  
                duplicateCountBitsCount,
                trailCountBitsCount,    
                recordNumberDuplicateCountTrailingCountBytes,
                indexKeys              
            );
        }

        public IEnumerable<CompactIndexExteriorNodeIndexEntry> GetIndexEntries(CompactIndex indexFile)
        {
            BinaryReader reader = indexFile.Reader;

            // Move reader to the start of the IndexKeys block, at 24 bytes offset from the start of the ExteriorNode.
            reader.BaseStream.Seek( this.Offset + 24, SeekOrigin.Begin );

            Int64 end = this.Offset + 512; // TODO: limit this to non-garbage data.

            if( this.IndexKeyEntryLength > 8 ) throw new InvalidOperationException("IndexKey entries must be 8 bytes long or shorter."); // because we use UInt64 for bitwise operations.

            Int32 totalBits = this.RecordNumberBitsCount + this.DuplicateCountBitsCount + this.TrailCountBitsCount;
            if( this.IndexKeyEntryLength * 8 != totalBits ) throw new InvalidOperationException("IndexKeyEntryLength does not match the combined bit count.");
            // for now, we assume all bit-lengths occupy full bytes:
            //if( this.RecordNumberBitsCount   % 8 != 0 ) throw new InvalidOperationException("RecordNumberBitsCount is not an integral number of bytes.");
            //if( this.DuplicateCountBitsCount % 8 != 0 ) throw new InvalidOperationException("DuplicateCountBitsCount is not an integral number of bytes.");
            //if( this.TrailCountBitsCount     % 8 != 0 ) throw new InvalidOperationException("TrailCountBitsCount is not an integral number of bytes.");

            /*Byte[] entryBuffer = new Byte[ this.IndexKeyEntryLength ];

            while( reader.BaseStream.Position < end )
            {
                {
                    Int32 read = reader.Read( entryBuffer, 0, this.IndexKeyEntryLength );
                    if( read != this.IndexKeyEntryLength ) throw new InvalidOperationException("Could not read all bytes of an entry.");

                    // reverse the array because BitConverter will use native byte ordering:
                    //if( BitConverter.IsLittleEndian ) Array.Reverse( entryBuffer );
                }
                
                UInt64 entry = BitConverter.ToUInt64( entryBuffer, 0 );

                // entry format:
                // <record-number><duplicate-bytes-count><trail-count>
                Int32 shiftToGetRecordNumber        = 64 - this.RecordNumberBitsCount;
                Int32 shiftToGetDuplicateBytesCount = ( 64 - this.RecordNumberBitsCount ) - this.DuplicateCountBitsCount;
                Int32 shiftToGetTrailBytesCount     = ( ( 64 - this.RecordNumberBitsCount ) - this.DuplicateCountBitsCount ) - this.TrailCountBitsCount;

                UInt64 recordNumber        = ( entry >> shiftToGetRecordNumber        ) & this.RecordNumberMask;
                UInt64 duplicateBytesCount = ( entry >> shiftToGetDuplicateBytesCount ) & this.DuplicateByteCountMask;
                UInt64 trailBytesCount     = ( entry >> shiftToGetTrailBytesCount     ) & this.TrailingByteCountMask;

                yield return new CompactIndexExteriorNodeIndexEntry( (UInt32)recordNumber, (UInt32)duplicateBytesCount, (UInt32)trailBytesCount );
            }*/

            Boolean supportedBitPacking =
                this.IndexKeyEntryLength == 4 &&
                this.RecordNumberBitsCount == 24 &&
                this.RecordNumberMask == 0x00FFFFFF &&
                this.DuplicateCountBitsCount == 4 &&
                this.DuplicateByteCountMask == 0x0F &&
                this.TrailCountBitsCount == 4 &&
                this.TrailingByteCountMask == 0x0F;

            if( !supportedBitPacking ) throw new NotSupportedException("Unsupported bit-packing options.");

            //////

            Int32 keyValueSrc = IndexKeyBufferLength; // 488

            for( Int32 i = 0; i < this.KeyCount; i++ )
            {
                Byte[] entryBuffer = new Byte[ this.IndexKeyEntryLength ];
                {
                    Int32 read = reader.Read( entryBuffer, 0, this.IndexKeyEntryLength );
                    if( read != this.IndexKeyEntryLength ) throw new InvalidOperationException("Could not read all bytes of an entry.");
                }

                Int32 recordNumberInt =
                    ( entryBuffer[3] << 24 ) |
                    ( entryBuffer[2] << 16 ) |
                    ( entryBuffer[1] <<  8 ) |
                    ( entryBuffer[0] );

                Int64 recordNumber64 = recordNumberInt & this.RecordNumberMask;
                UInt32 recordNumber = (UInt32)recordNumber64;

                Int32 bi = 16 - this.TrailCountBitsCount - this.DuplicateCountBitsCount;

                //////

                Int32 trailAndDupeInt = ( entryBuffer[3] << 8 ) | ( entryBuffer[2] );
                UInt16 trailAndDupe = (UInt16)( trailAndDupeInt >> bi );

                Int32 duplicateBytesCount = ( i == 0 ) ? 0 : ( trailAndDupe & this.DuplicateByteCountMask );
                Int32 trailingBytesCount  = ( trailAndDupe >> this.DuplicateCountBitsCount ) & this.TrailingByteCountMask;

                Int32 newBytesCount = indexFile.Header.KeyLength - duplicateBytesCount - trailingBytesCount;

                keyValueSrc -= newBytesCount;
                Int32 keyValueStart = keyValueSrc;

                CompactIndexExteriorNodeIndexEntry entry = new CompactIndexExteriorNodeIndexEntry()
                {
                    EntryBytes = entryBuffer,
                    RecordNumber = recordNumber,
                    DuplicateBytes = duplicateBytesCount,
                    TrailingBytes = trailingBytesCount,

                    KeyValueIndex0 = keyValueStart,
                    KeyValueIndexN = keyValueStart + newBytesCount
                };

//                if( duplicateBytesCount > 0 )
//                {
//                    entry.KeyValueRanges.Add( new Range( ,  ) );
//                }

                yield return entry;
            }
        }
    }

    [DebuggerDisplay("KeyValue = {KeyValue}, RecordNumber = {RecordNumber}")]
    public class CompactIndexExteriorNodeIndexEntry
    {
        public Byte[] EntryBytes     { get; set; }
        public UInt32 RecordNumber   { get; set; }
        public Int32  DuplicateBytes { get; set; } // what does "duplicate bytes" mean?
        public Int32  TrailingBytes  { get; set; }
        
//        public List<Range> KeyValueRanges { get; } = new List<Range>();

        public Int32  KeyValueIndex0 { get; set; }
        public Int32  KeyValueIndexN { get; set; }
        public Int32  KeyValueLength => this.KeyValueIndexN - this.KeyValueIndex0; //this.KeyValueRanges.Sum( r => r.Length );

        public String KeyValue { get; set; }
    }

    [Flags]
    public enum CompactIndexNodeAttributes : ushort
    {
        IndexNode = 0,
        RootNode  = 1,
        LeafNode  = 2
    }

    public struct Range
    {
        public Range(UInt32 start, UInt32 end)
        {
            this.Start = start;
            this.End   = end;
        }

        public UInt32 Start { get; }
        public UInt32 End   { get; }
        public UInt32 Length => this.End - this.Start;
    }
}
