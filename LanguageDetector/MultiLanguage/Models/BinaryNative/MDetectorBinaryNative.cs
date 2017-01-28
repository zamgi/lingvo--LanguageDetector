
namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    unsafe public class MDetectorBinaryNative : MDetector
    {
        #region [.private field's.]
        private MModelBinaryNative _ModelBinaryNative;
        #endregion

        #region [.ctor().]
        public MDetectorBinaryNative( MDetectorConfig config, MModelBinaryNative model ) : base( config, model )
        {
            _ModelBinaryNative = model;
        }
        #endregion

        #region [.protected override method's.]
        unsafe protected override void ProcessTermCallback( string term )
        {
            //-1-grams-
            var termDetecting = 0;
            MModelBinaryNative.WeighByLanguageNative wbln;
            if ( _ModelBinaryNative.TryGetValue( term, out wbln ) )
	        {
                termDetecting = 1;

                for ( var n = 0; n < wbln.CountBuckets; n++ )
                {
                    var ptr = &wbln.WeighByLanguagesBasePtr[ n ];
                    var i = (int) ptr->Language;
                    *(_WeightsPtrBase             + i) += ptr->Weight;
                    *(_TermCountByLanguagePtrBase + i) += 1;
                }
	        }

            //-2-grams-
            var ngram_2 = _NgramStringBuilder.Clear().Append( _TermPrevious ).Append( SPACE ).Append( term ).ToString();
            if ( _ModelBinaryNative.TryGetValue( ngram_2, out wbln ) )
            {
                termDetecting = 1;

                for ( var n = 0; n < wbln.CountBuckets; n++ )
                {
                    var ptr = &wbln.WeighByLanguagesBasePtr[ n ];
                    var i = (int) ptr->Language;
                    *(_WeightsPtrBase             + i) += ptr->Weight;
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
