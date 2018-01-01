using System;

namespace Dbf.Cdx
{
    [Flags]
    public enum CdxIndexOptions : byte
    {
        None                  =  0,
        Unique                =  1,
        HasForClause          =  8,
        IsCompactIndex        = 32,
        IsCompoundIndexHeader = 64,

        All = None | Unique | HasForClause | IsCompactIndex | IsCompoundIndexHeader
    }

    public enum CdxIndexOrder : ushort
    {
        Ascending  = 0,
        Descending = 1
    }

    [Flags]
    public enum CdxNodeAttributes : ushort
    {
        IndexNode = 0,
        RootNode  = 1,
        LeafNode  = 2,

        All = IndexNode | RootNode | LeafNode
    }
}
