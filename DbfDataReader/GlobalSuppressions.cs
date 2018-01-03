// Naming violations we don't care about:

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Asc", Scope = "member", Target = "Dbf.Cdx.IndexSearcher.#SearchInteriorNodeAsc(Dbf.Cdx.CdxIndex,Dbf.Cdx.InteriorCdxNode,System.Byte[],System.Collections.Generic.IComparer`1)" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Asc", Scope = "member", Target = "Dbf.Cdx.IndexSearcher.#SearchLeafNodeAsc(Dbf.Cdx.CdxIndex,Dbf.Cdx.LeafCdxNode,System.Byte[],System.Collections.Generic.IComparer`1)" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "B", Scope = "member", Target = "Dbf.DbfColumnType.#B" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dbt", Scope = "member", Target = "Dbf.MemoBlock.#DbtBlockNumber" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Dbf.AsyncDbfDataReader" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Dbf.DbfDataReader" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Dbf.SubsetSyncDbfDataReader" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Dbf.SyncDbfDataReader" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Scope = "type", Target = "Dbf.DbfVersion" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "bytes", Scope = "member", Target = "Dbf.Cdx.InteriorIndexKeyEntry.#.ctor(System.Byte[],System.UInt32,System.Int32)" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "decimal", Scope = "member", Target = "Dbf.DbfReaders.DbfNumber.#.ctor(System.Int32,System.Decimal)" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "int32", Scope = "member", Target = "Dbf.DbfReaders.DbfNumber.#.ctor(System.Int32,System.Decimal)" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "pointer", Scope = "member", Target = "Dbf.Cdx.InteriorIndexKeyEntry.#.ctor(System.Byte[],System.UInt32,System.Int32)" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "Dbf.DbfRecord.#Values" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags", Scope = "member", Target = "Dbf.DbfActualColumnType.#NullFlags" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags", Scope = "member", Target = "Dbf.DbfColumnType.#NullFlags" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "LeafCdxNode", Scope = "member", Target = "Dbf.Cdx.LeafCdxNode.#Read(Dbf.Cdx.CdxIndexHeader,System.Int64,Dbf.Cdx.CdxNodeAttributes,System.IO.BinaryReader)" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x", Scope = "member", Target = "Dbf.SequentialByteArrayComparer.#Compare(System.Byte[],System.Byte[])" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y", Scope = "member", Target = "Dbf.SequentialByteArrayComparer.#Compare(System.Byte[],System.Byte[])" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x", Scope = "member", Target = "Dbf.SequentialByteArrayComparer.#CompareWithoutChecks(System.Byte[],System.Byte[])" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y", Scope = "member", Target = "Dbf.SequentialByteArrayComparer.#CompareWithoutChecks(System.Byte[],System.Byte[])" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x", Scope = "member", Target = "Dbf.SequentialByteArrayComparer.#CompareWithoutChecks(System.Byte[],System.Byte[],System.Int32,System.Int32,System.Int32)" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y", Scope = "member", Target = "Dbf.SequentialByteArrayComparer.#CompareWithoutChecks(System.Byte[],System.Byte[],System.Int32,System.Int32,System.Int32)" )]

// Actual suppressions for good reasons:

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "Dbf.DbfColumnType" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Dbf.DbfTable.#ColumnsByName" )]

// FxCop bugs:

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Scope = "member", Target = "Dbf.DbfTable.#Open(System.String,System.Text.Encoding)" )]

// FxCop bug: DataReaders are not collections:
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Scope = "type", Target = "Dbf.DbfDataReader" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Scope = "type", Target = "Dbf.SubsetSyncDbfDataReader" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Scope = "type", Target = "Dbf.SyncDbfDataReader" )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Scope = "type", Target = "Dbf.AsyncDbfDataReader" )]


//

