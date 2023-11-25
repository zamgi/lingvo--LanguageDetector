using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

using lingvo.core;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MModelBinaryNative : MModelNativeTextMMFBase, IMModel
    {
#if DEBUG
        /// <summary>
        /// 
        /// </summary>
        public static class FuskingTraitor
        {
            public static Dictionary< IntPtr, IntPtr > GetDictionary( MModelBinaryNative model ) => model._Dictionary;
        } 
#endif
        /// <summary>
        /// 
        /// </summary>
        unsafe public struct WeighByLanguageNative
        {
            public int CountBuckets;
            public WeighByLanguage* WeighByLanguagesBasePtr;

            public static WeighByLanguageNative Create( IntPtr weighByLanguagesBasePtr )
            {
                var pairsBytePtr = (byte*) weighByLanguagesBasePtr;
                //var countBuckets = *pairsBytePtr++;
                //var weighByLanguagesPtr = (WeighByLanguage*) pairsBytePtr;

                var weighByLanguageNative = new WeighByLanguageNative() 
                {
                    CountBuckets            = *pairsBytePtr++,
                    WeighByLanguagesBasePtr = (WeighByLanguage*) pairsBytePtr,
                };
                return (weighByLanguageNative);
            }
#if DEBUG
            public override string ToString()
            {
                if ( (CountBuckets != 0) && (WeighByLanguagesBasePtr != null) )
                {
                    var sb = new System.Text.StringBuilder( CountBuckets * 25 );
                    for ( var i = 0; i < CountBuckets; i++ )
                    {
                        var ptr = &WeighByLanguagesBasePtr[ i ];
                        if ( i != 0 )
                        {
                            sb.Append( ", " );
                        }
                        sb.Append( ptr->Language ).Append( ':' ).Append( ptr->Weight );
                    }
                    return (sb.ToString());
                }
                return ("EMPTY");
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        unsafe private struct WeighByLanguageEnumerator : IEnumerator< WeighByLanguage >, IEnumerable< WeighByLanguage >
        {
            private WeighByLanguage* _WeighByLanguagesPtr;
            private WeighByLanguage _Current;
            private byte _CountBuckets;

            public WeighByLanguageEnumerator( IntPtr weighByLanguagesBasePtr )
            {                
                var pairsBytePtr = (byte*) weighByLanguagesBasePtr;
                _CountBuckets    = *pairsBytePtr++;
                _WeighByLanguagesPtr = (WeighByLanguage*) pairsBytePtr;
                _Current = default;
            }

            public WeighByLanguage Current => _Current;
            public bool MoveNext()
            {
                if ( 0 < _CountBuckets )
                {
                    _CountBuckets--;
                    var ptr = &_WeighByLanguagesPtr[ _CountBuckets ];
                    _Current = new WeighByLanguage()
                    {
                        Language = ptr->Language,
                        Weight   = ptr->Weight,
                    };                    
                    return (true);
                }
                return (false);
            }

            public void Dispose() { }

            object IEnumerator.Current => _Current;
            public void Reset() => throw (new NotSupportedException());
            WeighByLanguageEnumerator GetEnumerator() => this;
            IEnumerator< WeighByLanguage > IEnumerable< WeighByLanguage >.GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        #region [.private field's.]
        private Dictionary< IntPtr, IntPtr > _Dictionary;
        private NativeMemAllocationMediator _NativeMemAllocator;
        #endregion        

        #region [.ctor().]
        public MModelBinaryNative( MModelBinaryNativeConfig config )
        {
            _NativeMemAllocator = new NativeMemAllocationMediator( nativeBlockAllocSize: 1024 * 1024 * 10 );
            _Dictionary = LoadBinaryModel( config, _NativeMemAllocator );
        }
        ~MModelBinaryNative() => DisposeNativeResources();
        public void Dispose()
        {
            DisposeNativeResources();
            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources() => _NativeMemAllocator.Dispose();
        #endregion

        #region [.model-dictionary loading.]
        private static Dictionary< IntPtr, IntPtr > LoadBinaryModel( MModelBinaryNativeConfig config, NativeMemAllocationMediator nativeMemAllocator )
        {
            var dict = new Dictionary< IntPtr, IntPtr >( config.ModelDictionaryCapacity, IntPtrEqualityComparer.Inst );
            foreach ( var modelFilename in config.ModelFilenames )
            {
                LoadFromBinFile( modelFilename, dict, nativeMemAllocator );
            }
            return (dict);
        }
        unsafe private static void LoadFromBinFile( string modelFilename, Dictionary< IntPtr, IntPtr > dict, NativeMemAllocationMediator nativeMemAllocator )
        {
            const int BUFFER_SIZE = 0x2000;

            using ( var fs = new FileStream( modelFilename, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan ) )
#if NETSTANDARD || NETCOREAPP
            using ( var mmf = MemoryMappedFile.CreateFromFile( fs, null, 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, true ) )
#else
            using ( var mmf = MemoryMappedFile.CreateFromFile( fs, null, 0L, MemoryMappedFileAccess.Read, new MemoryMappedFileSecurity(), HandleInheritability.None, true ) )
#endif            
            using ( var accessor = mmf.CreateViewAccessor( 0L, 0L, MemoryMappedFileAccess.Read ) )
            {
                byte* buffer = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer( ref buffer );

                IntPtr textPtr;
                for ( byte* endBuffer = buffer + fs.Length; buffer < endBuffer; )
                {
                    #region [.read 'textPtr' as C#-chars (with double-byte-zero '\0').]
                    var bufferCharPtr = (char*) buffer;
                    for ( var idx = 0; ; idx++ )
                    {
                        if ( BUFFER_SIZE < idx )
                        {
                            throw (new InvalidDataException( "WTF?!?!: [BUFFER_SIZE < idx]" ));
                        }
                        if ( bufferCharPtr[ idx ] == '\0' )
                        {
                            textPtr = nativeMemAllocator.AllocAndCopy( bufferCharPtr, idx );
                            buffer  = (byte*) (bufferCharPtr + idx + 1);
                            break;
                        }
                    }
                    #endregion

                    #region [.read buckets.]
                    var countBuckets = *buffer++;
                    var pairsPtr = nativeMemAllocator.Alloc( sizeof(byte) + countBuckets * sizeof(WeighByLanguage) );
                    var pairsBytePtr = (byte*) pairsPtr;
                    *pairsBytePtr++ = countBuckets;
                    var pairsLanguageWeightPtr = (WeighByLanguage*) pairsBytePtr;
                    for ( var i = 0; i < countBuckets; i++ )
                    {
                        var ptr = &pairsLanguageWeightPtr[ i ];
                        ptr->Language = (Language) (*buffer++);
                        ptr->Weight   = *((float*) buffer);
                        //pairsLanguageWeightPtr[ i ] = new WeighByLanguage()
                        //{
                        //  Language = (Language) (*buffer++),
                        //  Weight = *((float*) buffer),
                        //};
                        buffer += sizeof(float);
                    }
                    #endregion

                    dict.Add( textPtr, pairsPtr );
                }
            }
        }
        #endregion

        #region [.IModel.]
        unsafe private static WeighByLanguage[] ToArrayOfWeighByLanguage( IntPtr weighByLanguagesBasePtr )
        {
            var pairsBytePtr = (byte*) weighByLanguagesBasePtr;
            var countBuckets = *pairsBytePtr++;
            var weighByLanguagesPtr = (WeighByLanguage*) pairsBytePtr;
            var array = new WeighByLanguage[ countBuckets ];
            for ( var i = 0; i < countBuckets; i++ )
            {
                var ptr = &weighByLanguagesPtr[ i ];
                array[ i ] = new WeighByLanguage()
                {
                    Language = ptr->Language,
                    Weight   = ptr->Weight,
                };
            }
            return (array);
        }
        
        unsafe public bool TryGetValue( string ngram, out WeighByLanguageNative weighByLanguageNative )
        {
            fixed ( char* ngramPtr = ngram )
            {
                if ( _Dictionary.TryGetValue( (IntPtr) ngramPtr, out var weighByLanguagesBasePtr ) )
                {                    
                    weighByLanguageNative = WeighByLanguageNative.Create( weighByLanguagesBasePtr );
                    return (true);
                }
            }
            weighByLanguageNative = default;
            return (false);
        }
        public int RecordCount => _Dictionary.Count;
        unsafe public bool TryGetValue( string ngram, out IEnumerable< WeighByLanguage > weighByLanguages )
        {
            fixed ( char* ngramPtr = ngram )
            {
                if ( _Dictionary.TryGetValue( (IntPtr) ngramPtr, out var weighByLanguagesBasePtr ) )
                {
                    weighByLanguages = new WeighByLanguageEnumerator( weighByLanguagesBasePtr );
                    return (true);
                }
            }
            weighByLanguages = null;
            return (false);
        }
        public IEnumerable< MModelRecord > GetAllRecords()
        {
            //return (_Dictionary.GetAllModelRecords());
            
            foreach ( var p in _Dictionary )
            {
                yield return (new MModelRecord() { Ngram            = StringsHelper.ToString( p.Key ), 
                                                  WeighByLanguages = ToArrayOfWeighByLanguage( p.Value ) }); //new WeighByLanguageEnumerator( p.Value ) }); //
            }
        }
        #endregion
    }
}
