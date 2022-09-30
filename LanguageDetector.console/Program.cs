using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;

using lingvo.urls;
using lingvo.ld.MultiLanguage;

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
        private string   _BINARY_MODEL_FOLDER;
        private string[] _BINARY_MODEL_FILE_NAMES;

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
        public MModelConfig GetMModelConfig()
        {
            var modelConfig = new MModelConfig() { ModelDictionaryCapacity = this.ML_MODEL_DICTIONARY_CAPACITY };
            foreach ( var languageConfig in this.GetModelLanguageConfigs() )
            {
                modelConfig.AddLanguageConfig( languageConfig );
            }
            return (modelConfig);
        }

        public MModelBinaryNativeConfig GetMModelBinaryNativeConfig() => new MModelBinaryNativeConfig( _BINARY_MODEL_FILE_NAMES ) { ModelDictionaryCapacity = ML_MODEL_DICTIONARY_CAPACITY };

        public MDetectorConfig GetMDetectorConfig() => new MDetectorConfig()
        {
            UrlDetectorModel                   = new UrlDetectorModel( URL_DETECTOR_RESOURCES_XML_FILENAME ),
            ThresholdPercent                   = ML_THRESHOLD_PERCENT,
            ThresholdPercentBetween3Language   = ML_THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE,
            ThresholdDetectingWordCount        = ML_THRESHOLD_DETECTING_WORD_COUNT,
            ThresholdPercentDetectingWordCount = ML_THRESHOLD_PERCENT_DETECTING_WORD_COUNT,
            ThresholdAbsoluteWeightLanguage    = ML_THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE,
        };

        /*public ManyLanguageDetectorModel GetManyLanguageDetectorModel( ManyLanguageDetectorModelConfig config ) => new ManyLanguageDetectorModel( config );
        public ManyLanguageDetectorModelConfig GetManyLanguageDetectorModelConfig()
        {
            var config = new ManyLanguageDetectorModelConfig() { ModelDictionaryCapacity = ML_MODEL_DICTIONARY_CAPACITY };
            foreach ( var lang in Languages.All )
            {
                var key = (lang == Language.RU) ? "RU-ML" : lang.ToString();
                var modelFilename = ConfigurationManager.AppSettings[ key ];

                if ( !string.IsNullOrWhiteSpace( modelFilename ) )
                {
                    modelFilename = Path.Combine( LANGUAGE_MODELS_FOLDER, modelFilename );

                    var lconfig = new LanguageConfigAdv( lang, modelFilename );
                    config.AddLanguageConfig( lconfig );
                }
            }
            return (config);
        }*/
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                #region [.GC.]
                GCSettings.LatencyMode = GCLatencyMode.LowLatency;
                if ( GCSettings.LatencyMode != GCLatencyMode.LowLatency )
                {
                    GCSettings.LatencyMode = GCLatencyMode.Batch;
                } 
                #endregion

                Test__MModelBinaryNative();

                //Test__MModelClassic();                
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( "\r\n[.....finita.....]" );
            Console.ReadLine();
        }

        private static void Test__MModelBinaryNative()
        {
            //using ( var m = new MModelDictionaryNativeMMF( Config.Inst.GetMModelConfig() ) )
            //using ( var m = new MModelNativeTextMMF( Config.Inst.GetMModelConfig(), ModelLoadTypeEnum.Consecutively ) ) 
            //{
            //    Console.WriteLine( m.TryGetValue( "XZ", out var q ) );
            //}

            var sw = Stopwatch.StartNew();
            using ( var model = new MModelBinaryNative( Config.Inst.GetMModelBinaryNativeConfig() ) )
            {
                var count = model.RecordCount;
                sw.Stop(); Console.WriteLine( "elapsed: " + sw.Elapsed + ", count: " + count );

                GCCollect();

                var detector = new MDetector( Config.Inst.GetMDetectorConfig(), model );
                var languageInfos = detector.DetectLanguage( "\r\n[.....push enter for continue.....]" );
                Console.WriteLine( string.Join( ", ", languageInfos.Select( i => $"{i.Language}, {i.Weight}, {i.Percent} %" ) ) );

                Console.Write( "disposing language model..." );
            }
            GCCollect();
            Console.WriteLine( "end" );
        }

        private static void Test__MModelClassic()
        {
            var sw = Stopwatch.StartNew();            
            using ( var model = new MModelClassic( Config.Inst.GetMModelConfig() ) )
            {
                var count = model.RecordCount;
                sw.Stop();

                GCCollect();
                Console.WriteLine( "elapsed: " + sw.Elapsed + ", count: " + count );

                Console.ForegroundColor = ConsoleColor.DarkGray; Console.WriteLine( "\r\n[.....push enter for continue.....]" ); Console.ResetColor();
                Console.ReadLine();

                //*
                var detector = new MDetector( Config.Inst.GetMDetectorConfig(), model );
                var languageInfos = detector.DetectLanguage( "\r\n[.....push enter for continue.....]" );
                //*/

                Console.Write( "disposing language model..." );
            }
            GCCollect();
            Console.WriteLine( "end" );
        }

        private static void GCCollect()
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            GC.WaitForPendingFinalizers();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }
    }
}
