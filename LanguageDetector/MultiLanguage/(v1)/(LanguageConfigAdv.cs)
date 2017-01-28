using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

using lingvo.core;

namespace lingvo.ld.v1
{
    /// <summary>
    /// 
    /// </summary>
    unsafe public sealed class LanguageConfigAdv : LanguageConfig
    {
        /// <summary>
        /// 
        /// </summary>
        internal struct Pair_v1
        {
            public string   Text;
            public float    Weight;
            public Language Language;
#if DEBUG
            public override string ToString()
            {
                return (Text + ", " + Weight + ", " + Language);
            }  
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        internal struct Pair_v2
        {
            public IntPtr   TextPtr;
            public int      TextLength;
            public float    Weight;
            public Language Language;
#if DEBUG
            public override string ToString()
            {
                return (StringsHelper.ToString( TextPtr ) + ", " + Weight + ", " + Language);
            }  
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        internal delegate void LoadModelFilenameContentMMFCallback_v1( ref Pair_v1 pair );
        /// <summary>
        /// 
        /// </summary>
        internal delegate void LoadModelFilenameContentMMFCallback_v2( ref Pair_v2 pair );

        /// <summary>
        /// 
        /// </summary>
        private struct NativeString
        {
            public static NativeString EMPTY = new NativeString() { Length = -1, Start = null };

            public char* Start;
            public int   Length;
#if DEBUG
            public override string ToString()
            {
                return (StringsHelper.ToString( Start, Length ));
            } 
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class EnumeratorMMF : IEnumerator< NativeString >
        {
            private const int  BUFFER_SIZE = 0x400;
            private const byte NEW_LINE    = (byte) '\n';

            private char[]   _CharBuffer;
            private int      _CharBufferLength;
            private char*    _CharBufferBase;
            private GCHandle _CharBufferGCHandle;
            private Encoding _Encoding;

            private FileStream               _FS;
            private MemoryMappedFile         _MMF;
            private MemoryMappedViewAccessor _Accessor;
            private byte* _Buffer;
            private byte* _EndBuffer;
            private NativeString _NativeString;

            private EnumeratorMMF( string fileName )
            {                    
                _Encoding           = Encoding.UTF8;
                _CharBufferLength   = BUFFER_SIZE;
                _CharBuffer         = new char[ _CharBufferLength ];
                _CharBufferGCHandle = GCHandle.Alloc( _CharBuffer, GCHandleType.Pinned );
                _CharBufferBase     = (char*) _CharBufferGCHandle.AddrOfPinnedObject().ToPointer();
                _NativeString       = NativeString.EMPTY;
                
                _FS  = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan );
                _MMF = MemoryMappedFile.CreateFromFile( _FS, null, 0L, MemoryMappedFileAccess.Read, new MemoryMappedFileSecurity(), HandleInheritability.None, true );
                _Accessor = _MMF.CreateViewAccessor( 0L, 0L, MemoryMappedFileAccess.Read );

                _Accessor.SafeMemoryMappedViewHandle.AcquirePointer( ref _Buffer );

                var length = _FS.Length;
                //try skip The UTF-8 representation of the BOM is the byte sequence [0xEF, 0xBB, 0xBF]
                if ( 3 <= length && _Buffer[ 0 ] == 0xEF && _Buffer[ 1 ] == 0xBB && _Buffer[ 2 ] == 0xBF )
                {
                    _Buffer += 3;
                    length  -= 3;
                }
                _EndBuffer = _Buffer + length;
            }
            ~EnumeratorMMF()
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
                if ( _CharBufferBase != null )
                {
                    _CharBufferGCHandle.Free();
                    _CharBufferBase = null;
                }

                if ( _Accessor != null )
                {
                    _Accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    _Accessor.Dispose();
                    _Accessor = null;
                }
                if ( _MMF != null )
                {
                    _MMF.Dispose();
                    _MMF = null;
                }
                if ( _FS != null )
                {
                    _FS.Dispose();
                    _FS = null;
                }
            }

            public NativeString Current
            {
                get { return (_NativeString); }
            }
            public bool MoveNext()
            {
                var start = _Buffer;
                for ( long currentIndex = 0, endIndex = _EndBuffer - start; currentIndex <= endIndex; currentIndex++ )
                {
                    if ( start [ currentIndex ] == NEW_LINE )
                    {
                        #region [.line.]
                        int len = (int) currentIndex;

                        _Buffer = start + currentIndex + 1; //force move forward over 'NEW_LINE' char

                        if ( 0 < len )
                        {
                            char* startChar = _CharBufferBase;

                            int realLen = _Encoding.GetChars( start, len, startChar, _CharBufferLength );

                            int startIndex  = 0;
                            int finishIndex = realLen - 1;
                            //skip starts white-spaces
                            for ( ; ; ) //for ( ; startChar <= finishChar; startChar++ )
                            {
                                if ( ((_CTM[ startChar[ startIndex ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                     (finishIndex <= ++startIndex)
                                   )
                                {
                                    break;
                                }
                            }
                            //skip ends white-spaces
                            for ( ; ; ) //for ( ; startChar <= finishChar; finishChar-- )
                            {
                                if ( ((_CTM[ startChar[ finishIndex ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                     (--finishIndex <= startIndex)
                                   )
                                {
                                    break;
                                }
                            }

                            realLen = (finishIndex - startIndex) + 1;

                            if ( 0 < realLen )
                            {
                                _NativeString.Start  = startChar + startIndex;
                                _NativeString.Length = realLen;
                                return (true);
                            }
                        }
                        #endregion
                    }
                }

                #region [.last-line.]
                {
                    int len = (int) (_EndBuffer - start);
                    if ( 0 < len )
                    {
                        _Buffer = _EndBuffer + 1; //force move forward over '_EndBuffer'


                        var startChar = _CharBufferBase;

                        int realLen = _Encoding.GetChars( start, len, startChar, _CharBufferLength );
                            
                        int startIndex  = 0;
                        int finishIndex = realLen - 1;
                        //skip starts white-spaces
                        for ( ; ; ) //for ( ; startChar <= finishChar; startChar++ )
                        {
                            if ( ((_CTM[ startChar[ startIndex ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                 (finishIndex <= ++startIndex)
                               )
                            {
                                break;
                            }
                        }
                        //skip ends white-spaces
                        for ( ; ; ) //for ( ; startChar <= finishChar; finishChar-- )
                        {
                            if ( ((_CTM[ startChar[ finishIndex ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                 (--finishIndex <= startIndex)
                               )
                            {
                                break;
                            }
                        }

                        realLen = (finishIndex - startIndex) + 1;

                        if ( 0 < realLen )
                        {
                            _NativeString.Start  = startChar + startIndex;
                            _NativeString.Length = realLen;
                            return (true);
                        }
                    }
                } 
                #endregion

                _NativeString = NativeString.EMPTY;
                return (false);
            }

            object IEnumerator.Current
            {
                get { return (Current); }
            }
            public void Reset()
            {
                throw (new NotSupportedException());
            }

            public static EnumeratorMMF Create( string fileName )
            {
                return (new EnumeratorMMF( fileName ));
            }
        }
        

        private static CharType* _CTM;

        static LanguageConfigAdv()
        {
            _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
        }

        public LanguageConfigAdv( Language language, string modelFilename ) : base( language, modelFilename )
        {
        }

        internal void LoadModelFilenameContentMMF_v1( LoadModelFilenameContentMMFCallback_v1 callbackAction )
        {
            using ( var emmf = EnumeratorMMF.Create( ModelFilename ) )
            {
                var lineCount = 0;
                var text      = default(string);
                var weight    = default(float);
                var pair      = new Pair_v1() { Language = this.Language };

                #region [.read first line.]
                if ( !emmf.MoveNext() )
                {
                    return;
                } 
                #endregion

                #region [.skip beginning comments.]
                for ( ; ; )
                {
                    #region [.check on comment.]
                    if ( *emmf.Current.Start != '#' )
                    {
                        break;
                    } 
                    #endregion

                    #region [.move to next line.]
                    if ( !emmf.MoveNext() )
                    {
                        return;
                    }
                    #endregion
                } 
                #endregion

                #region [.read all lines.]
                for ( ; ; )
                {
                    lineCount++;

                    var ns = emmf.Current;

                    #region [.first-value in string.]
                    int startIndex_1  = 0;
                    int finishIndex_2 = ns.Length - 1;

                    #region commented
                    //skip starts white-spaces
                    /*for ( ; ; )
                    {
                        if ( ((_CTM[ ns.Start[ startIndex_1 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                             (finishIndex_2 <= ++startIndex_1)
                           )
                        {
                            break;
                        }
                    }*/
                    #endregion
                    //search '\t'
                    int startIndex_2  = 0;
                    int finishIndex_1 = 0;
                    for ( ; ; )
                    {
                        if ( ns.Start[ finishIndex_1 ] == '\t' )
                        {
                            startIndex_2 = finishIndex_1 + 1;
                            finishIndex_1--;
                            break;
                        }
                        //not found '\t'
                        if ( finishIndex_2 <= ++finishIndex_1 )
                        {
                            throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                        }
                    }
                    //skip ends white-spaces
                    for ( ; ; )
                    {
                        if ( ((_CTM[ ns.Start[ finishIndex_1 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                             (--finishIndex_1 <= startIndex_1)
                           )
                        {
                            break;
                        }
                    }

                    if ( finishIndex_1 < startIndex_1 )
                    {
                        throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                    }
                    #endregion

                    #region [.second-value in string.]
                    //skip starts white-spaces
                    for ( ; ; )
                    {
                        if ( ((_CTM[ ns.Start[ startIndex_2 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                             (finishIndex_2 <= ++startIndex_2)
                           )
                        {
                            break;
                        }
                    }
                    #region commented
                    //skip ends white-spaces
                    /*for ( ; ; )
                    {
                        if ( ((_CTM[ ns.Start[ finishIndex_2 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                             (--finishIndex_2 <= startIndex_1)
                           )
                        {
                            break;
                        }
                    }*/
                    #endregion
                    #endregion

                    #region [.fill 'Pair_v1' & calling 'callbackAction()'.]
                    var len = (finishIndex_2 - startIndex_2) + 1;
                    text = StringsHelper.ToString( ns.Start + startIndex_2, len );

                    if ( !float.TryParse( text, NS, NFI, out weight ) ) //if ( !Number.TryParseSingle( text, NS, NFI, out weight ) )
                    {
                        throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                    }

                    len = (finishIndex_1 - startIndex_1) + 1;
                    text = StringsHelper.ToString( ns.Start + startIndex_1, len );
                    StringsHelper.ToUpperInvariantInPlace( text );

                    pair.Text   = text;
                    pair.Weight = weight;
                    callbackAction( ref pair ); 
                    #endregion

                    #region [.move to next line.]
                    if ( !emmf.MoveNext() )
                    {
                        break;
                    } 
                    #endregion
                }
                #endregion
            }        
        }
        internal void LoadModelFilenameContentMMF_v2( LoadModelFilenameContentMMFCallback_v2 callbackAction )
        {
            using ( var emmf = EnumeratorMMF.Create( ModelFilename ) )
            {
                var lineCount = 0;
                var text      = default(string);
                var weight    = default(float);
                var pair      = new Pair_v2() { Language = this.Language };

                #region [.read first line.]
                if ( !emmf.MoveNext() )
                {
                    return;
                } 
                #endregion

                #region [.skip beginning comments.]
                for ( ; ; )
                {
                    #region [.check on comment.]
                    if ( *emmf.Current.Start != '#' )
                    {
                        break;
                    } 
                    #endregion

                    #region [.move to next line.]
                    if ( !emmf.MoveNext() )
                    {
                        return;
                    }
                    #endregion
                } 
                #endregion

                #region [.read all lines.]
                for ( ; ; )
                {
                    lineCount++;

                    var ns = emmf.Current;

                    #region [.first-value in string.]
                    int startIndex_1  = 0;
                    int finishIndex_2 = ns.Length - 1;

                    #region commented
                    //skip starts white-spaces
                    /*for ( ; ; )
                    {
                        if ( ((_CTM[ ns.Start[ startIndex_1 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                             (finishIndex_2 <= ++startIndex_1)
                           )
                        {
                            break;
                        }
                    }*/
                    #endregion
                    //search '\t'
                    int startIndex_2  = 0;
                    int finishIndex_1 = 0;
                    for ( ; ; )
                    {
                        if ( ns.Start[ finishIndex_1 ] == '\t' )
                        {
                            startIndex_2 = finishIndex_1 + 1;
                            finishIndex_1--;
                            break;
                        }
                        //not found '\t'
                        if ( finishIndex_2 <= ++finishIndex_1 )
                        {
                            throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                        }
                    }
                    //skip ends white-spaces
                    for ( ; ; )
                    {
                        if ( ((_CTM[ ns.Start[ finishIndex_1 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                             (--finishIndex_1 <= startIndex_1)
                           )
                        {
                            break;
                        }
                    }

                    if ( finishIndex_1 < startIndex_1 )
                    {
                        throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                    }
                    #endregion

                    #region [.second-value in string.]
                    //skip starts white-spaces
                    for ( ; ; )
                    {
                        if ( ((_CTM[ ns.Start[ startIndex_2 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                             (finishIndex_2 <= ++startIndex_2)
                           )
                        {
                            break;
                        }
                    }
                    #region commented
                    //skip ends white-spaces
                    /*for ( ; ; )
                    {
                        if ( ((_CTM[ ns.Start[ finishIndex_2 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                             (--finishIndex_2 <= startIndex_1)
                           )
                        {
                            break;
                        }
                    }*/
                    #endregion
                    #endregion

                    #region [.fill 'Pair_v2' & calling 'callbackAction()'.]
                    var len = (finishIndex_2 - startIndex_2) + 1;
                    text = StringsHelper.ToString( ns.Start + startIndex_2, len );

                    if ( !float.TryParse( text, NS, NFI, out weight ) ) //if ( !Number.TryParseSingle( text, NS, NFI, out weight ) )
                    {
                        throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                    }

                    pair.TextLength = (finishIndex_1 - startIndex_1) + 1;
                    var textPtr = ns.Start + startIndex_1;
                    textPtr[ pair.TextLength ] = '\0';
                    StringsHelper.ToUpperInvariantInPlace( textPtr, pair.TextLength );

                    pair.TextPtr = (IntPtr) textPtr;
                    pair.Weight = weight;

                    callbackAction( ref pair ); 
                    #endregion

                    #region [.move to next line.]
                    if ( !emmf.MoveNext() )
                    {
                        break;
                    }
                    #endregion
                }
                #endregion
            }
        }
#if DEBUG
        public override string ToString()
        {
            return (Language + ", '" + ModelFilename + '\'');
        } 
#endif
    }
}
