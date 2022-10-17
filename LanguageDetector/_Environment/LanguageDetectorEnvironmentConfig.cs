using System.Collections.Generic;

using lingvo.ld.MultiLanguage;
using lingvo.ld.RussianLanguage;
using lingvo.urls;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILanguageDetectorEnvironmentConfig
    {
        string URL_DETECTOR_RESOURCES_XML_FILENAME { get; }
        string LANGUAGE_MODELS_FOLDER              { get; }

        int    ML_THRESHOLD_PERCENT                      { get; }
        int    ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE   { get; }
        int    ML_THRESHOLD_DETECTING_WORD_COUNT         { get; }
        int    ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT { get; }
        float  ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE     { get; }
        int    ML_MODEL_DICTIONARY_CAPACITY              { get; }

        int    RU_CYRILLIC_LETTERS_PERCENT { get; }
        float  RU_THRESHOLD                { get; }

        string BINARY_MODEL_FOLDER { get; }
        IReadOnlyList< string > BINARY_MODEL_FILE_NAMES { get; }


        RDetectorConfig GetRDetectorConfig();

        MModelConfig GetMModelConfig();
        MDetectorConfig GetMDetectorConfig();        
        MModelBinaryNativeConfig GetMModelBinaryNativeConfig();        
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class LanguageDetectorEnvironmentConfigBase : ILanguageDetectorEnvironmentConfig
    {
        public abstract string URL_DETECTOR_RESOURCES_XML_FILENAME { get; }
        public abstract string LANGUAGE_MODELS_FOLDER              { get; }

        public abstract int    ML_THRESHOLD_PERCENT                      { get; }
        public abstract int    ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE   { get; }
        public abstract int    ML_THRESHOLD_DETECTING_WORD_COUNT         { get; }
        public abstract int    ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT { get; }
        public abstract float  ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE     { get; }
        public abstract int    ML_MODEL_DICTIONARY_CAPACITY              { get; }

        public abstract int    RU_CYRILLIC_LETTERS_PERCENT { get; }
        public abstract float  RU_THRESHOLD                { get; }

        public abstract string BINARY_MODEL_FOLDER { get; }
        public abstract IReadOnlyList< string > BINARY_MODEL_FILE_NAMES { get; }


        #region [.RussianLanguage.]
        protected abstract LanguageConfig GetRModelConfig();
        public RDetectorConfig GetRDetectorConfig() => new RDetectorConfig()
        {
            UrlDetectorModel       = new UrlDetectorModel( URL_DETECTOR_RESOURCES_XML_FILENAME ),
            CyrillicLettersPercent = RU_CYRILLIC_LETTERS_PERCENT,
            Threshold              = RU_THRESHOLD,
        };        
        #endregion

        #region [.MultiLanguage.]
        protected abstract IEnumerable< LanguageConfig > GetModelLanguageConfigs();
        public MModelConfig GetMModelConfig()
        {
            var modelConfig = new MModelConfig() { ModelDictionaryCapacity = this.ML_MODEL_DICTIONARY_CAPACITY };
            foreach ( var languageConfig in this.GetModelLanguageConfigs() )
            {
                modelConfig.AddLanguageConfig( languageConfig );
            }
            return (modelConfig);
        }
        public MModelBinaryNativeConfig GetMModelBinaryNativeConfig() => new MModelBinaryNativeConfig( BINARY_MODEL_FILE_NAMES ) { ModelDictionaryCapacity = ML_MODEL_DICTIONARY_CAPACITY };
        public MDetectorConfig GetMDetectorConfig() => new MDetectorConfig()
        {
            UrlDetectorModel                   = new UrlDetectorModel( URL_DETECTOR_RESOURCES_XML_FILENAME ),
            ThresholdPercent                   = ML_THRESHOLD_PERCENT,
            ThresholdPercentBetween3Language   = ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE,
            ThresholdDetectingWordCount        = ML_THRESHOLD_DETECTING_WORD_COUNT,
            ThresholdPercentDetectingWordCount = ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT,
            ThresholdAbsoluteWeightLanguage    = ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE,
        };
        #endregion
    }
}
