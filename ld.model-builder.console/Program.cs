using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using lingvo.core.algorithm;
using lingvo.tokenizing;
using lingvo.urls;

namespace lingvo
{
    /// <summary>
    /// 
    /// </summary>
    internal enum BuildModeEnum { single_model, all_possible_models }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config
    {
        private Config()
        {
            USE_BOOST_PRIORITY = bool.Parse( ConfigurationManager.AppSettings[ "USE_BOOST_PRIORITY" ] );
            BUILD_MODE         = (BuildModeEnum) Enum.Parse( typeof(BuildModeEnum), ConfigurationManager.AppSettings[ "BUILD_MODE" ], true );

            URL_DETECTOR_RESOURCES_XML_FILENAME = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];

            NGARMS        = (NGramsEnum)       Enum.Parse( typeof(NGramsEnum)      , ConfigurationManager.AppSettings[ "NGARMS"        ], true );
            CUT_THRESHOLD = (CutThresholdEnum) Enum.Parse( typeof(CutThresholdEnum), ConfigurationManager.AppSettings[ "CUT_THRESHOLD" ], true );
        
            _INPUT_FOLDERS_BY_LANGUAGE_ = ConfigurationManager.AppSettings[ "INPUT_FOLDERS_BY_LANGUAGE" ];

            _CLEAR_CYRILLICS_CHARS_BY_LANGUAGE_ = ConfigurationManager.AppSettings[ "CLEAR_CYRILLICS_CHARS_BY_LANGUAGE" ];        

            INPUT_BASE_FOLDER = ConfigurationManager.AppSettings[ "INPUT_BASE_FOLDER"  ];
            INPUT_ENCODING    = Encoding.GetEncoding( ConfigurationManager.AppSettings[ "INPUT_ENCODING" ] );

            OUTPUT_BASE_FOLDER = ConfigurationManager.AppSettings[ "OUTPUT_BASE_FOLDER" ];
            OUTPUT_ENCODING    = Encoding.GetEncoding( ConfigurationManager.AppSettings[ "OUTPUT_ENCODING" ] );

            USE_PORTION      = bool.Parse( ConfigurationManager.AppSettings[ "USE_PORTION" ] );
            MAX_PORTION_SIZE = int.Parse( ConfigurationManager.AppSettings[ "MAX_PORTION_SIZE" ] );


            #region [.INPUT_FOLDERS_BY_LANGUAGE.]
            var inputFoldersByLanguage = _INPUT_FOLDERS_BY_LANGUAGE_.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
            for ( var i = 0; i < inputFoldersByLanguage.Length; i++ )
            {
                var item = inputFoldersByLanguage[ i ].Trim( ' ', '\r', '\n', '\t' );
                inputFoldersByLanguage[ i ] = item;
            }
            INPUT_FOLDERS_BY_LANGUAGE = inputFoldersByLanguage.Where( item => !string.IsNullOrWhiteSpace( item ) ).ToArray();
            #endregion

            #region [.CLEAR_CYRILLICS_CHARS_BY_LANGUAGE.]
            var clearCyrillicsCharsByLanguage = _CLEAR_CYRILLICS_CHARS_BY_LANGUAGE_.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
            for ( var i = 0; i < clearCyrillicsCharsByLanguage.Length; i++ )
            {
                var item = clearCyrillicsCharsByLanguage[ i ].Trim( ' ', '\r', '\n', '\t' );
                clearCyrillicsCharsByLanguage[ i ] = item;
            }
            CLEAR_CYRILLICS_CHARS_BY_LANGUAGE = clearCyrillicsCharsByLanguage.Where( item => !string.IsNullOrWhiteSpace( item ) ).ToArray();
            #endregion

            #region [.corrent CUT_THRESHOLD & NGARMS.]
            //-correct CUT_THRESHOLD-
            switch ( CUT_THRESHOLD )
            {
                case CutThresholdEnum.cut_1:
                case CutThresholdEnum.cut_2:
                    break;

                default:
                    Console.WriteLine( "(corrected CUT_THRESHOLD: " + CUT_THRESHOLD + " => " + CutThresholdEnum.cut_1 + ")!" );
                    CUT_THRESHOLD = CutThresholdEnum.cut_1;
                    break;
            }

            //-correct NGARMS-
            switch ( NGARMS )
            {
                case NGramsEnum.ngram_1:
                case NGramsEnum.ngram_2:
                    break;

                default:
                    Console.WriteLine( "(corrected NGARMS: " + NGARMS + " => " + NGramsEnum.ngram_2 + ")!" );
                    NGARMS = NGramsEnum.ngram_2;
                    break;
            }
            #endregion
        }

        private static Config _Inst;
        public static Config Inst
        {
            get
            {
                if ( _Inst == null )
                {
                    lock ( typeof(Config) )
                    {
                        if ( _Inst == null )
                        {
                            _Inst = new Config();
                        }
                    }
                }
                return (_Inst);
            }
        }

        public readonly bool USE_BOOST_PRIORITY;
        public readonly BuildModeEnum BUILD_MODE;

        public readonly string URL_DETECTOR_RESOURCES_XML_FILENAME;

        public readonly NGramsEnum       NGARMS;
        public readonly CutThresholdEnum CUT_THRESHOLD;
        
        public readonly string[] INPUT_FOLDERS_BY_LANGUAGE;
        private string _INPUT_FOLDERS_BY_LANGUAGE_;

        public readonly string[] CLEAR_CYRILLICS_CHARS_BY_LANGUAGE;
        private string _CLEAR_CYRILLICS_CHARS_BY_LANGUAGE_;

        public readonly string   INPUT_BASE_FOLDER;
        public readonly Encoding INPUT_ENCODING;

        public readonly string   OUTPUT_BASE_FOLDER;
        public readonly Encoding OUTPUT_ENCODING;

        public readonly bool USE_PORTION;
        public readonly int  MAX_PORTION_SIZE;
    }
}

namespace lingvo.ld.modelbuilder
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 
        /// </summary>
        private struct build_params_t
        {
            public UrlDetectorModel UrlDetectorModel;
            public string           InputBaseFolder;
            public string[]         InputFoldersByLanguage;
            public NGramsEnum       Ngrams;
            public CutThresholdEnum CutThreshold;
            public string           OutputBaseFolder;
            public int              MaxPortionSize;
            public string[]         ClearCyrillicsCharsByLanguage;

            public override string ToString() => (Ngrams + "-" + CutThreshold);
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class word_IComparer : IComparer< string >, IEqualityComparer< string >
        {
            public static readonly word_IComparer Instance = new word_IComparer();
            private word_IComparer() { }

            #region [.IComparer< string >.]
            public int Compare( string x, string y )
            {
                return (string.CompareOrdinal( x, y ));
            }
            #endregion

            #region [.IEqualityComparer< string >.]
            public bool Equals( string x, string y )
            {
                return (string.CompareOrdinal( x, y ) == 0);
            }
            public int GetHashCode( string obj )
            {
                return (obj.GetHashCode());
            }
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class portioner_t
        {
            /// <summary>
            /// 
            /// </summary>
            private sealed class ngram_filereader_t : IDisposable
            {
                private static readonly char[] SPLIT_CHAR = new[] { '\t' };
                private readonly StreamReader _Sr;
                private string _Line;

                public ngram_filereader_t( string fileName )
                {
                    _Sr   = new StreamReader( fileName, Config.Inst.INPUT_ENCODING );
                    _Line = _Sr.ReadLine();
                }
                public void Dispose() => _Sr.Dispose();

                public word_t ReadNext()
                {
                    if ( _Line != null )
                    {
                        var a = _Line.Split( SPLIT_CHAR, StringSplitOptions.RemoveEmptyEntries );
                        _Line = _Sr.ReadLine();
                        var word = new word_t() 
                        { 
                            Value = a[ 0 ], 
                            Count = int.Parse( a[ 1 ] ) 
                        };
                        return (word);
                    }
                    return (null);
                }
            }
            /// <summary>
            /// 
            /// </summary>
            private sealed class tuple_t : IDisposable
            {
                public tuple_t( word_t _word, ngram_filereader_t _ngram_filereader )
                {
                    word             = _word;
                    ngram_filereader = _ngram_filereader;
                }

                public word_t word { get; set; }
                public ngram_filereader_t ngram_filereader { get; }

                public void Dispose() => ngram_filereader.Dispose();
            }
            /// <summary>
            /// 
            /// </summary>
            private sealed class tuple_word_value_IComparer : IComparer< tuple_t >
            {
                public static readonly tuple_word_value_IComparer Instance = new tuple_word_value_IComparer();
                private tuple_word_value_IComparer() { }

                #region [.IComparer< tuple_t >.]
                public int Compare( tuple_t x, tuple_t y )
                {
                    return (string.CompareOrdinal( x.word.Value, y.word.Value ));
                }
                #endregion
            }
            /// <summary>
            /// 
            /// </summary>
            private sealed class tuple_word_count_IComparer : IComparer< tuple_t >
            {
                public static readonly tuple_word_count_IComparer Instance = new tuple_word_count_IComparer();
                private tuple_word_count_IComparer() { }

                #region [.IComparer< tuple_t >.]
                public int Compare( tuple_t x, tuple_t y )
                {
                    var d = y.word.Count - x.word.Count;            
                    if ( d != 0 )
                        return (d);

                    return (string.CompareOrdinal( y.word.Value, x.word.Value ));
                }
                #endregion
            }

            private readonly build_params_t            _Bp;
            private readonly FileInfo                  _Fi;
            private int                                _DocumentWordCount;
            private readonly Dictionary< string, int > _DocumentNgrams_1;
            private readonly Dictionary< string, int > _DocumentNgrams_2;
            private readonly Dictionary< string, int > _DocumentNgrams_3;
            private readonly Dictionary< string, int > _DocumentNgrams_4;
            private string                             _Word_prev1;
            private string                             _Word_prev2;
            private string                             _Word_prev3;
            private readonly Dictionary< NGramsEnum, List< string > > _OutputFilenames;
            private readonly StringBuilder             _Sb;
            private readonly tfidf                     _Tfidf;

            public portioner_t( build_params_t bp, FileInfo fi, tfidf _tfidf )
            {
                _Bp = bp;
                _Fi = fi;

                _OutputFilenames  = new Dictionary< NGramsEnum, List< string > >();
                _OutputFilenames.Add( NGramsEnum.ngram_1, new List< string >() );
                _DocumentNgrams_1 = new Dictionary< string, int >( _Bp.MaxPortionSize, word_IComparer.Instance );

                switch ( _Bp.Ngrams )
                {
                    case NGramsEnum.ngram_4:
                        _OutputFilenames.Add( NGramsEnum.ngram_4, new List< string >() );
                        _DocumentNgrams_4 = new Dictionary< string, int >( _Bp.MaxPortionSize, word_IComparer.Instance );
                    goto case NGramsEnum.ngram_3;

                    case NGramsEnum.ngram_3:
                        _OutputFilenames.Add( NGramsEnum.ngram_3, new List< string >() );
                        _DocumentNgrams_3 = new Dictionary< string, int >( _Bp.MaxPortionSize, word_IComparer.Instance );
                    goto case NGramsEnum.ngram_2;

                    case NGramsEnum.ngram_2:
                        _OutputFilenames.Add( NGramsEnum.ngram_2, new List< string >() );
                        _DocumentNgrams_2 = new Dictionary< string, int >( _Bp.MaxPortionSize, word_IComparer.Instance );
                    break;
                }
               
                _Sb = new StringBuilder();

                _Tfidf = _tfidf;
            }

            private void ProcessWordAction( string word )
            {
                CheckPortion( _DocumentNgrams_1, NGramsEnum.ngram_1 );

                _DocumentWordCount++;

                _DocumentNgrams_1.AddOrUpdate( word );

                switch ( _Bp.Ngrams )
                {
                    case NGramsEnum.ngram_4:
                        CheckPortion( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                        if ( _Word_prev3 != null )
                        {
                            _DocumentNgrams_4.AddOrUpdate( _Sb.Clear()
                                                              .Append( _Word_prev3 ).Append( ' ' )
                                                              .Append( _Word_prev2 ).Append( ' ' )
                                                              .Append( _Word_prev1 ).Append( ' ' )
                                                              .Append( word )
                                                              .ToString() 
                                                         );                        
                        }
                        _Word_prev3 = _Word_prev2;
                    goto case NGramsEnum.ngram_3;

                    case NGramsEnum.ngram_3:
                        CheckPortion( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                        if ( _Word_prev2 != null )
                        {
                            _DocumentNgrams_3.AddOrUpdate( _Sb.Clear()
                                                              .Append( _Word_prev2 ).Append( ' ' )
                                                              .Append( _Word_prev1 ).Append( ' ' )
                                                              .Append( word )
                                                              .ToString() 
                                                         );                        
                        }
                        _Word_prev2 = _Word_prev1;
                    goto case NGramsEnum.ngram_2;

                    case NGramsEnum.ngram_2:
                        CheckPortion( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                        if ( _Word_prev1 != null )
                        {
                            _DocumentNgrams_2.AddOrUpdate( _Sb.Clear()
                                                              .Append( _Word_prev1 ).Append( ' ' )
                                                              .Append( word )
                                                              .ToString() 
                                                         );                        
                        }
                        _Word_prev1 = word;
                    break;
                }
            }
            private void ProcessWordActionClearCyrillicsChars( string word )
            {
                if ( word.HasCyrillicsChars() )
                {
                    _Word_prev3 = _Word_prev2 = _Word_prev1 = null;
                    return;
                }

                CheckPortion( _DocumentNgrams_1, NGramsEnum.ngram_1 );

                _DocumentWordCount++;

                _DocumentNgrams_1.AddOrUpdate( word );

                switch ( _Bp.Ngrams )
                {
                    case NGramsEnum.ngram_4:
                        CheckPortion( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                        if ( _Word_prev3 != null )
                        {
                            _DocumentNgrams_4.AddOrUpdate( _Sb.Clear()
                                                              .Append( _Word_prev3 ).Append( ' ' )
                                                              .Append( _Word_prev2 ).Append( ' ' )
                                                              .Append( _Word_prev1 ).Append( ' ' )
                                                              .Append( word )
                                                              .ToString() 
                                                         );                        
                        }
                        _Word_prev3 = _Word_prev2;
                    goto case NGramsEnum.ngram_3;

                    case NGramsEnum.ngram_3:
                        CheckPortion( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                        if ( _Word_prev2 != null )
                        {
                            _DocumentNgrams_3.AddOrUpdate( _Sb.Clear()
                                                              .Append( _Word_prev2 ).Append( ' ' )
                                                              .Append( _Word_prev1 ).Append( ' ' )
                                                              .Append( word )
                                                              .ToString() 
                                                         );                        
                        }
                        _Word_prev2 = _Word_prev1;
                    goto case NGramsEnum.ngram_2;

                    case NGramsEnum.ngram_2:
                        CheckPortion( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                        if ( _Word_prev1 != null )
                        {
                            _DocumentNgrams_2.AddOrUpdate( _Sb.Clear()
                                                              .Append( _Word_prev1 ).Append( ' ' )
                                                              .Append( word )
                                                              .ToString() 
                                                         );                        
                        }
                        _Word_prev1 = word;
                    break;
                }
            }
            private void ProcessLastAction()
            {
                CheckLastPortion( _DocumentNgrams_1, NGramsEnum.ngram_1 );

                switch ( _Bp.Ngrams )
                {
                    case NGramsEnum.ngram_4:
                        CheckLastPortion( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                    goto case NGramsEnum.ngram_3;

                    case NGramsEnum.ngram_3:
                        CheckLastPortion( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                    goto case NGramsEnum.ngram_2;

                    case NGramsEnum.ngram_2:
                        CheckLastPortion( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                    break;
                }
            }

            private void CheckPortion( Dictionary< string, int > dict, NGramsEnum ngram )
            {
                if ( _Bp.MaxPortionSize <= dict.Count )
                {
                    var lst = _OutputFilenames[ ngram ];
                    var outputFilename = Write2File( _Fi, lst.Count, dict, ngram );
                    lst.Add( outputFilename );
                    dict.Clear();
                }
            }
            private void CheckLastPortion( Dictionary< string, int > dict, NGramsEnum ngram )
            {
                if ( dict.Count != 0 )
                {
                    var lst = _OutputFilenames[ ngram ];
                    if ( 0 < lst.Count )
                    {
                        var outputFilename = Write2File( _Fi, lst.Count, dict, ngram );
                        lst.Add( outputFilename );
                        dict.Clear();
                    } 
                }
            }
            
            private void ProcessNgrams()
            {
                _Tfidf.BeginAddDocumentWords();

                //-ngran_1-
                ProcessNgrams_Routine( NGramsEnum.ngram_1 );

                switch ( _Bp.Ngrams )
                {
                    case NGramsEnum.ngram_4:
                        ProcessNgrams_Routine( NGramsEnum.ngram_4 );
                    goto case NGramsEnum.ngram_3;

                    case NGramsEnum.ngram_3:
                        ProcessNgrams_Routine( NGramsEnum.ngram_3 );
                    goto case NGramsEnum.ngram_2;

                    case NGramsEnum.ngram_2:
                        ProcessNgrams_Routine( NGramsEnum.ngram_2 );
                    break;
                }

                _Tfidf.EndAddDocumentWords( _DocumentWordCount );
            }
            private void ProcessNgrams_Routine( NGramsEnum ngram )
            {
                var outputFilenames = _OutputFilenames[ ngram ];
                if ( outputFilenames.Count == 0 )
                {
                    var dict = default(Dictionary< string, int >);
                    switch ( ngram )
                    {
                        //case NGramsEnum.ngram_1: dict = _DocumentNgrams_1; break;
                        case NGramsEnum.ngram_2: dict = _DocumentNgrams_2; break;
                        case NGramsEnum.ngram_3: dict = _DocumentNgrams_3; break;
                        case NGramsEnum.ngram_4: dict = _DocumentNgrams_4; break;
                        default:                 dict = _DocumentNgrams_1; break;
                    }

                    //-1-
                    _Tfidf.CutDictionaryIfNeed( dict, ngram );


                    //-2-
                    _Tfidf.AddDocumentWords( dict );
                }
                else
                {
                    //-1-
                    _OutputFilenames[ ngram ] = new List< string >();
                    var ss  = new SortedSet< word_t >( word_t_comparer.Instance );
                    var sum = 0;
                    foreach ( var word in GroupByMerging_1( outputFilenames ) )
                    {
                        sum += word.Count;                        
                        ss.Add( word );
                        if ( _Bp.MaxPortionSize <= ss.Count )
                        {
                            var lst = _OutputFilenames[ ngram ];
                            var outputFilename = Write2File( _Fi, lst.Count, ss, ngram );
                            lst.Add( outputFilename );
                            ss.Clear();
                        }
                    }
                    if ( ss.Count != 0 )
                    {
                        var lst = _OutputFilenames[ ngram ];
                        var outputFilename = Write2File( _Fi, lst.Count, ss, ngram );
                        lst.Add( outputFilename );
                        ss.Clear();
                    }

                    //-2-
                    outputFilenames.ForEach( outputFilename => File.Delete( outputFilename ) );
                    outputFilenames = _OutputFilenames[ ngram ];
                    var tuples = CreateTuples( outputFilenames );
                    ss = _Tfidf.CreateSortedSetAndCutIfNeed( GroupByMerging_2( tuples ), ngram, sum );


                    //-3-
                    tuples.ForEach( tuple => tuple.Dispose() );
                    outputFilenames.ForEach( outputFilename => File.Delete( outputFilename ) );
                    _Tfidf.AddDocumentWords( ss );
                }
            }

            public void BuildTFMatrix_UsePortion( bool clearCyrillicsChars )
            {
                using ( var tokenizer = new mld_tokenizer( _Bp.UrlDetectorModel ) )
                {
                    //-1-
                    var processWordAction = default(Action< string >);
                    if ( clearCyrillicsChars )
                    {
                        processWordAction = ProcessWordActionClearCyrillicsChars;
                    }
                    else
                    {
                        processWordAction = ProcessWordAction;
                    }

                    using ( var sr = new StreamReader( _Fi.FullName, Config.Inst.INPUT_ENCODING ) )
                    {
                        for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                        {
                            tokenizer.Run( line, processWordAction );
                        }

                        ProcessLastAction();

                        if ( (_OutputFilenames[ NGramsEnum.ngram_1 ].Count == 0) && (_DocumentNgrams_1.Count == 0) )
                        {
                            throw (new InvalidDataException( "input text is-null-or-white-space, filename: '" + _Fi.FullName + '\'' ));
                        }
                    }

                    //-2-
                    ProcessNgrams();
                }
            }

            private static IEnumerable< word_t > GroupByMerging_1( List< string > fileNames )
            {
                var current_tuples = new List< tuple_t >( fileNames.Count );
                for ( var i = 0; i < fileNames.Count; i++ )
                {
                    var aw = new ngram_filereader_t( fileNames[ i ] );
                    var w  = aw.ReadNext();

                    current_tuples.Add( new tuple_t( w, aw ) );
                }

                for ( ; current_tuples.Count != 0; )
                {
                    current_tuples.Sort( tuple_word_value_IComparer.Instance );

                    var tuple = current_tuples[ 0 ];
                    var word  = tuple.word;
                
                    for ( var j = 1; j < current_tuples.Count; j++ )
                    {
                        var t = current_tuples[ j ];
                        if ( word.Value == t.word.Value )
                        {
                            word.Count += t.word.Count;

                            t.word = t.ngram_filereader.ReadNext();
                            if ( t.word == null )
                            {
                                t.Dispose();
                                current_tuples.RemoveAt( j );
                                j--;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    tuple.word = tuple.ngram_filereader.ReadNext();
                    if ( tuple.word == null )
                    {
                        tuple.Dispose();
                        current_tuples.RemoveAt( 0 );
                    }

                    yield return (word);
                }

            }
            private static List< tuple_t > CreateTuples( List< string > fileNames )
            {
                var tuples = new List< tuple_t >( fileNames.Count );
                for ( var i = 0; i < fileNames.Count; i++ )
                {
                    var aw = new ngram_filereader_t( fileNames[ i ] );
                    var w  = aw.ReadNext();

                    tuples.Add( new tuple_t( w, aw ) );
                }
                return (tuples);
            }
            private static IEnumerable< word_t > GroupByMerging_2( List< tuple_t > tuples )
            {
                var current_tuples = tuples;
                #region 
                /*var current_tuples = new List< tuple_t >( fileNames.Count );                
                for ( var i = 0; i < fileNames.Count; i++ )
                {
                    var aw = new ngram_filereader_t( fileNames[ i ] );
                    var w  = aw.ReadNext();

                    current_tuples.Add( new tuple_t( w, aw ) );
                }
                */
                #endregion

                for ( ; current_tuples.Count != 0; )
                {
                    current_tuples.Sort( tuple_word_count_IComparer.Instance );

                    var tuple = current_tuples[ 0 ];
                    var word  = tuple.word;

                    #region don't need. commented
                    /*for ( var j = 1; j < current_tuples.Count; j++ )
                    {
                        var t = current_tuples[ j ];
                        if ( word.Value == t.word.Value )
                        {
                            word.Count += t.word.Count;

                            t.word = t.ngram_filereader.ReadNext();
                            if ( t.word == null )
                            {
                                t.Dispose();
                                current_tuples.RemoveAt( j );
                                j--;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }*/
                    #endregion

                    tuple.word = tuple.ngram_filereader.ReadNext();
                    if ( tuple.word == null )
                    {
                        tuple.Dispose();
                        current_tuples.RemoveAt( 0 );
                    }

                    yield return (word);
                }

            }

            private static string Write2File( FileInfo fi, int portionNumber, Dictionary< string, int > tf_matrix, NGramsEnum tf_matrix_type )
            {
                var outputFilename = Path.Combine( fi.DirectoryName, "temp", fi.Name + '.' + tf_matrix_type.ToString() + '.' + portionNumber.ToString() );
                Console.Write( "start write portion-file: '" + outputFilename + "'..." );

                var ss = new SortedDictionary< string, int >( word_IComparer.Instance );
                foreach ( var p in tf_matrix )
                {
                    ss.Add( p.Key, p.Value );
                }
                tf_matrix.Clear();

                var ofi = new FileInfo( outputFilename );
                if ( !ofi.Directory.Exists )
                {
                    ofi.Directory.Create();
                }
                using ( var sw = new StreamWriter( outputFilename, false, Config.Inst.OUTPUT_ENCODING ) )
                {
                    foreach ( var p in ss )
                    {
                        sw.WriteLine( p.Key + '\t' + p.Value );
                    }
                }
                ss.Clear();
                ss = null;
                GC.Collect();

                Console.WriteLine( "end write portion-file" );

                return (outputFilename);
            }
            private static string Write2File( FileInfo fi, int portionNumber, SortedSet< word_t > ss, NGramsEnum ss_type )
            {
                var outputFilename = Path.Combine( fi.DirectoryName, "temp", fi.Name + ".ss." + ss_type.ToString() + '.' + portionNumber.ToString() );
                Console.Write( "start write portion-file: '" + outputFilename + "'..." );

                var ofi = new FileInfo( outputFilename );
                if ( !ofi.Directory.Exists )
                {
                    ofi.Directory.Create();
                }
                using ( var sw = new StreamWriter( outputFilename, false, Config.Inst.OUTPUT_ENCODING ) )
                {
                    foreach ( var word in ss )
                    {
                        sw.WriteLine( word.Value + '\t' + word.Count );
                    }
                }
                ss.Clear();
                ss = null;
                GC.Collect();

                Console.WriteLine( "end write portion-file" );

                return (outputFilename);
            }
        }

        private static void Main( string[] args )
        {
            var wasErrors = false;
            try
            {
                #region [.print to console config.]
                Console.WriteLine(Environment.NewLine + "----------------------------------------------");
                Console.WriteLine( "USE_BOOST_PRIORITY       : '" + Config.Inst.USE_BOOST_PRIORITY + "'" );
                Console.WriteLine( "BUILD_MODE               : '" + Config.Inst.BUILD_MODE + "'" );
                switch ( Config.Inst.BUILD_MODE )
                {
                    case BuildModeEnum.single_model:
                Console.WriteLine( "NGARMS                   : '" + Config.Inst.NGARMS  + "'" );
                Console.WriteLine( "CUT_THRESHOLD            : '" + Config.Inst.CUT_THRESHOLD + "'" );                
                    break;
                }
                Console.WriteLine( "INPUT_FOLDERS_BY_LANGUAGE: '" + string.Join( "'; '", Config.Inst.INPUT_FOLDERS_BY_LANGUAGE ) + "'" );
                Console.WriteLine( "INPUT_BASE_FOLDER        : '" + Config.Inst.INPUT_BASE_FOLDER + "'" );
                Console.WriteLine( "INPUT_ENCODING           : '" + Config.Inst.INPUT_ENCODING.WebName + "'" );
                Console.WriteLine( "OUTPUT_BASE_FOLDER       : '" + Config.Inst.OUTPUT_BASE_FOLDER + "'" );
                Console.WriteLine( "OUTPUT_ENCODING          : '" + Config.Inst.OUTPUT_ENCODING.WebName + "'" );
                Console.WriteLine( "USE_PORTION              : '" + Config.Inst.USE_PORTION + "'" );
                if ( Config.Inst.USE_PORTION )
                Console.WriteLine( "MAX_PORTION_SIZE         : '" + Config.Inst.MAX_PORTION_SIZE + "'" );
                Console.WriteLine("----------------------------------------------" + Environment.NewLine);
                #endregion

                #region [.use boost priority.]
                if ( Config.Inst.USE_BOOST_PRIORITY )
                {
                    var pr = Process.GetCurrentProcess();
                    pr.PriorityClass              = ProcessPriorityClass.RealTime;
                    pr.PriorityBoostEnabled       = true;
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                }
                #endregion

                #region [.url-detector.]
                var urlDetectorModel = new UrlDetectorModel( Config.Inst.URL_DETECTOR_RESOURCES_XML_FILENAME );
                #endregion

                #region [.build model's.]
                if ( Config.Inst.BUILD_MODE == BuildModeEnum.single_model )
                {
                    var bp = new build_params_t()
                    {
                        UrlDetectorModel              = urlDetectorModel,
                        InputBaseFolder               = Config.Inst.INPUT_BASE_FOLDER,
                        InputFoldersByLanguage        = Config.Inst.INPUT_FOLDERS_BY_LANGUAGE,
                        Ngrams                        = Config.Inst.NGARMS,
                        CutThreshold                  = Config.Inst.CUT_THRESHOLD,
                        OutputBaseFolder              = Config.Inst.OUTPUT_BASE_FOLDER,
                        MaxPortionSize                = Config.Inst.MAX_PORTION_SIZE,
                        ClearCyrillicsCharsByLanguage = Config.Inst.CLEAR_CYRILLICS_CHARS_BY_LANGUAGE,
                    };
                    var sw = Stopwatch.StartNew();
                    if ( Config.Inst.USE_PORTION )
                    {
                        Build_UsePortion( bp );
                    }
                    else
                    {
                        Build( bp );
                    }
                    sw.Stop();

                    Console.WriteLine( "'" + Config.Inst.NGARMS + "; " + Config.Inst.CUT_THRESHOLD + "' - success, elapsed: " + sw.Elapsed + Environment.NewLine );
                }
                else
                {
                    #region [.build model's.]
                    var sw_total = Stopwatch.StartNew();
                    foreach ( var t in Extensions.GetProcessParams() )
                    {
                        var bp = new build_params_t()
                        {
                            UrlDetectorModel              = urlDetectorModel,
                            InputBaseFolder               = Config.Inst.INPUT_BASE_FOLDER,
                            InputFoldersByLanguage        = Config.Inst.INPUT_FOLDERS_BY_LANGUAGE,
                            Ngrams                        = t.Item1,
                            CutThreshold                  = t.Item2,
                            OutputBaseFolder              = Config.Inst.OUTPUT_BASE_FOLDER,
                            MaxPortionSize                = Config.Inst.MAX_PORTION_SIZE,
                            ClearCyrillicsCharsByLanguage = Config.Inst.CLEAR_CYRILLICS_CHARS_BY_LANGUAGE,
                        };
                        try
                        {
                            var sw = Stopwatch.StartNew();
                            if ( Config.Inst.USE_PORTION )
                            {
                                Build_UsePortion( bp );
                            }
                            else
                            {
                                Build( bp );
                            }
                            sw.Stop();

                            Console.WriteLine( "'" + bp.Ngrams + "; " + bp.CutThreshold + "' - success, elapsed: " + sw.Elapsed + Environment.NewLine );
                        }
                        catch ( Exception ex )
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine( "'" + bp.Ngrams + "; " + bp.CutThreshold + "' - " +  ex.GetType() + ": " + ex.Message );
                            Console.ResetColor();
                            wasErrors = true;
                        }
                    }
                    sw_total.Stop();

                    Console.WriteLine( "total elapsed: " + sw_total.Elapsed );
                    #endregion
                }
                #endregion
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
                wasErrors = true;
            }

            if ( wasErrors )
            {
                Console.WriteLine( Environment.NewLine + "[.....finita fusking comedy (push ENTER 4 exit).....]" );
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine( Environment.NewLine + "[.....finita fusking comedy.....]" );
            }
        }

        private static void Build( build_params_t bp )
        {
            foreach ( var inputFolder4Language in bp.InputFoldersByLanguage )
            {
                BuildSingleLanguage( bp, inputFolder4Language );
            }
        }
        private static void BuildSingleLanguage( build_params_t bp, string inputFolder4Language )
        {
            #region [.-0-.]
            Console.WriteLine( "start process language-folder: '/" + inputFolder4Language + "/'..." );

            var _tfidf    = new tfidf( bp.Ngrams, bp.CutThreshold );
            var tokenizer = new mld_tokenizer( bp.UrlDetectorModel );
            var clearCyrillicsChars = bp.ClearCyrillicsCharsByLanguage.Contains( inputFolder4Language );

            var processWordAction = default(Action< string >);
            if ( clearCyrillicsChars )
            {
                processWordAction = (word) =>
                {
                    if ( !word.HasCyrillicsChars() )
                    {
                        _tfidf.AddDocumentWord( word );
                    }
                };
            }
            else
            {
                processWordAction = (word) =>
                {
                    _tfidf.AddDocumentWord( word );
                };
            }
            #endregion

            #region [.-1-.]
            var folder4Language = Path.Combine( bp.InputBaseFolder, inputFolder4Language );
            var fileNames = new List< string >();
            var fis = from fileName in Directory.EnumerateFiles( folder4Language )
                        let fi = new FileInfo( fileName )
                        orderby fi.Length descending
                      select fi;
            foreach ( var fi in fis )
            {
                fileNames.Add( fi.Name );
                Console.Write( "start process file: '" + fi.Name + "' [" + fi.DisplaySize() + "]..." );

                _tfidf.BeginAddDocument();
                using ( var sr = new StreamReader( fi.FullName, Config.Inst.INPUT_ENCODING ) )
                {
                    for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                    {
                        tokenizer.Run( line, processWordAction );
                    }

                    if ( !_tfidf.CurrentDocumentHasWords )
                    {
                        throw (new InvalidDataException( "input text is-null-or-white-space, filename: '" + fi.FullName + '\'' ));
                    }
                }
                _tfidf.EndAddDocument();
                GC.Collect();

                #region commented
                /*
                var words = tokenizer.run( text );
                text = null;
                GC.Collect();

                _tfidf.AddDocument( words );
                words = null;
                GC.Collect();
                */
                #endregion

                Console.WriteLine( "end process file" );
            }
            #endregion
            
            #region [.-2-.]
            Console.Write( "start process TFiDF..." );
            var _tfidf_result = _tfidf.Process();
            _tfidf = null;
            GC.Collect();
            Console.WriteLine( "end process TFiDF" );
            #endregion

            #region [.-3-.]
            Console.Write( "start write result..." );
            var outputFolder = Path.Combine( bp.OutputBaseFolder, inputFolder4Language );
            if ( !Directory.Exists( outputFolder ) )
            {
                Directory.CreateDirectory( outputFolder );
            }

            var fileNamesCount = fileNames.Count;
            var sws = new List< StreamWriter >( fileNamesCount );
            for ( var i = 0; i < fileNamesCount; i++ )
            {
                var fn = fileNames[ i ];
                var fi = new FileInfo( Path.Combine( outputFolder, fn ) );
                var outputFile = Path.Combine( fi.DirectoryName, fi.Name.Substring( 0, fi.Name.Length - fi.Extension.Length ) +
                                               "-(" + bp.Ngrams + "-" + bp.CutThreshold + ")" + fi.Extension );

                var sw = new StreamWriter( outputFile, false, Config.Inst.OUTPUT_ENCODING );
                sw.WriteLine( "#\t'" + fn + "' (" + bp.Ngrams + "-" + bp.CutThreshold + ")" );
                sws.Add( sw );
            }

            var nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
            
            for ( int i = 0, len = _tfidf_result.TFiDF.Length; i < len; i++ )
            {
                var values = _tfidf_result.TFiDF[ i ];
                //if ( values.AllValuesAreEquals() ) continue;
                var w = _tfidf_result.Words[ i ];
                for ( int j = 0; j < fileNamesCount; j++ )
                {
                    var v = values[ j ];
                    if ( v != 0 )
                    {
                        var sw = sws[ j ];
                        sw.Write( w );
                        sw.Write( '\t' );
                        sw.WriteLine( v.ToString( nfi ) );
                    }
                }
            }

            sws.ForEach( sw => { sw.Close(); sw.Dispose(); } );
            
            Console.WriteLine( "end write result" + Environment.NewLine );
            #endregion
        }

        private static void Build_UsePortion( build_params_t bp )
        {
            foreach ( var inputFolder4Language in bp.InputFoldersByLanguage )
            {
                BuildSingleLanguage_UsePortion( bp, inputFolder4Language );
            }
        }
        private static void BuildSingleLanguage_UsePortion( build_params_t bp, string inputFolder4Language )
        {
            #region [.-0-.]
            Console.WriteLine( "start process language-folder: '/" + inputFolder4Language + "/'..." );

            var _tfidf = new tfidf( bp.Ngrams, bp.CutThreshold );
            #endregion

            #region [.-1-.]
            var clearCyrillicsChars = bp.ClearCyrillicsCharsByLanguage.Contains( inputFolder4Language );
            var folder4Language = Path.Combine( bp.InputBaseFolder, inputFolder4Language );
            var fileNames = new List< string >();
            var fis = from fileName in Directory.EnumerateFiles( folder4Language )
                        let fi = new FileInfo( fileName )
                        orderby fi.Length descending
                      select fi;
            foreach ( var fi in fis )
            {
                fileNames.Add( fi.Name );
                Console.WriteLine( "start process file: '" + fi.Name + "' [" + fi.DisplaySize() + "]..." );

                BuildTFMatrix_UsePortion( bp, fi, _tfidf, clearCyrillicsChars );                

                Console.WriteLine( "end process file" + Environment.NewLine );
            }
            #endregion
            
            #region [.-2-.]
            Console.Write( "start process TFiDF..." );
            var _tfidf_result = _tfidf.Process();
            _tfidf = null;
            GC.Collect();
            Console.WriteLine( "end process TFiDF" );
            #endregion

            #region [.-3-.]
            Console.Write( "start write result..." );
            var outputFolder = Path.Combine( bp.OutputBaseFolder, inputFolder4Language );
            if ( !Directory.Exists( outputFolder ) )
            {
                Directory.CreateDirectory( outputFolder );
            }

            var fileNamesCount = fileNames.Count;
            var sws = new List< StreamWriter >( fileNamesCount );
            for ( var i = 0; i < fileNamesCount; i++ )
            {
                var fn = fileNames[ i ];
                var fi = new FileInfo( Path.Combine( outputFolder, fn ) );
                var outputFile = Path.Combine( fi.DirectoryName, fi.Name.Substring( 0, fi.Name.Length - fi.Extension.Length ) +
                                               "-(" + bp.Ngrams + "-" + bp.CutThreshold + ")" + fi.Extension );

                var sw = new StreamWriter( outputFile, false, Config.Inst.OUTPUT_ENCODING );
                sw.WriteLine( "#\t'" + fn + "' (" + bp.Ngrams + "-" + bp.CutThreshold + ")" );
                sws.Add( sw );
            }

            var nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
            
            for ( int i = 0, len = _tfidf_result.TFiDF.Length; i < len; i++ )
            {
                var values = _tfidf_result.TFiDF[ i ];
                //if ( values.AllValuesAreEquals() ) continue;
                var w = _tfidf_result.Words[ i ];
                for ( int j = 0; j < fileNamesCount; j++ )
                {
                    var v = values[ j ];
                    if ( v != 0 )
                    {
                        var sw = sws[ j ];
                        sw.Write( w );
                        sw.Write( '\t' );
                        sw.WriteLine( v.ToString( nfi ) );
                    }
                }
            }

            sws.ForEach( sw => { sw.Close(); sw.Dispose(); } );

            var tempOutputFolder = Path.Combine( folder4Language, "temp" );
            if ( Directory.Exists( tempOutputFolder ) )
            {
                Directory.Delete( tempOutputFolder, true );
            }

            Console.WriteLine( "end write result" + Environment.NewLine );            
            #endregion
        }
        private static void BuildTFMatrix_UsePortion( build_params_t bp, FileInfo fi, tfidf _tfidf, bool clearCyrillicsChars )
        {
            var portioner = new portioner_t( bp, fi, _tfidf );

            portioner.BuildTFMatrix_UsePortion( clearCyrillicsChars );

            portioner = null;
            GC.Collect();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static string DisplaySize( this FileInfo fileInfo )
        {
            const float KILOBYTE = 1024;
            const float MEGABYTE = KILOBYTE * KILOBYTE;
            const float GIGABYTE = MEGABYTE * KILOBYTE;

            if ( fileInfo == null )
                return ("NULL");

            if ( GIGABYTE < fileInfo.Length )
                return ( (fileInfo.Length / GIGABYTE).ToString("N2") + " GB");
            if ( MEGABYTE < fileInfo.Length )
                return ( (fileInfo.Length / MEGABYTE).ToString("N2") + " MB");
            if ( KILOBYTE < fileInfo.Length )
                return ( (fileInfo.Length / KILOBYTE).ToString("N2") + " KB");
            return (fileInfo.Length.ToString("N0") + " bytes");
        }

        unsafe public static bool HasCyrillicsChars( this string value )
        {
            fixed ( char* _base = value )
            {
                for ( var ptr = _base; ; ptr++ )
                {
                    var ch = *ptr;
                    switch ( ch )
                    {
                        case '\0':
                            return (false);
                        case 'Ё':
                        case 'ё':
                            return (true);
                        default:
                            if ( 'А' <= ch && ch <= 'я' )
                                return (true);
                        break;    
                    }
                }
            }
        }

        public static IEnumerable< Tuple< NGramsEnum, CutThresholdEnum > > GetProcessParams()
        {
            for ( var ngarms = NGramsEnum.ngram_1; ngarms <= NGramsEnum.ngram_2; ngarms++ )
            {
                for ( var d = CutThresholdEnum.cut_1; d <= CutThresholdEnum.cut_2; d++ )
                {
                    yield return (Tuple.Create( ngarms, d ));
                }
            }
        }

        /*public static bool AllValuesAreEquals( this float[] values )
        {
            var v1 = values[ 0 ];
            for ( int i = 1, len = values.Length; i < len; i++ )
            {
                if ( v1 != values[ i ] )
                {
                    return (false);
                }
            }
            return (true);
        }*/
    }
}
