using System;

namespace Dbf.Cdx
{
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

    [Flags]
    public enum CompactIndexNodeAttributes : ushort
    {
        IndexNode = 0,
        RootNode  = 1,
        LeafNode  = 2
    }
}
