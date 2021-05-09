using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

using lingvo.ld.MultiLanguage;
using lingvo.ld.RussianLanguage;
using lingvo.urls;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Config
    {        
        private Config()
        {
            var NFI = new NumberFormatInfo() { NumberDecimalSeparator = "." };
            var NS  = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;

            URL_DETECTOR_RESOURCES_XML_FILENAME       = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];
            LANGUAGE_MODELS_FOLDER                    = ConfigurationManager.AppSettings[ "LANGUAGE_MODELS_FOLDER" ];
            ML_THRESHOLD_PERCENT                      = int.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_PERCENT" ] );
            ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE   = int.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE" ] );
            ML_THRESHOLD_DETECTING_WORD_COUNT         = int.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_DETECTING_WORD_COUNT" ] );
            ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT = int.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT" ] );
            ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE     = float.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE" ], NS, NFI );
            ML_MODEL_DICTIONARY_CAPACITY              = int.Parse( ConfigurationManager.AppSettings[ "ML_MODEL_DICTIONARY_CAPACITY" ] );
            RU_CYRILLIC_LETTERS_PERCENT               = int.Parse( ConfigurationManager.AppSettings[ "RU_CYRILLIC_LETTERS_PERCENT" ] );
            RU_THRESHOLD                              = float.Parse( ConfigurationManager.AppSettings[ "RU_THRESHOLD" ], NS, NFI );

            MAX_INPUTTEXT_LENGTH                      = int.Parse( ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH" ] );
            CONCURRENT_FACTORY_INSTANCE_COUNT         = int.Parse( ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ] );

            _BINARY_MODEL_FOLDER = ConfigurationManager.AppSettings[ "BINARY_MODEL_FOLDER" ] ?? string.Empty;
            var bmfns = ConfigurationManager.AppSettings[ "BINARY_MODEL_FILE_NAMES" ] ?? string.Empty;
            _BINARY_MODEL_FILE_NAMES = (from fn in bmfns.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
                                        let fileName = fn.Trim()
                                        where ( !string.IsNullOrEmpty( fileName ) )
                                        select Path.Combine( _BINARY_MODEL_FOLDER, fileName )
                                       ).ToArray();
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

        public readonly string URL_DETECTOR_RESOURCES_XML_FILENAME;
        public readonly string LANGUAGE_MODELS_FOLDER;
        public readonly int    ML_THRESHOLD_PERCENT;
        public readonly int    ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE;
        public readonly int    ML_THRESHOLD_DETECTING_WORD_COUNT;
        public readonly int    ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT;
        public readonly float  ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE;
        public readonly int    ML_MODEL_DICTIONARY_CAPACITY;
        public readonly int    RU_CYRILLIC_LETTERS_PERCENT;
        public readonly float  RU_THRESHOLD;

        private string         _BINARY_MODEL_FOLDER;
        private string[]       _BINARY_MODEL_FILE_NAMES;

        public readonly int    MAX_INPUTTEXT_LENGTH;
        public readonly int    CONCURRENT_FACTORY_INSTANCE_COUNT;

        #region [.RussianLanguage.]
        public RDetectorConfig GetRDetectorConfig()
        {
            var config = new RDetectorConfig()
            {                                
                UrlDetectorModel       = new UrlDetectorModel( URL_DETECTOR_RESOURCES_XML_FILENAME ),
                CyrillicLettersPercent = RU_CYRILLIC_LETTERS_PERCENT,
                Threshold              = RU_THRESHOLD,
            };
            return (config);
        }
        public LanguageConfig  GetRModelConfig()
        {
            var modelFilename = Path.Combine( LANGUAGE_MODELS_FOLDER, ConfigurationManager.AppSettings[ "RU-RU" ] );
            var modelConfig   = new LanguageConfig( Language.RU, modelFilename );
            return (modelConfig);
        }
        #endregion

        #region [.MultiLanguage.]
        public MDetectorConfig GetMDetectorConfig()
        {
            var config = new MDetectorConfig()
            {
                UrlDetectorModel                   = new UrlDetectorModel( URL_DETECTOR_RESOURCES_XML_FILENAME ),
                ThresholdPercent                   = ML_THRESHOLD_PERCENT,
                ThresholdPercentBetween3Language   = ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE,
                ThresholdDetectingWordCount        = ML_THRESHOLD_DETECTING_WORD_COUNT,
                ThresholdPercentDetectingWordCount = ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT,
                ThresholdAbsoluteWeightLanguage    = ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE,
            };
            return (config);
        }
        private IEnumerable< LanguageConfig > GetMModelLanguageConfigs()
        {
            foreach ( var language in Languages.All )
            {
                var key = (language == Language.RU) ? "RU-ML" : language.ToString();
                var modelFilename = ConfigurationManager.AppSettings[ key ];

                if ( !string.IsNullOrWhiteSpace( modelFilename ) )
                {
                    modelFilename = Path.Combine( LANGUAGE_MODELS_FOLDER, modelFilename );

                    yield return (new LanguageConfig( language, modelFilename ));
                }
            }
        }
        public MModelConfig GetMModelConfig()
        {
            var modelConfig = new MModelConfig() { ModelDictionaryCapacity = this.ML_MODEL_DICTIONARY_CAPACITY };
            foreach ( var languageConfig in this.GetMModelLanguageConfigs() )
            {
                modelConfig.AddLanguageConfig( languageConfig );
            }
            return (modelConfig);
        }
        public MModelBinaryNativeConfig GetMModelBinaryNativeConfig()
        {
            var modelConfig = new MModelBinaryNativeConfig( _BINARY_MODEL_FILE_NAMES ) 
            { 
                ModelDictionaryCapacity = this.ML_MODEL_DICTIONARY_CAPACITY 
            };
            return (modelConfig);
        }
        #endregion
    }
}