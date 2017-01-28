using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

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
            public static Dictionary< IntPtr, IntPtr > GetDictionary( MModelBinaryNative model )
            {
                return (model._Dictionary);
            }
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
                _Current = default(WeighByLanguage);
            }

            public WeighByLanguage Current
            {
                get { return (_Current); }
            }
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

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get { return (this.Current); }
            }
            public void Reset()
            {
                throw (new NotSupportedException());
            }

            WeighByLanguageEnumerator GetEnumerator()
            {
                return (this);
            }
            IEnumerator< WeighByLanguage > IEnumerable< WeighByLanguage >.GetEnumerator()
            {
                return (this);
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return (GetEnumerator());
            }
        }


        #region [.private field's.]
        private Dictionary< IntPtr, IntPtr > _Dictionary;
        #endregion        

        #region [.ctor().]
        public MModelBinaryNative( MModelBinaryNativeConfig config )
        {
            _Dictionary = LoadBinaryModel( config );
        }
        ~MModelBinaryNative()
        {
            DisposeNativeResources();
        }

        public void Dispose()
        {
            DisposeNativeResources();

            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources()
        {
            if ( _Dictionary != null )
            {
                foreach ( var p in _Dictionary )
                {
                    Marshal.FreeHGlobal( p.Key );
                    Marshal.FreeHGlobal( p.Value );
                }
                _Dictionary = null;
            }
        } 
        #endregion

        #region [.model-dictionary loading.]
        /*unsafe private static Dictionary< IntPtr, WeighByLanguage[] > LoadDictionaryNativeFromBinFile_v1( string fileName, int capacity )
        {
            var dict = new Dictionary< IntPtr, WeighByLanguage[] >( capacity );

            const int BUFFER_SIZE = 0x2000;

            using ( var fs = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan ) )
            using ( var mmf = MemoryMappedFile.CreateFromFile( fs, null, 0L, MemoryMappedFileAccess.Read, new MemoryMappedFileSecurity(), HandleInheritability.None, true ) )
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
                            textPtr = StringsHelper.AllocHGlobalAndCopy( bufferCharPtr, idx );
                            buffer  = (byte*) (bufferCharPtr + idx + 1);
                            break;
                        }
                    }
                    #endregion

                    #region [.read buckets.]
                    var countBuckets = *buffer++;
                    var pairs = new WeighByLanguage[ countBuckets ];
                    for ( var i = 0; i < countBuckets; i++ )
                    {
                        pairs[ i ] = new WeighByLanguage()
                                     {
                                         Language = (Language) (*buffer++),
                                         Weight   = *((float*) buffer),
                                     };
                        buffer += sizeof(float);
                    }
                    #endregion

                    dict.Add( textPtr, pairs );
                }
            }

            return (dict);
        }*/
        /*unsafe private static Dictionary< IntPtr, IntPtr > LoadDictionaryNativeFromBinFile_v2( string fileName, int capacity )
        {
            var dict = new Dictionary< IntPtr, IntPtr >( capacity );

            const int BUFFER_SIZE = 0x2000;

            using ( var fs = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan ) )
            using ( var mmf = MemoryMappedFile.CreateFromFile( fs, null, 0L, MemoryMappedFileAccess.Read, new MemoryMappedFileSecurity(), HandleInheritability.None, true ) )
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
                            textPtr = StringsHelper.AllocHGlobalAndCopy( bufferCharPtr, idx );
                            buffer  = (byte*) (bufferCharPtr + idx + 1);
                            break;
                        }
                    }
                    #endregion

                    #region [.read buckets.]
                    var countBuckets = *buffer++;
                    var pairsPtr = Marshal.AllocHGlobal( sizeof(byte) + countBuckets * sizeof(WeighByLanguage) );
                    var pairsBytePtr = (byte*) pairsPtr;
                    *pairsBytePtr++ = countBuckets;
                    var pairsLanguageWeightPtr = (WeighByLanguage*) pairsBytePtr;
                    for ( var i = 0; i < countBuckets; i++ )
                    {
                        var ptr = &pairsLanguageWeightPtr[ i ];
                        ptr->Language = (Language) (*buffer++);
                        ptr->Weight   = *((float*) buffer);
                        //pairsLanguageWeightPtr[ i ] = new WeighByLanguage()
                        //                {
                        //                    Language = (Language) (*buffer++),
                        //                    Weight   = *((float*) buffer),
                        //                };
                        buffer += sizeof(float);
                    }
                    #endregion

                    dict.Add( textPtr, pairsPtr );
                }
            }

            return (dict);
        }*/
        private static Dictionary< IntPtr, IntPtr > LoadBinaryModel( MModelBinaryNativeConfig config )
        {
            var dict = new Dictionary< IntPtr, IntPtr >( config.ModelDictionaryCapacity, default(IntPtrEqualityComparer) );

            foreach ( var modelFilename in config.ModelFilenames )
            {
                LoadFromBinFile( modelFilename, dict );
            }

            return (dict);
        }
        unsafe private static void LoadFromBinFile( string modelFilename, Dictionary< IntPtr, IntPtr > dict )
        {
            const int BUFFER_SIZE = 0x2000;

            using ( var fs = new FileStream( modelFilename, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan ) )
            using ( var mmf = MemoryMappedFile.CreateFromFile( fs, null, 0L, MemoryMappedFileAccess.Read, new MemoryMappedFileSecurity(), HandleInheritability.None, true ) )
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
                            textPtr = StringsHelper.AllocHGlobalAndCopy( bufferCharPtr, idx );
                            buffer  = (byte*) (bufferCharPtr + idx + 1);
                            break;
                        }
                    }
                    #endregion

                    #region [.read buckets.]
                    var countBuckets = *buffer++;
                    var pairsPtr = Marshal.AllocHGlobal( sizeof(byte) + countBuckets * sizeof(WeighByLanguage) );
                    var pairsBytePtr = (byte*) pairsPtr;
                    *pairsBytePtr++ = countBuckets;
                    var pairsLanguageWeightPtr = (WeighByLanguage*) pairsBytePtr;
                    for ( var i = 0; i < countBuckets; i++ )
                    {
                        var ptr = &pairsLanguageWeightPtr[ i ];
                        ptr->Language = (Language) (*buffer++);
                        ptr->Weight   = *((float*) buffer);
                        //pairsLanguageWeightPtr[ i ] = new WeighByLanguage()
                        //                {
                        //                    Language = (Language) (*buffer++),
                        //                    Weight   = *((float*) buffer),
                        //                };
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
                IntPtr weighByLanguagesBasePtr;
                if ( _Dictionary.TryGetValue( (IntPtr) ngramPtr, out weighByLanguagesBasePtr ) )
                {                    
                    weighByLanguageNative = WeighByLanguageNative.Create( weighByLanguagesBasePtr );
                    return (true);
                }
            }
            weighByLanguageNative = default(WeighByLanguageNative);
            return (false);
        }
        public int RecordCount
        {
            get { return (_Dictionary.Count); }
        }
        unsafe public bool TryGetValue( string ngram, out IEnumerable< WeighByLanguage > weighByLanguages )
        {
            fixed ( char* ngramPtr = ngram )
            {
                IntPtr weighByLanguagesBasePtr;
                if ( _Dictionary.TryGetValue( (IntPtr) ngramPtr, out weighByLanguagesBasePtr ) )
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
