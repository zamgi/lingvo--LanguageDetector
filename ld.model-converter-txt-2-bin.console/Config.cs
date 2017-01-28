using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

using lingvo.ld.MultiLanguage;

namespace lingvo.ld.modelconverter
{
    /// <summary>
    /// 
    /// </summary>
    internal interface IConfig
    {
        MModelConfig GetModelConfig();
        string LANGUAGE_MODELS_FOLDER { get; }
        int    ML_MODEL_DICTIONARY_CAPACITY { get; }
        string OUTPUT_FILE_NAME { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config : IConfig
    {
        private string _LANGUAGE_MODELS_FOLDER;
        private int    _ML_MODEL_DICTIONARY_CAPACITY;
        private string _OUTPUT_FILE_NAME;
        private int    _OUTPUT_FILE_SIZE_IN_BYTES;
        private string   _BINARY_MODEL_FOLDER;
        private string[] _BINARY_MODEL_FILE_NAMES;

        private Config()
        {
            _LANGUAGE_MODELS_FOLDER       = ConfigurationManager.AppSettings[ "LANGUAGE_MODELS_FOLDER" ];
            _ML_MODEL_DICTIONARY_CAPACITY = int.Parse( ConfigurationManager.AppSettings[ "ML_MODEL_DICTIONARY_CAPACITY" ] );
            _OUTPUT_FILE_NAME             = ConfigurationManager.AppSettings[ "OUTPUT_FILE_NAME" ];

            var v = ConfigurationManager.AppSettings[ "OUTPUT_FILE_SIZE_IN_BYTES" ];
            var n = 0;
            if ( int.TryParse( v, out n ) )
            {
                _OUTPUT_FILE_SIZE_IN_BYTES = n;
            }

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

        public string LANGUAGE_MODELS_FOLDER { get { return (_LANGUAGE_MODELS_FOLDER); } }
        public int    ML_MODEL_DICTIONARY_CAPACITY { get { return (_ML_MODEL_DICTIONARY_CAPACITY); } }
        public string OUTPUT_FILE_NAME { get { return (_OUTPUT_FILE_NAME); } }
        public int    OUTPUT_FILE_SIZE_IN_BYTES { get { return (_OUTPUT_FILE_SIZE_IN_BYTES); } }

        private IEnumerable< LanguageConfig > GetModelLanguageConfigs()
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
        public MModelConfig GetModelConfig()
        {
            var modelConfig = new MModelConfig() { ModelDictionaryCapacity = this.ML_MODEL_DICTIONARY_CAPACITY };
            foreach ( var languageConfig in this.GetModelLanguageConfigs() )
            {
                modelConfig.AddLanguageConfig( languageConfig );
            }
            return (modelConfig);
        }

        public MModelBinaryNativeConfig GetModelBinaryNativeConfig()
        {
            var modelConfig = new MModelBinaryNativeConfig( _BINARY_MODEL_FILE_NAMES ) 
            { 
                ModelDictionaryCapacity = this.ML_MODEL_DICTIONARY_CAPACITY 
            };
            return (modelConfig);
        }
    }
}
