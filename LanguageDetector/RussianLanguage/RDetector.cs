using System;
using System.Text;

using lingvo.core;
using lingvo.tokenizing;

namespace lingvo.ld.RussianLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RDetector : ILanguageDetector
    {
        #region [.private field's.]
        private const float NULL_WEIGHT = 0.0f;
        private static readonly LanguageInfo[] LANGUAGEINFO_EMPTY = new LanguageInfo[ 0 ];

        private readonly IRModel       _Model;
        private readonly mld_tokenizer _Tokenizer;
        private readonly StringBuilder _Buf;
        #endregion

        #region [.ctor().]
        public RDetector( RDetectorConfig config, IRModel model )
        {
            config.ThrowIfNull( nameof(config) );
            model.ThrowIfNull( nameof(model) );
            config.UrlDetectorModel.ThrowIfNull( nameof(config.UrlDetectorModel) );

            Threshold              = config.Threshold;
            CyrillicLettersPercent = config.CyrillicLettersPercent;
            _Model                 = model;
            _Tokenizer             = new mld_tokenizer( config.UrlDetectorModel );
            _Buf                   = new StringBuilder( 50 );
        }
        public void Dispose() => _Tokenizer.Dispose();
        #endregion

        #region [.properties.]
        private float _Threshold;
        private int _CyrillicLettersPercent;

        public float Threshold
        {
            get => _Threshold;
            set => _Threshold = value;
        }
        public int   CyrillicLettersPercent
        {
            get => _CyrillicLettersPercent;
            set 
            {
                if ( value < 0 || 100 < value ) throw (new ArgumentException( nameof(CyrillicLettersPercent) ));
                _CyrillicLettersPercent = value;
            }
        }
        #endregion

        #region [.ILanguageDetector.]
#if DEBUG
        public LanguageInfo[] DetectLanguage( string text )
        {
            if ( text.IsNullOrWhiteSpace() )
                return (null);

            var weight = ProcessInternal( text, out var _ );
            if ( weight < _Threshold )
                return (LANGUAGEINFO_EMPTY);

            var languageInfo = new LanguageInfo( Language.RU, weight );
            return (new[] { languageInfo });            
        }
#else
        public LanguageInfo[] DetectLanguage( string text )
        {
            if ( text.IsNullOrWhiteSpace() )
                return (null);

            var weight = ProcessInternal( text );
            if ( weight == NULL_WEIGHT || weight < _Threshold )
                return (LANGUAGEINFO_EMPTY);

            var languageInfo = new LanguageInfo( Language.RU, weight );
            return (new[] { languageInfo });            
        }
#endif
        #endregion

        #region [.public method's.]
#if DEBUG
        public LanguageInfo DetectLanguageRU( string text )
        {
            if ( text.IsNullOrWhiteSpace() )
                return (null);

            var weight = ProcessInternal( text, out var _ );
            if ( weight == NULL_WEIGHT || weight < _Threshold )
                return (null);

            return (new LanguageInfo( Language.RU, weight ));
        }
        public LanguageInfo DetectLanguageRU( string text, out int cyrillicLettersPercent )
        {
            if ( text.IsNullOrWhiteSpace() )
            {
                cyrillicLettersPercent = (int)(NULL_WEIGHT); //Convert.ToInt32( NULL_WEIGHT );
                return (null);
            }

            var weight = ProcessInternal( text, out cyrillicLettersPercent );
            if ( weight == NULL_WEIGHT || weight < _Threshold )
                return (null);

            return (new LanguageInfo( Language.RU, weight ));
        }
#else
        public LanguageInfo DetectLanguageRU( string text )
        {
            if ( text.IsNullOrWhiteSpace() )
                return (null);

            var weight = ProcessInternal( text );
            if ( weight == NULL_WEIGHT || weight < _Threshold )
                return (null);

            return (new LanguageInfo( Language.RU, weight ));
        }
#endif
        #endregion

        #region [.private method's.]
#if DEBUG
        private float ProcessInternal( string text, out int cyrillicLettersPercent )
#else
        private float ProcessInternal( string text )
#endif
        {            
#if DEBUG
            var hasCyrillicLetters = rld_tokenizer.HasCyrillicLetters( text, _CyrillicLettersPercent, out cyrillicLettersPercent );
#else
            var hasCyrillicLetters = rld_tokenizer.HasCyrillicLetters( text, _CyrillicLettersPercent );
#endif
            if ( !hasCyrillicLetters )
            {
                return (NULL_WEIGHT);
            }

            var terms = _Tokenizer.Run( text );
            if ( terms.Count == 0 )
            {
                return (NULL_WEIGHT);
            }

            var termCount          = 0;
            var termCountInHashset = 0;
            var termPrevious = terms[ 0 ];
            if ( _Model.Contains( termPrevious ) )
            {
                termCountInHashset++;
            }
            for ( int i = 1, len = terms.Count; i < len; i++ )
            {
                var term = terms[ i ];
                if ( _Model.Contains( term ) )
                {
                    termCountInHashset++;
                }
                var ngram_2 = _Buf.Clear().Append( termPrevious ).Append( ' ' ).Append( term ).ToString();
                if ( _Model.Contains( ngram_2 ) )
                {
                    termCountInHashset++;
                }
                termPrevious = term;

                termCount++;
            }
			
			if ( termCount == 0 )
			{
				return (NULL_WEIGHT);
			}			

            var totalWeight = (1.0f * termCountInHashset) / termCount;
            return (totalWeight);
        }
        #endregion
    }
}
