using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

using Newtonsoft.Json;
using lingvo.urls;
using lingvo.ld.MultiLanguage;
using lingvo.ld.RussianLanguage;
//using lingvo.ld.v1;

namespace lingvo.ld
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


        /*public ManyLanguageDetectorModelConfig GetManyLanguageDetectorModelConfig()
        {
            var modelConfig = new ManyLanguageDetectorModelConfig() { ModelDictionaryCapacity = ML_MODEL_DICTIONARY_CAPACITY };
            foreach ( var lang in Languages.All )
            {
                var key = (lang == Language.RU) ? "RU-ML" : lang.ToString();
                var modelFilename = ConfigurationManager.AppSettings[ key ];

                if ( !string.IsNullOrWhiteSpace( modelFilename ) )
                {
                    modelFilename = Path.Combine( LANGUAGE_MODELS_FOLDER, modelFilename );

                    var lconfig = new LanguageConfigAdv( lang, modelFilename );
                    modelConfig.AddLanguageConfig( lconfig );
                }
            };
            return (modelConfig);
        }*/

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

    /// <summary>
    /// Summary description for RESTProcessHandler
    /// </summary>
    public class RESTProcessHandler : IHttpHandler
    {
        /// <summary>
        /// 
        /// </summary>
        internal struct result
        {
            /// <summary>
            /// 
            /// </summary>
            public struct language_info
            {
                [JsonProperty(PropertyName="l")] public string language
                {
                    get;
                    set;
                }
                [JsonProperty(PropertyName="n")] public string language_fullname
                {
                    get;
                    set;
                }
                [JsonProperty(PropertyName="p")] public float  percent
                {
                    get;
                    set;
                }
            }

            public result( Exception ex ) : this()
            {
                exception_message = ex.ToString();
            }
            public result( LanguageInfo[] languageInfos ) : this()
            {
                language_infos = (from li in languageInfos
                                  select
                                    new language_info()
                                    {
                                        language          = li.Language.ToString(),
                                        language_fullname = li.Language.ToText(),
                                        percent           = li.Percent,
                                    }
                                 ).ToArray();
            }

            [JsonProperty(PropertyName="langs")]
            public language_info[] language_infos
            {
                get;
                private set;
            }
            [JsonProperty(PropertyName="err")]
            public string          exception_message
            {
                get;
                private set;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private struct http_context_data
        {
            private static readonly object _SyncLock = new object();
            private readonly HttpContext _Context;

            public http_context_data( HttpContext context )
            {
                _Context = context;
            }

            /*private ConcurrentFactory _MultiLanguageConcurrentFactory
            {
                get { return ((ConcurrentFactory) _Context.Cache[ "_MultiLanguageConcurrentFactory" ]); }
                set
                {                    
                    if ( value != null )
                        _Context.Cache[ "_MultiLanguageConcurrentFactory" ] = value;
                    else
                        _Context.Cache.Remove( "_MultiLanguageConcurrentFactory" );
                }
            }
            private ConcurrentFactory _RussianLanguageConcurrentFactory
            {
                get { return ((ConcurrentFactory) _Context.Cache[ "_RussianLanguageConcurrentFactory" ]); }
                set
                {                    
                    if ( value != null )
                        _Context.Cache[ "_RussianLanguageConcurrentFactory" ] = value;
                    else
                        _Context.Cache.Remove( "_RussianLanguageConcurrentFactory" );
                }
            }*/

            private static ConcurrentFactory _MultiLanguageConcurrentFactory;
            private static ConcurrentFactory _RussianLanguageConcurrentFactory;

            public ConcurrentFactory GetMultiLanguageConcurrentFactory()
            {
                var f = _MultiLanguageConcurrentFactory;
                if ( f == null )
                {
                    lock ( _SyncLock )
                    {
                        f = _MultiLanguageConcurrentFactory;
                        if ( f == null )
                        {
                            var config      = Config.Inst.GetMDetectorConfig();
                            var modelConfig = Config.Inst.GetMModelBinaryNativeConfig();
                            var model       = new MModelBinaryNative( modelConfig );
                            /*
                            var modelConfig = Config.Inst.GetManyLanguageDetectorModelConfig();
                            var model       = new ManyLanguageDetectorModel( modelConfig );
                            */

                            f = new ConcurrentFactory( config, model, Config.Inst.CONCURRENT_FACTORY_INSTANCE_COUNT );                            
                            _MultiLanguageConcurrentFactory = f;
                            //GC.KeepAlive( _MultiLanguageConcurrentFactory );
                        }
                    }
                }
                return (f);
            }
            public ConcurrentFactory GetRussianLanguageConcurrentFactory()
            {
                var f = _RussianLanguageConcurrentFactory;
                if ( f == null )
                {
                    lock ( _SyncLock )
                    {
                        f = _RussianLanguageConcurrentFactory;
                        if ( f == null )
                        {
                            var config      = Config.Inst.GetRDetectorConfig();
                            var modelConfig = Config.Inst.GetRModelConfig();
                            var model       = new RModelClassic( modelConfig );

                            f = new ConcurrentFactory( config, model, Config.Inst.CONCURRENT_FACTORY_INSTANCE_COUNT );
                            _RussianLanguageConcurrentFactory = f;
                            //GC.KeepAlive( _RussianLanguageConcurrentFactory );
                        }
                    }
                }
                return (f);
            }
        }

        static RESTProcessHandler()
        {
            Environment.CurrentDirectory = HttpContext.Current.Server.MapPath( "~/" );
        }

        public bool IsReusable
        {
            get { return (true); }
        }

        public void ProcessRequest( HttpContext context )
        {
            try
            {
                var text = context.GetRequestStringParam( "text", Config.Inst.MAX_INPUTTEXT_LENGTH );
                var type = context.Request[ "type" ];

                var hcd = new http_context_data( context );
                var languageInfos = default( LanguageInfo[] );
                if ( string.Compare( type, "ru", true ) == 0 )
                {
                    languageInfos = hcd.GetRussianLanguageConcurrentFactory().DetectLanguage( text );
                }
                else
                {
                    languageInfos = hcd.GetMultiLanguageConcurrentFactory().DetectLanguage( text );
                }

                context.Response.ToJson( languageInfos );
            }
            catch ( Exception ex )
            {
                context.Response.ToJson( ex );
            }

            /*
            {
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                GC.WaitForPendingFinalizers();
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            }
            */
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static bool Try2Boolean( this string value, bool defaultValue )
        {
            if ( value != null )
            {
                var result = default( bool );
                if ( bool.TryParse( value, out result ) )
                    return (result);
            }
            return (defaultValue);
        }

        public static string GetRequestStringParam( this HttpContext context, string paramName, int maxLength )
        {
            var value = context.Request[ paramName ];
            if ( (value != null) && (maxLength < value.Length) && (0 < maxLength) )
            {
                return (value.Substring( 0, maxLength ));
            }
            return (value);
        }

        public static void ToJson( this HttpResponse response, LanguageInfo[] languageInfos )
        {
            response.ToJson( new RESTProcessHandler.result( languageInfos ) );
        }
        public static void ToJson( this HttpResponse response, Exception ex )
        {
            response.ToJson( new RESTProcessHandler.result( ex ) );
        }
        public static void ToJson( this HttpResponse response, RESTProcessHandler.result result )
        {
            response.ContentType = "application/json";
            //---response.Headers.Add( "Access-Control-Allow-Origin", "*" );

            var json = JsonConvert.SerializeObject( result );
            response.Write( json );
        }

        public static string ToText( this Language language )
        {
            switch ( language )
            {
                case Language.RU: return ("russian / русский");
                case Language.EN: return ("english / английский");
                case Language.NL: return ("dutch / голландский");
                case Language.FI: return ("finnish / финский");
                case Language.SW: return ("swedish / шведский");
                case Language.UK: return ("ukrainian / украинский");
                case Language.BG: return ("bulgarian / болгарский");
                case Language.BE: return ("belorussian / белорусский");
                case Language.DE: return ("german / немецкий");
                case Language.FR: return ("french / французский");
                case Language.ES: return ("spanish / испанский");
                case Language.KK: return ("kazakh / казахский");
                case Language.PL: return ("polish / польский");
                case Language.TT: return ("tatar / татарский");
                case Language.IT: return ("italian / итальянский");
                //case Language.SR: return ("serbian / сербский");
                case Language.PT: return ("portuguese / португальский");
                case Language.DA: return ("danish / датский");
                case Language.CS: return ("czech / чешский");
                case Language.NO: return ("norwegian / норвежский");
            }
            return (language.ToString());
        }
    }
}