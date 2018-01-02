using System;
using System.Collections.Generic;

namespace Dbf.Cdx
{
    public static class IndexSearcher
    {
        private static LeafCdxNode GetLeftmostLeafNode(CdxIndex index)
        {
            BaseCdxNode node = index.RootNode;
            LeafCdxNode leftmostLeafNode = node as LeafCdxNode;
            if( leftmostLeafNode == null )
            {
                while( node != null )
                {
                    InteriorCdxNode interiorNode = (InteriorCdxNode)node;
                    if( interiorNode.KeyEntries.Length == 0 ) throw new CdxException( CdxErrorCode.InteriorNodeHasNoKeyEntries );

                    node = index.ReadNode( interiorNode.KeyEntries[0].NodePointer );
                    leftmostLeafNode = node as LeafCdxNode;
                    if( leftmostLeafNode != null ) node = null;
                }
            }

            // As we always chose the 0th key of an interior-node, the leftmost should have no left sibling:
            if( leftmostLeafNode.LeftSibling != BaseCdxNode.NoSibling ) throw new CdxException( CdxErrorCode.LeftmostNodeHasLeftSibling );

            return leftmostLeafNode;
        }

        public static IEnumerable<LeafCdxKeyEntry> GetAllKeys(CdxIndex index)
        {
            if( index == null ) throw new ArgumentNullException(nameof(index));

            LeafCdxNode currentLeaf = GetLeftmostLeafNode( index );
            while( true )
            {
                foreach( LeafCdxKeyEntry key in currentLeaf.IndexKeys )
                {
                    yield return key;
                }

                if( currentLeaf.RightSibling == BaseCdxNode.NoSibling ) break;
                
                currentLeaf = (LeafCdxNode)index.ReadNode( currentLeaf.RightSibling );
            }
        }

        public static Int32 Count(CdxIndex index)
        {
            if( index == null ) throw new ArgumentNullException(nameof(index));

            // Traverse to the left-most leaf-node, then traverse to the right, counting keys.
            // There is no need to store HashSet<> of keys, as the values are sorted, so just store the previous value to determine uniqueness (i.e. if current value equals the previous one).
            
            // uhhh, well the index keys are in-order, but the DbfRecordNumber is not, hmmmm.

            // let's just get the dumb count: the size of an index.

            LeafCdxNode currentLeaf = GetLeftmostLeafNode( index );
            Int32 total = 0;
            while( true )
            {
                total += currentLeaf.KeyCount;

                if( currentLeaf.RightSibling == BaseCdxNode.NoSibling ) break;
                
                currentLeaf = (LeafCdxNode)index.ReadNode( currentLeaf.RightSibling );
            }

            return total;
        }

        /// <summary>Returns all matching DBF Record Number values that match the specified key. The key length must match the index.</summary>
        public static IEnumerable<UInt32> SearchIndex(CdxIndex index, Byte[] targetKey)
        {
            if( index == null ) throw new ArgumentNullException(nameof(index));
            if( targetKey == null ) throw new ArgumentNullException(nameof(targetKey));
            if( index.Header.KeyLength != targetKey.Length ) throw new ArgumentException("The specified key value has a different length than the index key.", nameof(targetKey));

            BaseCdxNode node = index.RootNode;

            Boolean isAscending = index.Header.Order == CdxIndexOrder.Ascending;

            if( isAscending )
            {
                if( node is LeafCdxNode leafNode )
                {
                    return SearchLeafNodeAsc( index, leafNode, targetKey, SequentialByteArrayComparer.Instance );
                }
                else
                {
                    InteriorCdxNode interiorNode = (InteriorCdxNode)node;

                    return SearchInteriorNodeAsc( index, interiorNode, targetKey, SequentialByteArrayComparer.Instance );
                }
            }
            else
            {
                throw new NotSupportedException("Descending indexes are not currently supported.");
            }
        }

        public static IEnumerable<UInt32> SearchInteriorNodeAsc(CdxIndex index, InteriorCdxNode node, Byte[] targetKey, IComparer<Byte[]> comparer)
        {
            // Interior node's child-key's values are such that the pointee node's keys are all less-than-or-equal-to that value.
            // A recursive approach isn't necessary as we don't need to backtrack or otherwise re-visit internal nodes, only find the first leaf node and then linear-scan.
            // ...though using binary-search might provide some speed-up, but for now to be simple we'll use linear-scan.

            LeafCdxNode leafNode = null;

            while( node != null )
            {
                foreach( InteriorIndexKeyEntry key in node.KeyEntries )
                {
                    Int32 cmp = comparer.Compare( key.KeyBytes, targetKey );
                    if( cmp < 0 )
                    {
                        // If a key's value is less than the targetKey, then we can dismiss it, as all of its children will also be less-than-or-equal-to the target.
                    }
                    else
                    {
                        // If a key is exactly equal to the targetKey, then if it's a unique index then we can simply return the associated DbfRecordNumber and we're done.
                        // Otherwise, it means we need to go inside.

                        // If a key is greater than the target, and it's the first time it's been encountered, then we need to go inside, as its children might be matches (as they're all less-than-or-equal the value).

                        // But after seeing the first value, we can return immediately because the next key will have values greater than (and equal to?) the key's value.
                        // *If* the CDX is such that the node does have children equal to targetKey then they would be retrieved during the linear-scan of the Leaf Node by going for the Right-sibling node.

                        BaseCdxNode nextNode = index.ReadNode( key.NodePointer );
                        leafNode = nextNode as LeafCdxNode;
                        if( leafNode != null )
                        {
                            node = null;
                            break;
                        }
                        else
                        {
                            node = (InteriorCdxNode)nextNode;
                            break;
                        }
                    }
                }
            }

            if( leafNode != null )
            {
                IEnumerable<UInt32> searchLeafNodeResult = SearchLeafNodeAsc( index, leafNode, targetKey, comparer );
                foreach( UInt32 dbfRecordNumber in searchLeafNodeResult ) yield return dbfRecordNumber;
            }
        }

        public static IEnumerable<UInt32> SearchLeafNodeAsc(CdxIndex index, LeafCdxNode node, Byte[] targetKey, IComparer<Byte[]> comparer)
        {
            Int32 lastKeyIdx = -1;
            LeafCdxKeyEntry[] keys = node.IndexKeys;
            for( Int32 i = 0; i < keys.Length; i++ )
            {
                LeafCdxKeyEntry key = keys[i];

                Int32 cmp = comparer.Compare( key.KeyBytes, targetKey );
                if( cmp < 0 )
                {
                    // NOOP.
                }
                else if( cmp == 0 )
                {
                    yield return key.DbfRecordNumber;
                    lastKeyIdx = i;
                }
                else // cmp > 0 )
                {
                    // No possibility of any more matches, break.
                    break;
                }
            }

            if( lastKeyIdx == keys.Length - 1 && node.RightSibling != BaseCdxNode.NoSibling )
            {
                // Continue search on the next sibling node.
                LeafCdxNode rightSibling = (LeafCdxNode)index.ReadNode( node.RightSibling );

                IEnumerable<UInt32> rightSiblingResults = SearchLeafNodeAsc( index, rightSibling, targetKey, comparer );
                foreach( UInt32 dbfRecordNumber in rightSiblingResults ) yield return dbfRecordNumber;
            }
        }

        // There is no built-in BinarySearch for IList<T>, surprisingly.
        public static Int32 BinarySearch(IReadOnlyList<LeafCdxKeyEntry> list, Byte[] target, IComparer<Byte[]> comparer, Boolean isInAscendingOrder)
        {
            if( list == null ) throw new ArgumentNullException(nameof(list));
            if( target == null ) throw new ArgumentNullException(nameof(target));
            if( comparer == null ) throw new ArgumentNullException(nameof(comparer));

            Int32 lower = 0;
            Int32 upper = list.Count - 1;

            while( lower <= upper )
            {
                Int32 middle = lower + (upper - lower) / 2;
                Int32 comparisonResult = comparer.Compare( list[middle].KeyBytes, target );

                if( ( isInAscendingOrder && comparisonResult < 0 ) || ( !isInAscendingOrder && comparisonResult > 0 ) )
                {
                    upper = middle - 1;
                }
                else if( ( isInAscendingOrder && comparisonResult > 0 ) || ( !isInAscendingOrder && comparisonResult < 0 ) )
                {
                    lower = middle + 1;
                }
                else 
                {
                    return middle;
                }
            }

            return ~lower;
        }
    }

    public class SequentialByteArrayComparer : IComparer<Byte[]>
    {
        // I wonder if an unsafe-cast to UInt64* and comparing that way would be faster...

        Int32 IComparer<Byte[]>.Compare(Byte[] x, Byte[] y)
        {
            //return Compare( x, y );
            return CompareWithoutChecks( x, y );
        }

        public static Int32 Compare(Byte[] x, Byte[] y)
        {
            if( x == null ) throw new ArgumentNullException(nameof(x));
            if( y == null ) throw new ArgumentNullException(nameof(y));
            if( x.Length != y.Length ) throw new ArgumentException("Argument arrays have different lengths.");

            for( Int32 i = 0; i < x.Length; i++ )
            {
                Int32 cmp = x[i].CompareTo( y[i] );
                if( cmp != 0 ) return cmp;
            }

            return 0;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0" )]
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1" )]
        public static Int32 CompareWithoutChecks(Byte[] x, Byte[] y)
        {
            for( Int32 i = 0; i < x.Length; i++ )
            {
                Int32 cmp = x[i].CompareTo( y[i] );
                if( cmp != 0 ) return cmp;
            }

            return 0;
        }

        public static SequentialByteArrayComparer Instance { get; } = new SequentialByteArrayComparer();
    }
}
