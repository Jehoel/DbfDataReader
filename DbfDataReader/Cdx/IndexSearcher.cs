using System;
using System.Collections.Generic;
using System.Linq;

namespace Dbf.Cdx
{
    public static class IndexSearcher
    {
        #region Utility

        private static LeafCdxNode GetLeftmostLeafNode(CdxIndex index)
        {
            BaseCdxNode node = index.RootNode;
            LeafCdxNode leftmostLeafNode = node as LeafCdxNode;
            if( leftmostLeafNode == null )
            {
                while( node != null )
                {
                    InteriorCdxNode interiorNode = (InteriorCdxNode)node;
                    if( interiorNode.KeyEntries.Count == 0 ) throw new CdxException( CdxErrorCode.InteriorNodeHasNoKeyEntries );

                    node = index.ReadNode( interiorNode.KeyEntries[0].NodePointer );
                    leftmostLeafNode = node as LeafCdxNode;
                    if( leftmostLeafNode != null ) node = null;
                }
            }

            // As we always chose the 0th key of an interior-node, the leftmost should have no left sibling:
            if( leftmostLeafNode.LeftSibling != BaseCdxNode.NoSibling ) throw new CdxException( CdxErrorCode.LeftmostNodeHasLeftSibling );

            return leftmostLeafNode;
        }

        #endregion

        #region Get

        public static IEnumerable<LeafCdxKeyEntry> GetAll(CdxIndex index)
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

        /// <summary>Applies the specified predicate to all leaf keys in an index. Use only if you really need to check every key entry. Use SearchIndex instead for most use-cases.</summary>
        public static IEnumerable<LeafCdxKeyEntry> GetAll(CdxIndex index, Func<Byte[],Boolean> keyPredicate)
        {
            if( index == null ) throw new ArgumentNullException(nameof(index));
            if( keyPredicate == null ) throw new ArgumentNullException(nameof(keyPredicate));

#if REIMPLEMENTATION
            LeafCdxNode currentLeaf = GetLeftmostLeafNode( index );
            while( true )
            {
                foreach( LeafCdxKeyEntry keyEntry in currentLeaf.IndexKeys )
                {
                    if( keyPredicate( keyEntry.KeyBytes ) ) yield return keyEntry;
                }

                if( currentLeaf.RightSibling == BaseCdxNode.NoSibling ) break;
                
                currentLeaf = (LeafCdxNode)index.ReadNode( currentLeaf.RightSibling );
            }
#else
            return GetAll( index ).Where( ke => keyPredicate( ke.KeyBytes ) );
#endif
        }

        #endregion

        #region Count

        public static Int32 CountAll(CdxIndex index)
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

        /// <summary>Applies the specified predicate to all leaf keys in an index. Use only if you really need to check every key entry. Use CountRange() instead for most use-cases.</summary>
        public static Int32 CountAll(CdxIndex index, Func<Byte[],Boolean> keyPredicate)
        {
            if( index == null ) throw new ArgumentNullException(nameof(index));
            if( keyPredicate == null ) throw new ArgumentNullException(nameof(keyPredicate));

            Int32 total = 0;

            LeafCdxNode currentLeaf = GetLeftmostLeafNode( index );
            while( true )
            {
                foreach( LeafCdxKeyEntry keyEntry in currentLeaf.IndexKeys )
                {
                    if( keyPredicate( keyEntry.KeyBytes ) ) total += 1;
                }

                if( currentLeaf.RightSibling == BaseCdxNode.NoSibling ) break;
                
                currentLeaf = (LeafCdxNode)index.ReadNode( currentLeaf.RightSibling );
            }

            return total;
        }

        /// <summary>Counts the results from SearchIndex.</summary>
        public static Int32 CountRange(CdxIndex index, Func<Byte[],Int32> keyComparison)
        {
            if( index == null ) throw new ArgumentNullException(nameof(index));
            if( keyComparison == null ) throw new ArgumentNullException(nameof(keyComparison));

#if REIMPLEMENTATION
            Int32 total = 0;
            LeafCdxNode currentLeaf = GetLeftmostLeafNode( index );
            while( currentLeaf != null )
            {
                foreach( LeafCdxKeyEntry keyEntry in currentLeaf.IndexKeys )
                {
                    Int32 cmp = keyComparison( keyEntry.KeyBytes );
                    if( cmp < 0 )
                    {
                        // NOOP
                    }
                    else if( cmp == 0 )
                    {
                        total += 1;
                    }
                    else if( cmp > 0 )
                    {
                        return total;
                    }
                }

                if( currentLeaf.RightSibling == BaseCdxNode.NoSibling ) currentLeaf = null;
                else                                                    currentLeaf = (LeafCdxNode)index.ReadNode( currentLeaf.RightSibling );
            }
            return total;
#else
            return SearchIndex( index, keyComparison ).Count();
#endif
        }

        #endregion

        #region Search

        /// <summary>Returns all matching DBF Record Number values that exactly match the specified key. The key length must match the index.</summary>
        public static IEnumerable<LeafCdxKeyEntry> SearchIndex(CdxIndex index, Byte[] targetKey)
        {
            if( index == null ) throw new ArgumentNullException(nameof(index));
            if( targetKey == null ) throw new ArgumentNullException(nameof(targetKey));
            if( targetKey.Length != index.Header.KeyLength ) throw new ArgumentException( "Value array length does not match the index's key length.", nameof(targetKey) );

            if( BuildOptions.NativeCompare )
            {
                return SearchIndex( index, keyComparison: b => SequentialByteArrayComparer.CompareWithoutChecksNative( b, targetKey ) );
            }
            else
            {
                return SearchIndex( index, keyComparison: b => SequentialByteArrayComparer.CompareWithoutChecks( b, targetKey ) );
            }
        }

        public static IEnumerable<LeafCdxKeyEntry> SearchIndex(CdxIndex index, Func<Byte[],Int32> keyComparison)
        {
            if( index == null ) throw new ArgumentNullException(nameof(index));
            if( keyComparison == null ) throw new ArgumentNullException(nameof(keyComparison));

            Boolean isAscending = index.Header.Order == CdxIndexOrder.Ascending;

            if( isAscending )
            {
                if( index.RootNode is InteriorCdxNode interiorNode )
                {
                    return SearchInteriorNodesAsc( index, interiorNode, keyComparison );
                }
                else if( index.RootNode is LeafCdxNode leafNode )
                {
                    return SearchLeafNodeAsc( index, leafNode, keyComparison );
                }
                else
                {
                    return Enumerable.Empty<LeafCdxKeyEntry>();
                }
            }
            else
            {
                throw new NotSupportedException("Descending indexes are not currently supported.");
            }
        }

        private static IEnumerable<LeafCdxKeyEntry> SearchInteriorNodesAsc(CdxIndex index, InteriorCdxNode node, Func<Byte[],Int32> keyComparison)
        {
            // Interior node's child-key's values are such that the pointee node's keys are all less-than-or-equal-to that value.
            // A recursive approach isn't necessary as we don't need to backtrack or otherwise re-visit internal nodes, only find the first leaf node and then linear-scan.
            // ...though using binary-search might provide some speed-up, but for now to be simple we'll use linear-scan.

            LeafCdxNode leafNode = null;

            while( node != null )
            {
                BaseCdxNode nextNode = SearchInteriorNodeAsc( index, node, keyComparison );
                if( nextNode == null ) yield break; // Not found.

                if( ( leafNode = nextNode as LeafCdxNode ) != null )
                {
                    break;
                }
                else
                {
                    node = (InteriorCdxNode)nextNode;
                }
            }

            if( leafNode != null )
            {
                IEnumerable<LeafCdxKeyEntry> searchLeafNodeResult = SearchLeafNodeAsc( index, leafNode, keyComparison );
                foreach( LeafCdxKeyEntry keyEntry in searchLeafNodeResult ) yield return keyEntry;
            }
        }

        private static BaseCdxNode SearchInteriorNodeAsc(CdxIndex index, InteriorCdxNode node, Func<Byte[],Int32> keyComparison)
        {
            foreach( InteriorIndexKeyEntry key in node.KeyEntries )
            {
                Int32 cmp = keyComparison( key.KeyBytes );
                if( cmp < 0 )
                {
                    // If a key's value is less than the targetKey, then we can dismiss it, as all of its children will also be less-than-or-equal-to the target.
                }
                else
                {
                    // If a key is exactly equal to the targetKey, then if it's a unique index then we can simply return the associated DbfRecordNumber and we're done.
                    // Otherwise, it means we need to go inside (but for simplicity-of-code, we'll always go down to the Leaf-level and return all matches anyway).

                    // If a key is greater than the target, and it's the first time it's been encountered, then we need to go inside, as its children might be matches (as they're all less-than-or-equal the value).

                    // But after seeing the first value, we can return immediately because the next key will have values greater than (and equal to?) the key's value.
                    // *If* the CDX is such that the node does have children equal to targetKey then they would be retrieved during the linear-scan of the Leaf Node by going for the Right-sibling node.

                    BaseCdxNode nextNode = index.ReadNode( key.NodePointer );
                    return nextNode;
                }
            }

            return null; // No nodes matched the key - the key probably doesn't exist.
        }

        private static IEnumerable<LeafCdxKeyEntry> SearchLeafNodeAsc(CdxIndex index, LeafCdxNode node, Func<Byte[],Int32> keyComparison)
        {
            Int32 lastKeyIdx = -1;
            IReadOnlyList<LeafCdxKeyEntry> keys = node.IndexKeys;
            for( Int32 i = 0; i < keys.Count; i++ )
            {
                LeafCdxKeyEntry keyEntry = keys[i];

                Int32 cmp = keyComparison( keyEntry.KeyBytes );
                if( cmp < 0 )
                {
                    // NOOP.
                }
                else if( cmp == 0 )
                {
                    yield return keyEntry;
                    lastKeyIdx = i;
                }
                else // cmp > 0 )
                {
                    // No possibility of any more matches, break.
                    break;
                }
            }

            if( lastKeyIdx == keys.Count - 1 && node.RightSibling != BaseCdxNode.NoSibling )
            {
                // Continue search on the next sibling node.
                LeafCdxNode rightSibling = (LeafCdxNode)index.ReadNode( node.RightSibling );

                IEnumerable<LeafCdxKeyEntry> rightSiblingResults = SearchLeafNodeAsc( index, rightSibling, keyComparison );
                foreach( LeafCdxKeyEntry keyEntry in rightSiblingResults ) yield return keyEntry;
            }
        }

        #endregion

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
}
