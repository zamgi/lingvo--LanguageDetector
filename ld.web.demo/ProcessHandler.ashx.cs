using System;
using System.Linq;
using System.Web;

using lingvo.ld.MultiLanguage;
using lingvo.ld.RussianLanguage;
using Newtonsoft.Json;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ProcessHandler : IHttpHandler
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
                [JsonProperty(PropertyName="l")] public string language          { get; set; }
                [JsonProperty(PropertyName="n")] public string language_fullname { get; set; }
                [JsonProperty(PropertyName="p")] public float  percent           { get; set; }
            }

            public result( Exception ex ) : this() => exception_message = ex.ToString();
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

            [JsonProperty(PropertyName="langs")] public language_info[] language_infos    { get; private set; }
            [JsonProperty(PropertyName="err")  ] public string          exception_message { get; private set; }
        }

        /// <summary>
        /// 
        /// </summary>
        private struct http_context_data
        {
            private static readonly object _SyncLock = new object();
            private readonly HttpContext _Context;

            public http_context_data( HttpContext context ) => _Context = context;

            #region comm.
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
            #endregion

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

        static ProcessHandler() => Environment.CurrentDirectory = HttpContext.Current.Server.MapPath( "~/" );

        public bool IsReusable => true;
        public void ProcessRequest( HttpContext context )
        {
            #region [.log.]
            if ( Log.ProcessViewCommand( context ) )
            {
                return;
            }
            #endregion

            var text = default(string);
            try
            {
                text = context.GetRequestStringParam( "text", Config.Inst.MAX_INPUTTEXT_LENGTH );
                var type = context.Request[ "type" ];

                var hcd = new http_context_data( context );
                var languageInfos = default(LanguageInfo[]);
                if ( string.Compare( type, "ru", true ) == 0 )
                {
                    languageInfos = hcd.GetRussianLanguageConcurrentFactory().DetectLanguage( text );
                }
                else
                {
                    languageInfos = hcd.GetMultiLanguageConcurrentFactory().DetectLanguage( text );
                }

                //---Log.Info( context, text );
                context.Response.ToJson( languageInfos );
            }
            catch ( Exception ex )
            {
                Log.Error( context, text, ex );
                context.Response.ToJson( ex );
            }

            #region comm.
            /*
            {
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                GC.WaitForPendingFinalizers();
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            }
            */ 
            #endregion
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static string GetRequestStringParam( this HttpContext context, string paramName, int maxLength )
        {
            var value = context.Request[ paramName ];
            if ( (value != null) && (maxLength < value.Length) && (0 < maxLength) )
            {
                return (value.Substring( 0, maxLength ));
            }
            return (value);
        }

        public static void ToJson( this HttpResponse response, LanguageInfo[] languageInfos ) => response.ToJson( new ProcessHandler.result( languageInfos ) );
        public static void ToJson( this HttpResponse response, Exception ex ) => response.ToJson( new ProcessHandler.result( ex ) );
        public static void ToJson( this HttpResponse response, ProcessHandler.result result )
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