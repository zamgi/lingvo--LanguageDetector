// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  Dictionary
** 
** <OWNER>Microsoft</OWNER>
**
** Purpose: Generic hash table implementation
**
** #DictionaryVersusHashtableThreadSafety
** Hashtable has multiple reader/single writer (MR/SW) thread safety built into 
** certain methods and properties, whereas Dictionary doesn't. If you're 
** converting framework code that formerly used Hashtable to Dictionary, it's
** important to consider whether callers may have taken a dependence on MR/SW
** thread safety. If a reader writer lock is available, then that may be used
** with a Dictionary to get the same thread safety guarantee. 
** 
** Reader writer locks don't exist in silverlight, so we do the following as a
** result of removing non-generic collections from silverlight: 
** 1. If the Hashtable was fully synchronized, then we replace it with a 
**    Dictionary with full locks around reads/writes (same thread safety
**    guarantee).
** 2. Otherwise, the Hashtable has the default MR/SW thread safety behavior, 
**    so we do one of the following on a case-by-case basis:
**    a. If the ---- can be addressed by rearranging the code and using a temp
**       variable (for example, it's only populated immediately after created)
**       then we address the ---- this way and use Dictionary.
**    b. If there's concern about degrading performance with the increased 
**       locking, we ifdef with FEATURE_NONGENERIC_COLLECTIONS so we can at 
**       least use Hashtable in the desktop build, but Dictionary with full 
**       locks in silverlight builds. Note that this is heavier locking than 
**       MR/SW, but this is the only option without rewriting (or adding back)
**       the reader writer lock. 
**    c. If there's no performance concern (e.g. debug-only code) we 
**       consistently replace Hashtable with Dictionary plus full locks to 
**       reduce complexity.
**    d. Most of serialization is dead code in silverlight. Instead of updating
**       those Hashtable occurences in serialization, we carved out references 
**       to serialization such that this code doesn't need to build in 
**       silverlight. 
===========================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;
using lingvo.core;

namespace lingvo.ld.MultiLanguage
{    
    /// <summary>
    /// 
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class DictionaryNative : IDictionary< IntPtr, BucketValue >, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private struct Entry
        {
            public int    hashCode;    // Lower 31 bits of hash code, -1 if unused
            public int    next;        // Index of next entry, -1 if last
            public IntPtr   key;         // Key of entry
            public BucketValue value;       // Value of entry
        }

        private int[] _Buckets;
        private Entry[] _Entries;
        private int _Count;
        private int _FreeList;
        private int _FreeCount;
        private KeyCollection _Keys;
        private ValueCollection _Values;
        private IntPtrEqualityComparer _Comparer;        

        public DictionaryNative( int capacity )
        {
            if ( capacity < 0 ) throw (new ArgumentOutOfRangeException( "capacity" ));
            Capacity = capacity;
            Initialize( capacity );
        }

        public int Count
        {
            get { return (_Count - _FreeCount); }
        }
        public int Capacity
        {
            get;
            private set;
        }

        public KeyCollection Keys
        {
            get
            {
                Contract.Ensures( Contract.Result<KeyCollection>() != null );
                if ( _Keys == null ) _Keys = new KeyCollection( this );
                return _Keys;
            }
        }
        ICollection< IntPtr > IDictionary< IntPtr, BucketValue >.Keys
        {
            get
            {
                if ( _Keys == null ) _Keys = new KeyCollection( this );
                return _Keys;
            }
        }

        public ValueCollection Values
        {
            get
            {
                Contract.Ensures( Contract.Result<ValueCollection>() != null );
                if ( _Values == null ) _Values = new ValueCollection( this );
                return (_Values);
            }
        }
        ICollection< BucketValue > IDictionary< IntPtr, BucketValue >.Values
        {
            get
            {
                if ( _Values == null ) _Values = new ValueCollection( this );
                return (_Values);
            }
        }

        public BucketValue this[ IntPtr key ]
        {
            get
            {
                int i = FindEntry( key );
                if ( i >= 0 )
                {
                    return (_Entries[ i ].value);
                }

                throw (new KeyNotFoundException());
            }
            set
            {
                Insert( key, value, false );
            }
        }

        public void Add( IntPtr key, BucketValue value )
        {
            Insert( key, value, true );
        }
        public void Clear()
        {
            if ( _Count > 0 )
            {
                for ( int i = 0; i < _Buckets.Length; i++ )
                {
                    _Buckets[ i ] = -1;
                }
                Array.Clear( _Entries, 0, _Count );
                _FreeList = -1;
                _Count = 0;
                _FreeCount = 0;
            }
        }
        public bool ContainsKey( IntPtr key )
        {
            return (FindEntry( key ) >= 0);
        }
        public bool Remove( IntPtr key )
        {
            int hashCode = _Comparer.GetHashCode( key ) & 0x7FFFFFFF;
            int bucket = hashCode % _Buckets.Length;
            int last = -1;
            for ( int i = _Buckets[ bucket ]; i >= 0; last = i, i = _Entries[ i ].next )
            {
                if ( _Entries[ i ].hashCode == hashCode && _Comparer.Equals( _Entries[ i ].key, key ) )
                {
                    if ( last < 0 )
                    {
                        _Buckets[ bucket ] = _Entries[ i ].next;
                    }
                    else
                    {
                        _Entries[ last ].next = _Entries[ i ].next;
                    }
                    _Entries[ i ].hashCode = -1;
                    _Entries[ i ].next     = _FreeList;
                    _Entries[ i ].key      = default(IntPtr);
                    _Entries[ i ].value    = default(BucketValue);
                    _FreeList = i;
                    _FreeCount++;
                    return (true);
                }
            }
            return (false);
        }
        public bool TryGetValue( IntPtr key, out BucketValue value )
        {
            int i = FindEntry( key );
            if ( i >= 0 )
            {
                value = _Entries[ i ].value;
                return (true);
            }
            value = default(BucketValue);
            return (false);
        }

        //========================== EXTENSION ==========================//
        private static void AddToHead( ref BucketValue bucketVal, ref MModelNativeTextMMFBase.Pair pair )
        {
            bucketVal.NextBucket = new BucketRef() 
            { 
                Language   = pair.Language, 
                Weight     = pair.Weight,
                NextBucket = bucketVal.NextBucket,
            };
        }
        /*public bool TryAddToEndOfChain( ref LanguageNativeTextMMFConfig.Pair pair )
        {
            int i = FindEntry( pair.TextPtr );
            if ( i >= 0 )
            {
                AddToHead( ref _Entries[ i ].value, ref pair );
                return (true);
            }
            return (false);
        }*/
        unsafe internal void AddNewOrToEndOfChain( ref MModelNativeTextMMFBase.Pair pair )
        {
            int hashCode     = _Comparer.GetHashCode( pair.TextPtr ) & 0x7FFFFFFF;
            int targetBucket = hashCode % _Buckets.Length;

            #region [.try merge with exists.]
		    for ( int i = _Buckets[ targetBucket ]; i >= 0; i = _Entries[ i ].next )
            {
                if ( _Entries[ i ].hashCode == hashCode && _Comparer.Equals( _Entries[ i ].key, pair.TextPtr ) )
                {
                    //add to end of chain
                    AddToHead( ref _Entries[ i ].value, ref pair );

                    return;
                }
            } 
	        #endregion

            #region [.add new.]
            int index;
            if ( _FreeCount > 0 )
            {
                index = _FreeList;
                _FreeList = _Entries[ index ].next;
                _FreeCount--;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    targetBucket = hashCode % _Buckets.Length;
                }
                index = _Count;
                _Count++;
            }

            var textPtr = StringsHelper.AllocHGlobalAndCopy( pair.TextPtr, pair.TextLength );

            _Entries[ index ].hashCode = hashCode;
            _Entries[ index ].next     = _Buckets[ targetBucket ];
            _Entries[ index ].key      = textPtr;
            _Entries[ index ].value    = new BucketValue( pair.Language, pair.Weight );
            _Buckets[ targetBucket ]   = index; 
	        #endregion
        }

        private void AddNewOrMergeWithExists( ref Entry otherEntry )
        {
            int hashCode     = _Comparer.GetHashCode( otherEntry.key ) & 0x7FFFFFFF;
            int targetBucket = hashCode % _Buckets.Length;

            #region [.try merge with exists.]
		    for ( int i = _Buckets[ targetBucket ]; i >= 0; i = _Entries[ i ].next )
            {
                if ( _Entries[ i ].hashCode == hashCode && _Comparer.Equals( _Entries[ i ].key, otherEntry.key ) )
                {
                    //merge with exists
                    var bucketRef = new BucketRef()
                    {
                        Language   = otherEntry.value.Language,
                        Weight     = otherEntry.value.Weight,
                        NextBucket = otherEntry.value.NextBucket,
                    };
                    var next = _Entries[ i ].value.NextBucket;
                    if ( next == null )
                    {
                        _Entries[ i ].value.NextBucket = bucketRef;
                    }
                    else
                    {
                        for ( ; next.NextBucket != null; next = next.NextBucket ) ;
                        next.NextBucket = bucketRef;
                    }

                    return;
                }
            } 
	        #endregion

            #region [.add new.]
            int index;
            if ( _FreeCount > 0 )
            {
                index = _FreeList;
                _FreeList = _Entries[ index ].next;
                _FreeCount--;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    targetBucket = hashCode % _Buckets.Length;
                }
                index = _Count;
                _Count++;
            }

            _Entries[ index ].hashCode = hashCode;
            _Entries[ index ].next     = _Buckets[ targetBucket ];
            _Entries[ index ].key      = otherEntry.key;
            _Entries[ index ].value    = otherEntry.value;
            _Buckets[ targetBucket ]   = index; 
	        #endregion
        }
        public void MergeWith( DictionaryNative dict )
        {
            for ( int index = 0, len = dict._Count; index < len; index++ )
            {
                if ( 0 <= dict._Entries[ index ].hashCode )
                {
                    AddNewOrMergeWithExists( ref dict._Entries[ index ] );
                }
            }
        }

        public void Dispose()
        {
            _Buckets   = null;
            _Entries   = null;
            _FreeList  = -1;
            _Count     = 0;
            _FreeCount = 0;
            _Keys      = null;
            _Values    = null;
        }
        //===============================================================//

        private void Initialize( int capacity )
        {
            int size = HashHelpers.GetPrime( capacity );
            _Buckets = new int[ size ];
            for ( int i = 0; i < _Buckets.Length; i++ )
            {
                _Buckets[ i ] = -1;
            }
            _Entries  = new Entry[ size ];
            _FreeList = -1;
        }
        private int FindEntry( IntPtr key )
        {
            int hashCode = _Comparer.GetHashCode( key ) & 0x7FFFFFFF;
            for ( int i = _Buckets[ hashCode % _Buckets.Length ]; i >= 0; i = _Entries[ i ].next )
            {
                if ( _Entries[ i ].hashCode == hashCode && _Comparer.Equals( _Entries[ i ].key, key ) )
                {
                    return (i);
                }
            }
            return (-1);
        }
        private void Insert( IntPtr key, BucketValue value, bool add )
        {
            int hashCode     = _Comparer.GetHashCode( key ) & 0x7FFFFFFF;
            int targetBucket = hashCode % _Buckets.Length;
            for ( int i = _Buckets[ targetBucket ]; i >= 0; i = _Entries[ i ].next )
            {
                if ( _Entries[ i ].hashCode == hashCode && _Comparer.Equals( _Entries[ i ].key, key ) )
                {
                    if ( add )
                    {
                        throw (new ArgumentException( "AddingDuplicate" ));
                    }
                    _Entries[ i ].value = value;
                    return;
                }
            }

            int index;
            if ( _FreeCount > 0 )
            {
                index = _FreeList;
                _FreeList = _Entries[ index ].next;
                _FreeCount--;
            }
            else
            {
                if ( _Count == _Entries.Length )
                {
                    Resize();
                    targetBucket = hashCode % _Buckets.Length;
                }
                index = _Count;
                _Count++;
            }

            _Entries[ index ].hashCode = hashCode;
            _Entries[ index ].next     = _Buckets[ targetBucket ];
            _Entries[ index ].key      = key;
            _Entries[ index ].value    = value;
            _Buckets[ targetBucket ]   = index;
        }

        private void Resize()
        {
            Resize( HashHelpers.ExpandPrime( _Count ), false );
        }
        private void Resize( int newSize, bool forceNewHashCodes )
        {
            Contract.Assert( newSize >= _Entries.Length );
            int[] newBuckets = new int[ newSize ];
            for ( int i = 0; i < newBuckets.Length; i++ )
            {
                newBuckets[ i ] = -1;
            }
            var newEntries = new Entry[ newSize ];
            Array.Copy( _Entries, 0, newEntries, 0, _Count );
            if ( forceNewHashCodes )
            {
                for ( int i = 0; i < _Count; i++ )
                {
                    if ( newEntries[ i ].hashCode != -1 )
                    {
                        newEntries[ i ].hashCode = (_Comparer.GetHashCode( newEntries[ i ].key ) & 0x7FFFFFFF);
                    }
                }
            }
            for ( int i = 0; i < _Count; i++ )
            {
                if ( newEntries[ i ].hashCode >= 0 )
                {
                    int bucket = newEntries[ i ].hashCode % newSize;
                    newEntries[ i ].next = newBuckets[ bucket ];
                    newBuckets[ bucket ] = i;
                }
            }
            _Buckets = newBuckets;
            _Entries = newEntries;
        }

        public Enumerator GetEnumerator()
        {
            return (new Enumerator( this, Enumerator.KeyValuePair ));
        }
        IEnumerator< KeyValuePair< IntPtr, BucketValue > > IEnumerable< KeyValuePair< IntPtr, BucketValue > >.GetEnumerator()
        {
            return (new Enumerator( this, Enumerator.KeyValuePair ));
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (new Enumerator( this, Enumerator.KeyValuePair ));
        }

        void ICollection< KeyValuePair< IntPtr, BucketValue > >.Add( KeyValuePair< IntPtr, BucketValue > keyValuePair )
        {
            Add( keyValuePair.Key, keyValuePair.Value );
        }
        bool ICollection< KeyValuePair< IntPtr, BucketValue > >.Contains( KeyValuePair< IntPtr, BucketValue > keyValuePair )
        {
            int i = FindEntry( keyValuePair.Key );
            if ( i >= 0 && EqualityComparer< BucketValue >.Default.Equals( _Entries[ i ].value, keyValuePair.Value ) )
            {
                return (true);
            }
            return (false);
        }
        bool ICollection< KeyValuePair< IntPtr, BucketValue > >.Remove( KeyValuePair< IntPtr, BucketValue > keyValuePair )
        {
            int i = FindEntry( keyValuePair.Key );
            if ( i >= 0 && EqualityComparer< BucketValue >.Default.Equals( _Entries[ i ].value, keyValuePair.Value ) )
            {
                Remove( keyValuePair.Key );
                return (true);
            }
            return (false);
        }
        bool ICollection< KeyValuePair< IntPtr, BucketValue > >.IsReadOnly
        {
            get { return (false); }
        }
        void ICollection< KeyValuePair< IntPtr, BucketValue > >.CopyTo( KeyValuePair< IntPtr, BucketValue >[] array, int index )
        {
            if ( array == null )
            {
                throw (new ArgumentNullException( "array" ));
            }

            if ( index < 0 || index > array.Length )
            {
                throw (new ArgumentOutOfRangeException( "index" ));
            }

            if ( array.Length - index < Count )
            {
                throw (new ArgumentException( "ArrayPlusOffTooSmall" ));
            }

            int count = this._Count;
            Entry[] entries = this._Entries;
            for ( int i = 0; i < count; i++ )
            {
                if ( entries[ i ].hashCode >= 0 )
                {
                    array[ index++ ] = new KeyValuePair<IntPtr, BucketValue>( entries[ i ].key, entries[ i ].value );
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public struct Enumerator : IEnumerator< KeyValuePair< IntPtr, BucketValue > >, IDictionaryEnumerator
        {
            internal const int DictEntry    = 1;
            internal const int KeyValuePair = 2;

            private DictionaryNative _Dictionary;
            private int _Index;
            private KeyValuePair< IntPtr, BucketValue > _Current;
            private int _GetEnumeratorRetType;  // What should Enumerator.Current return?

            internal Enumerator( DictionaryNative dictionary, int getEnumeratorRetType )
            {
                _Dictionary = dictionary;
                _Index      = 0;
                _GetEnumeratorRetType = getEnumeratorRetType;
                _Current = new KeyValuePair< IntPtr, BucketValue >();
            }
            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ( (uint) _Index < (uint) _Dictionary._Count )
                {
                    if ( _Dictionary._Entries[ _Index ].hashCode >= 0 )
                    {
                        _Current = new KeyValuePair< IntPtr, BucketValue >( _Dictionary._Entries[ _Index ].key, _Dictionary._Entries[ _Index ].value );
                        _Index++;
                        return (true);
                    }
                    _Index++;
                }

                _Index = _Dictionary._Count + 1;
                _Current = new KeyValuePair< IntPtr, BucketValue >();
                return (false);
            }
            public KeyValuePair< IntPtr, BucketValue > Current
            {
                get { return (_Current); }
            }

            object IEnumerator.Current
            {
                get
                {
                    if ( _Index == 0 || (_Index == _Dictionary._Count + 1) )
                    {
                        throw (new InvalidOperationException());
                    }

                    if ( _GetEnumeratorRetType == DictEntry )
                    {
                        return (new DictionaryEntry( _Current.Key, _Current.Value ));
                    }
                    else
                    {
                        return (new KeyValuePair< IntPtr, BucketValue >( _Current.Key, _Current.Value ));
                    }
                }
            }
            void IEnumerator.Reset()
            {
                _Index   = 0;
                _Current = new KeyValuePair< IntPtr, BucketValue >();
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if ( _Index == 0 || (_Index == _Dictionary._Count + 1) )
                    {
                        throw (new InvalidOperationException());
                    }

                    return (new DictionaryEntry( _Current.Key, _Current.Value ));
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if ( _Index == 0 || (_Index == _Dictionary._Count + 1) )
                    {
                        throw (new InvalidOperationException());
                    }

                    return (_Current.Key);
                }
            }
            object IDictionaryEnumerator.Value
            {
                get
                {
                    if ( _Index == 0 || (_Index == _Dictionary._Count + 1) )
                    {
                        throw (new InvalidOperationException());
                    }

                    return (_Current.Value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class KeyCollection : ICollection< IntPtr >
        {
            private DictionaryNative _Dictionary;

            public KeyCollection( DictionaryNative dictionary )
            {
                if ( dictionary == null )
                {
                    throw (new ArgumentNullException( "dictionary" ));
                }
                this._Dictionary = dictionary;
            }

            public Enumerator GetEnumerator()
            {
                return (new Enumerator( _Dictionary ));
            }
            public void CopyTo( IntPtr[] array, int index )
            {
                if ( array == null )
                {
                    throw (new ArgumentNullException( "array" ));
                }

                if ( index < 0 || index > array.Length )
                {
                    throw (new ArgumentOutOfRangeException( "index" ));
                }

                if ( array.Length - index < _Dictionary.Count )
                {
                    throw (new ArgumentException( "ArrayPlusOffTooSmall" ));
                }

                int count = _Dictionary._Count;
                Entry[] entries = _Dictionary._Entries;
                for ( int i = 0; i < count; i++ )
                {
                    if ( entries[ i ].hashCode >= 0 ) array[ index++ ] = entries[ i ].key;
                }
            }
            public int Count
            {
                get { return (_Dictionary.Count); }
            }

            bool ICollection< IntPtr >.IsReadOnly
            {
                get { return (true); }
            }
            void ICollection< IntPtr >.Add( IntPtr item )
            {
                throw (new NotSupportedException());
            }
            void ICollection< IntPtr >.Clear()
            {
                throw (new NotSupportedException());
            }
            bool ICollection< IntPtr >.Contains( IntPtr item )
            {
                return (_Dictionary.ContainsKey( item ));
            }
            bool ICollection< IntPtr >.Remove( IntPtr item )
            {
                throw (new NotSupportedException());
            }

            IEnumerator< IntPtr > IEnumerable< IntPtr >.GetEnumerator()
            {
                return (new Enumerator( _Dictionary ));
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return (new Enumerator( _Dictionary ));
            }

            [Serializable]
            public struct Enumerator : IEnumerator< IntPtr >
            {
                private DictionaryNative _Dictionary;
                private int _Index;
                private IntPtr _CurrentKey;

                internal Enumerator( DictionaryNative dictionary )
                {
                    _Dictionary = dictionary;
                    _Index = 0;
                    _CurrentKey = default(IntPtr);
                }
                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    while ( (uint) _Index < (uint) _Dictionary._Count )
                    {
                        if ( _Dictionary._Entries[ _Index ].hashCode >= 0 )
                        {
                            _CurrentKey = _Dictionary._Entries[ _Index ].key;
                            _Index++;
                            return (true);
                        }
                        _Index++;
                    }

                    _Index = _Dictionary._Count + 1;
                    _CurrentKey = default(IntPtr);
                    return (false);
                }
                public IntPtr Current
                {
                    get { return (_CurrentKey); }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        if ( _Index == 0 || (_Index == _Dictionary._Count + 1) )
                        {
                            throw (new InvalidOperationException());
                        }

                        return (_CurrentKey);
                    }
                }
                void IEnumerator.Reset()
                {
                    _Index = 0;
                    _CurrentKey = default(IntPtr);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class ValueCollection : ICollection< BucketValue >
        {
            private DictionaryNative _Dictionary;

            public ValueCollection( DictionaryNative dictionary )
            {
                if ( dictionary == null )
                {
                    throw (new ArgumentNullException( "dictionary" ));
                }
                this._Dictionary = dictionary;
            }

            public Enumerator GetEnumerator()
            {
                return (new Enumerator( _Dictionary ));
            }
            public void CopyTo( BucketValue[] array, int index )
            {
                if ( array == null )
                {
                    throw (new ArgumentNullException( "array"));
                }

                if ( index < 0 || index > array.Length )
                {
                    throw (new ArgumentOutOfRangeException( "index" ));
                }

                if ( array.Length - index < _Dictionary.Count )
                {
                    throw (new ArgumentException( "ArrayPlusOffTooSmall" ));
                }

                int count = _Dictionary._Count;
                Entry[] entries = _Dictionary._Entries;
                for ( int i = 0; i < count; i++ )
                {
                    if ( entries[ i ].hashCode >= 0 )
                    {
                        array[ index++ ] = entries[ i ].value;
                    }
                }
            }
            public int Count
            {
                get { return (_Dictionary.Count); }
            }

            bool ICollection< BucketValue >.IsReadOnly
            {
                get { return (true); }
            }
            void ICollection< BucketValue >.Add( BucketValue item )
            {
                throw (new NotSupportedException());
            }
            bool ICollection< BucketValue >.Remove( BucketValue item )
            {
                throw (new NotSupportedException());
            }
            void ICollection< BucketValue >.Clear()
            {
                throw (new NotSupportedException());
            }
            bool ICollection< BucketValue >.Contains( BucketValue item )
            {
                throw (new NotImplementedException());
            }

            IEnumerator< BucketValue > IEnumerable< BucketValue >.GetEnumerator()
            {
                return (new Enumerator( _Dictionary ));
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return (new Enumerator( _Dictionary ));
            }

            [Serializable]
            public struct Enumerator : IEnumerator< BucketValue >
            {
                private DictionaryNative _Dictionary;
                private int _Index;
                private BucketValue _CurrentValue;

                internal Enumerator( DictionaryNative dictionary )
                {
                    _Dictionary = dictionary;
                    _Index = 0;
                    _CurrentValue = default(BucketValue);
                }
                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    while ( (uint) _Index < (uint) _Dictionary._Count )
                    {
                        if ( _Dictionary._Entries[ _Index ].hashCode >= 0 )
                        {
                            _CurrentValue = _Dictionary._Entries[ _Index ].value;
                            _Index++;
                            return (true);
                        }
                        _Index++;
                    }
                    _Index = _Dictionary._Count + 1;
                    _CurrentValue = default(BucketValue);
                    return (false);
                }
                public BucketValue Current
                {
                    get { return (_CurrentValue); }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        if ( _Index == 0 || (_Index == _Dictionary._Count + 1) )
                        {
                            throw (new InvalidOperationException());
                        }

                        return (_CurrentValue);
                    }
                }
                void IEnumerator.Reset()
                {
                    _Index = 0;
                    _CurrentValue = default(BucketValue);
                }
            }
        }
    }
}

