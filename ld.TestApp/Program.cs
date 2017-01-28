using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;

using lingvo.ld.MultiLanguage;
using lingvo.ld.v1;
using lingvo.urls;

namespace lingvo.ld.TestApp
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config
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
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            #region [.GC.]
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
            if ( GCSettings.LatencyMode != GCLatencyMode.LowLatency )
            {
                GCSettings.LatencyMode = GCLatencyMode.Batch;
            } 
            #endregion

            var sw = Stopwatch.StartNew();
            var modelConfig = GetManyLanguageDetectorModelConfig();
            var model = GetManyLanguageDetectorModel( modelConfig );
            var c = model.DictionaryNative.Count;
            sw.Stop();

                GCCollect();
            Console.WriteLine( "elapsed: " + sw.Elapsed + ", c: " + c );
            Console.ForegroundColor = ConsoleColor.DarkGray; Console.WriteLine( "\r\n[.....push enter for continue.....]" ); Console.ResetColor();
            Console.ReadLine();

            Console.Write( "disposing language model..." );
            model.Dispose();
            model = null;
            modelConfig = null;
                GCCollect();
            Console.WriteLine( "end" );

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( "\r\n[.....finita.....]" );
            Console.ReadLine();
        }

        private static ManyLanguageDetectorModel GetManyLanguageDetectorModel( ManyLanguageDetectorModelConfig config )
        {
            var model = new ManyLanguageDetectorModel( config );
            return (model);
        }
        private static ManyLanguageDetectorModelConfig GetManyLanguageDetectorModelConfig()
        {
            var config = new ManyLanguageDetectorModelConfig() { ModelDictionaryCapacity = Config.Inst.ML_MODEL_DICTIONARY_CAPACITY };
            foreach ( var lang in Languages.All )
            {
                var key = (lang == Language.RU) ? "RU-ML" : lang.ToString();
                var modelFilename = ConfigurationManager.AppSettings[ key ];

                if ( !string.IsNullOrWhiteSpace( modelFilename ) )
                {
                    modelFilename = Path.Combine( Config.Inst.LANGUAGE_MODELS_FOLDER, modelFilename );

                    var lconfig = new LanguageConfigAdv( lang, modelFilename );
                    config.AddLanguageConfig( lconfig );
                }
            }
            return (config);
        }

        private static MDetectorConfig GetMultiLanguageDetectorConfig()
        {
            var config = new MDetectorConfig()
            {
                UrlDetectorModel                   = new UrlDetectorModel( Config.Inst.URL_DETECTOR_RESOURCES_XML_FILENAME ),
                ThresholdPercent                   = Config.Inst.ML_THRESHOLD_PERCENT,
                ThresholdPercentBetween3Language   = Config.Inst.ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE,
                ThresholdDetectingWordCount        = Config.Inst.ML_THRESHOLD_DETECTING_WORD_COUNT,
                ThresholdPercentDetectingWordCount = Config.Inst.ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT,
                ThresholdAbsoluteWeightLanguage    = Config.Inst.ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE,
            };
            return (config);
        }

        private static void GCCollect()
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            GC.WaitForPendingFinalizers();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }
    }
}
