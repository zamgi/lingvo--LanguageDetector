using System.Collections.Generic;
using System.IO;

using lingvo.core;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MModelClassicMMF : MModelMMFBase, IMModel
    {
        /// <summary>
        /// 
        /// </summary>
        private struct Pair
        {
            public string   Text;
            public float    Weight;
            public Language Language;
#if DEBUG
            public override string ToString() => $"{Text}, {Weight}, {Language}";
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private delegate void LoadModelFilenameContentCallback( ref Pair pair );

        /// <summary>
        /// 
        /// </summary>
        unsafe private sealed class LanguageModelFileReaderMMF  : LanguageConfig
        {
            private LanguageModelFileReaderMMF( LanguageConfig languageConfig ) : base( languageConfig.Language, languageConfig.ModelFilename ) { }

            private void LoadModelFilenameContent( LoadModelFilenameContentCallback callbackAction )
            {
                using ( var emmf = EnumeratorMMF.Create( ModelFilename ) )
                {
                    var lineCount = 0;
                    var text      = default(string);
                    var weight    = default(float);
                    var pair      = new Pair() { Language = this.Language };

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

            public static void Read( LanguageConfig languageConfig, LoadModelFilenameContentCallback callbackAction )
            {
                var _this = new LanguageModelFileReaderMMF( languageConfig );
                _this.LoadModelFilenameContent( callbackAction );
            }
        }

        #region [.ctor().]
        private Dictionary< string, BucketValue > _Dictionary;
        public MModelClassicMMF( MModelConfig config )
        {
            //var sw = Stopwatch.StartNew();
            ConsecutivelyLoadMMF( config );
            //sw.Stop();
            //Console.WriteLine( "Elapsed: " + sw.Elapsed );        
        }
        public void Dispose()
        {
            if ( _Dictionary != null )
            {
                _Dictionary.Clear();
                _Dictionary = null;
            }
        } 
        #endregion

        #region [.model-dictionary loading.]
        private void ConsecutivelyLoadMMF( MModelConfig config )
        {
            _Dictionary = (0 < config.ModelDictionaryCapacity) 
                         ? new Dictionary< string, BucketValue >( config.ModelDictionaryCapacity )
                         : new Dictionary< string, BucketValue >();

            var callback = new LoadModelFilenameContentCallback( ConsecutivelyLoadMMFCallback );

            foreach ( var languageConfig in config.LanguageConfigs )
            {
                LanguageModelFileReaderMMF.Read( languageConfig, callback );
            }
        }
        private void ConsecutivelyLoadMMFCallback( ref Pair pair )
        {
            if ( _Dictionary.TryGetValue( pair.Text, out var bucketVal ) )
            {
                var bucketRef = new BucketRef() { Language = pair.Language, Weight = pair.Weight };
                if ( bucketVal.NextBucket == null )
                {
                    bucketVal.NextBucket = bucketRef;

                    _Dictionary[ pair.Text ] = bucketVal;
                }
                else
                {
                    var br = bucketVal.NextBucket;
                    for (; br.NextBucket != null; br = br.NextBucket );
                    br.NextBucket = bucketRef;
                }                        
            }
            else
            {
                _Dictionary.Add( pair.Text, new BucketValue( pair.Language, pair.Weight ) );
            }
        }
        #endregion

        #region [.IModel.]
        public int RecordCount => _Dictionary.Count;
        public bool TryGetValue( string ngram, out IEnumerable< WeighByLanguage > weighByLanguages )
        {
            if ( _Dictionary.TryGetValue( ngram, out var bucketVal ) )
            {
                weighByLanguages = new WeighByLanguageEnumerator( in bucketVal ); //bucketVal.GetWeighByLanguages();
                return (true);
            }
            weighByLanguages = null;
            return (false);
        }
        public IEnumerable< MModelRecord > GetAllRecords() => _Dictionary.GetAllModelRecords();
        #endregion
    }
}
