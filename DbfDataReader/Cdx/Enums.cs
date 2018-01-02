using System;

namespace Dbf.Cdx
{
    [Flags]
    public enum CdxIndexOptions : byte
    {
        None                  =   0,
        Unique                =   1,
        CustomIndex           =   4, // Observed in CALLS.CDX's Call_ID tagged index. Mentioned, but not described, here: http://devzone.advantagedatabase.com/dz/Content.aspx?Key=17&RefNo=980917-0558 - though it doesn't seem to be proprietary to AdvantageDB.
        HasForClause          =   8,
        BitVector             =  16, // "Bit vector (SoftC)"
        IsCompactIndex        =  32, // "Compact index format (FoxPro)"
        IsCompoundIndexHeader =  64, // "Compounding index header (FoxPro)"
        IsStructuralIndex     = 128, // "Structure index (FoxPro)"

        All = None | Unique | CustomIndex | HasForClause | BitVector | IsCompactIndex | IsCompoundIndexHeader | IsStructuralIndex
    }

    public enum CdxIndexOrder : ushort
    {
        Ascending  = 0,
        Descending = 1
    }

    [Flags]
    public enum CdxNodeAttributes : ushort
    {
        /// <summary>Also referred to as "Index page" - though the term could also include leaf nodes.</summary>
        InteriorNode = 0,
        RootNode     = 1,
        /// <summary>Also referred to as "exterior node" or "exterior page".</summary>
        LeafNode     = 2,

        Unknown      = 4, // Seen in CALLS.CDX's Call_ID tagged index. Undocumented.

        All = InteriorNode | RootNode | LeafNode | Unknown
    }
}
