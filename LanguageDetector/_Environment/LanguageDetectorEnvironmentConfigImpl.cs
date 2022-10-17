using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public class LanguageDetectorEnvironmentConfigImpl : LanguageDetectorEnvironmentConfigBase
    {
        public LanguageDetectorEnvironmentConfigImpl()
        {
            var nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
            var ns  = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;

            URL_DETECTOR_RESOURCES_XML_FILENAME = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];
            LANGUAGE_MODELS_FOLDER              = ConfigurationManager.AppSettings[ "LANGUAGE_MODELS_FOLDER" ];

            ML_THRESHOLD_PERCENT                      = int.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_PERCENT" ] );
            ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE   = int.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE" ] );
            ML_THRESHOLD_DETECTING_WORD_COUNT         = int.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_DETECTING_WORD_COUNT" ] );
            ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT = int.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT" ] );
            ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE     = float.Parse( ConfigurationManager.AppSettings[ "ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE" ], ns, nfi );
            ML_MODEL_DICTIONARY_CAPACITY              = int.Parse( ConfigurationManager.AppSettings[ "ML_MODEL_DICTIONARY_CAPACITY" ] );

            RU_CYRILLIC_LETTERS_PERCENT = int.Parse( ConfigurationManager.AppSettings[ "RU_CYRILLIC_LETTERS_PERCENT" ] );
            RU_THRESHOLD                = float.Parse( ConfigurationManager.AppSettings[ "RU_THRESHOLD" ], ns, nfi );

            BINARY_MODEL_FOLDER = ConfigurationManager.AppSettings[ "BINARY_MODEL_FOLDER" ] ?? string.Empty;
            var bmfns = ConfigurationManager.AppSettings[ "BINARY_MODEL_FILE_NAMES" ] ?? string.Empty;
            BINARY_MODEL_FILE_NAMES = (from fn in bmfns.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
                                        let fileName = fn.Trim()
                                        where (!string.IsNullOrEmpty( fileName ))
                                        select Path.Combine( BINARY_MODEL_FOLDER, fileName )
                                       ).ToList();
        }

        public override string URL_DETECTOR_RESOURCES_XML_FILENAME { get; }
        public override string LANGUAGE_MODELS_FOLDER              { get; }

        public override int ML_THRESHOLD_PERCENT                      { get; }
        public override int ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE   { get; }
        public override int ML_THRESHOLD_DETECTING_WORD_COUNT         { get; }
        public override int ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT { get; }
        public override float ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE   { get; }
        public override int ML_MODEL_DICTIONARY_CAPACITY              { get; }

        public override int   RU_CYRILLIC_LETTERS_PERCENT { get; }
        public override float RU_THRESHOLD                { get; }

        public override string BINARY_MODEL_FOLDER { get; }
        public override IReadOnlyList< string > BINARY_MODEL_FILE_NAMES { get; }

        #region [.RussianLanguage.]
        protected override LanguageConfig GetRModelConfig()
        {
            var modelFilename = Path.Combine( LANGUAGE_MODELS_FOLDER, ConfigurationManager.AppSettings[ "RU-RU" ] );
            var modelConfig   = new LanguageConfig( Language.RU, modelFilename );
            return (modelConfig);
        }
        #endregion

        #region [.MultiLanguage.]
        protected override IEnumerable< LanguageConfig > GetModelLanguageConfigs()
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
        #endregion
    }
}
