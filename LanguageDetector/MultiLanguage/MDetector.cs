using System;
using System.Collections.Generic;
using System.Text;

using lingvo.core;
using lingvo.tokenizing;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    unsafe public class MDetector : ILanguageDetector
    {
        #region [.private field's.]
        protected const  char     SPACE                              = ' ';
        private   const  int      THREE_LANGUAGE                     = 3;
        private   const  int      LANGUAGES_COUNT                    = (int) Language.LENGTH;
        private   const  float    NULL_WEIGHT                        = 0.0f;        
        private   const  int      NGRAM_STRINGBUILDER_DEFAULT_LENGTH = 100;
        private   static readonly LanguageInfo[] LANGUAGEINFO_EMPTY  = new LanguageInfo[ 0 ];

        protected readonly IMModel            _Model;
        protected float*                      _WeightsPtrBase;
        protected int*                        _TermCountByLanguagePtrBase;
        protected int                         _TermCount;
        protected int                         _TermCountDetecting;
        protected readonly StringBuilder      _NgramStringBuilder;
        protected string                      _TermPrevious;

        private readonly mld_tokenizer        _Tokenizer;
        private Action< string >              _ProcessTermCallbackAction;
        private readonly float[]              _Weights;
        private readonly int[]                _TermCountByLanguage;
        private readonly List< LanguageInfo > _LanguageInfos;
        #endregion

        #region [.ctor().]
        public MDetector( MDetectorConfig config, IMModel model )
        {
            config.ThrowIfNull("config");            
            config.UrlDetectorModel.ThrowIfNull( "config.UrlDetectorModel" );
            model.ThrowIfNull( "model" );

            _Model                             = model;
            ThresholdPercent                   = config.ThresholdPercent;
            ThresholdPercentBetween3Language   = config.ThresholdPercentBetween3Language;
            ThresholdDetectingWordCount        = config.ThresholdDetectingWordCount;
            ThresholdPercentDetectingWordCount = config.ThresholdPercentDetectingWordCount;
            ThresholdAbsoluteWeightLanguage    = config.ThresholdAbsoluteWeightLanguage;
            _Tokenizer                         = new mld_tokenizer( config.UrlDetectorModel );            
            _Weights                           = new float[ LANGUAGES_COUNT ];
            _TermCountByLanguage               = new int  [ LANGUAGES_COUNT ];
            _LanguageInfos                     = new List< LanguageInfo >( LANGUAGES_COUNT );
            _NgramStringBuilder                = new StringBuilder( NGRAM_STRINGBUILDER_DEFAULT_LENGTH );
            _ProcessTermCallbackAction         = new Action< string >( ProcessTermCallback );
        }
        #endregion

        #region [.properties.]
        private int   _ThresholdPercent;
        private int   _ThresholdPercentBetween3Language;
        private int   _ThresholdDetectingWordCount;
        private int   _ThresholdPercentDetectingWordCount;
        private float _ThresholdAbsoluteWeightLanguage;

        public int   ThresholdPercent
        {
            get { return (_ThresholdPercent); }
            set
            {
                if (value < 0 || 100 < value)
                    throw (new ArgumentException("ThresholdPercent"));

                _ThresholdPercent = value;
            }
        }
        public int   ThresholdPercentBetween3Language
        {
            get { return (_ThresholdPercentBetween3Language); }
            set
            {
                if (value < 0 || 100 < value)
                    throw (new ArgumentException("ThresholdPercentBetween3Language"));

                _ThresholdPercentBetween3Language = value;
            }
        }
        public int   ThresholdDetectingWordCount
        {
            get { return (_ThresholdDetectingWordCount); }
            set
            {
                if ( value < 0 )
                    throw (new ArgumentException( "ThresholdDetectingWordCount" ));

                _ThresholdDetectingWordCount = value;
            }
        }
        public int   ThresholdPercentDetectingWordCount
        {
            get { return (_ThresholdPercentDetectingWordCount); }
            set
            {
                if ( value < 0 || 100 < value )
                    throw (new ArgumentException( "ThresholdPercentDetectingWordCount" ));

                _ThresholdPercentDetectingWordCount = value;
            }
        }
        public float ThresholdAbsoluteWeightLanguage
        {
            get { return (_ThresholdAbsoluteWeightLanguage); }
            set
            {
                if ( value < 0.0 || 100.0 < value )
                    throw (new ArgumentException( "ThresholdAbsoluteWeightLanguage" ));

                _ThresholdAbsoluteWeightLanguage = value;
            }
        }
        #endregion

        #region [.ILanguageDetector.]
        public LanguageInfo[] DetectLanguage( string text )
        {
            if ( text.IsNullOrWhiteSpace() )
                return (LANGUAGEINFO_EMPTY);
            
            var languageInfos = ProcessWithTermEnumerable( text );
            return (languageInfos);
        }
        #endregion

        #region [.private method's.]
        unsafe private LanguageInfo[] ProcessWithTermEnumerable( string text )
        {
            fixed ( float* weightsPtrBase             = _Weights             )
            fixed ( int  * termCountByLanguagePtrBase = _TermCountByLanguage )
            {
                //-1-
                #region [.main phase.]
                _WeightsPtrBase             = weightsPtrBase;
                _TermCountByLanguagePtrBase = termCountByLanguagePtrBase;
                _TermCount                  = 0;
                _TermCountDetecting         = 0;

                //zeroize
                for ( var i = 0; i < LANGUAGES_COUNT; i++ )
                {
                    weightsPtrBase[ i ]             = 0;
                    termCountByLanguagePtrBase[ i ] = 0;
                }

                _Tokenizer.run( text, _ProcessTermCallbackAction );
                _TermPrevious = null;

                for ( var i = 0; i < LANGUAGES_COUNT; i++ )
                {
                    var weightPtr = weightsPtrBase + i;
                    *weightPtr = (*weightPtr * termCountByLanguagePtrBase[ i ]) / _TermCount;
                }
                #endregion

                //-2-
                #region [.form result.]
                //если в тексте более чем из 9 слов определилось не более 10% слов, то отбрасывать его в unk.
                if ( (_ThresholdDetectingWordCount < _TermCount) &&
                     ((100 * _TermCountDetecting / _TermCount) < _ThresholdPercentDetectingWordCount)
                   )
                {
                    return (LANGUAGEINFO_EMPTY);
                }

                var weightsSum = default(float);
                for ( var i = 0; i < LANGUAGES_COUNT; i++ )
                {
                    weightsSum += weightsPtrBase[ i ];
                }

                if ( weightsSum == NULL_WEIGHT )
                {
                    return (LANGUAGEINFO_EMPTY);
                }

                var aspect = 100 / weightsSum;
                for ( var i = 0; i < LANGUAGES_COUNT; i++ )
                {
                    var weight  = weightsPtrBase[ i ];
                    if ( weight < _ThresholdAbsoluteWeightLanguage )
                        continue;
				    var percent = (int)(weight * aspect); //Convert.ToInt32( weight * aspect );
                    if ( percent < _ThresholdPercent )
                        continue;

                    var language = ((Language) i);

                    _LanguageInfos.Add( new LanguageInfo( language, weight, percent ) );
                }

                _LanguageInfos.Sort( LanguageInfoComparer.Instance );

                //Если 3 и более языков, начиная с первого отличаются не более чем на 8% между собой, то это либо неизвестный язык, либо сильно смешенный – отбрасывать в unk. 
                if ( THREE_LANGUAGE <= _LanguageInfos.Count )
                {
                    var p1 = _LanguageInfos[ 0 ].Percent;
                    var p2 = _LanguageInfos[ 1 ].Percent;
                    if ( (p1 - p2) <= _ThresholdPercentBetween3Language )
                    {
                        var p3 = _LanguageInfos[ 2 ].Percent;
                        if ( (p2 - p3) <= _ThresholdPercentBetween3Language )
                        {
                            return (LANGUAGEINFO_EMPTY);
                        }
                    }
                }

                if ( 0 < _LanguageInfos.Count )
                {
                    var resultLanguageInfos = _LanguageInfos.ToArray();
                    _LanguageInfos.Clear();
                    return (resultLanguageInfos);
                }
                return (LANGUAGEINFO_EMPTY);
                #endregion
            }
        }

        unsafe protected virtual void ProcessTermCallback( string term )
        {
            //-1-grams-
            var termDetecting = 0;
            IEnumerable< WeighByLanguage > weighByLanguages;
            if ( _Model.TryGetValue( term, out weighByLanguages ) )
	        {
                termDetecting = 1;

                foreach ( var t in weighByLanguages )
                {
                    var i = (int) t.Language;
                    *(_WeightsPtrBase             + i) += t.Weight;
                    *(_TermCountByLanguagePtrBase + i) += 1;
                }
	        }

            //-2-grams-
            var ngram_2 = _NgramStringBuilder.Clear().Append( _TermPrevious ).Append( SPACE ).Append( term ).ToString();
            if ( _Model.TryGetValue( ngram_2, out weighByLanguages ) )
            {
                termDetecting = 1;

                foreach ( var t in weighByLanguages )
                {
                    var i = (int) t.Language;
                    *(_WeightsPtrBase             + i) += t.Weight;
                    *(_TermCountByLanguagePtrBase + i) += 1;
                }
            }
            _TermPrevious = term;

            _TermCount++;
            _TermCountDetecting += termDetecting;
        }
        #endregion
    }
}
