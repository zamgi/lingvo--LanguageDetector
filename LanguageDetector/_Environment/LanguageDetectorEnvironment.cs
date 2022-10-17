using System;
using System.Diagnostics;

using lingvo.ld.MultiLanguage;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class MLanguageDetectorEnvironmentBase< TModel > : IDisposable where TModel: IMModel
    {
        protected MLanguageDetectorEnvironmentBase() { }
        public void Dispose() => Model.Dispose();

        public ILanguageDetectorEnvironmentConfig LanguageDetectorEnvironmentConfig { get; protected set; }
        public MDetectorConfig MDetectorConfig { get; protected set; }
        public TModel          Model           { get; protected set; }

        public MDetector CreateMDetector() => new MDetector( MDetectorConfig, Model );
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class MLanguageDetectorEnvironment_BinaryNative : MLanguageDetectorEnvironmentBase< MModelBinaryNative >
    {
        public static MLanguageDetectorEnvironment_BinaryNative Create( ILanguageDetectorEnvironmentConfig opts, bool print2Console = true )
        {
            var sw = default(Stopwatch);
            if ( print2Console )
            {
                sw = Stopwatch.StartNew();
                Console.Write( "init language-detector-environment..." );
            }

            var config = opts.GetMDetectorConfig();
            var model  = new MModelBinaryNative( opts.GetMModelBinaryNativeConfig() );

            var env = new MLanguageDetectorEnvironment_BinaryNative() { MDetectorConfig = config, Model = model, LanguageDetectorEnvironmentConfig = opts };

            if ( print2Console )
            {
                sw.Stop();
                Console.WriteLine( $"end, (elapsed: {sw.Elapsed}).\r\n----------------------------------------------------\r\n" );
            }

            return (env);
        }
        public static MLanguageDetectorEnvironment_BinaryNative Create( bool print2Console = true ) => Create( new LanguageDetectorEnvironmentConfigImpl(), print2Console );
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class MLanguageDetectorEnvironment_Classic : MLanguageDetectorEnvironmentBase< MModelClassic >
    {
        public static MLanguageDetectorEnvironment_Classic Create( ILanguageDetectorEnvironmentConfig opts, bool print2Console = true )
        {
            var sw = default(Stopwatch);
            if ( print2Console )
            {
                sw = Stopwatch.StartNew();
                Console.Write( "init language-detector-environment..." );
            }

            var config = opts.GetMDetectorConfig();
            var model  = new MModelClassic( opts.GetMModelConfig() );

            var env = new MLanguageDetectorEnvironment_Classic() { MDetectorConfig = config, Model = model, LanguageDetectorEnvironmentConfig = opts };

            if ( print2Console )
            {
                sw.Stop();
                Console.WriteLine( $"end, (elapsed: {sw.Elapsed}).\r\n----------------------------------------------------\r\n" );
            }

            return (env);
        }
        public static MLanguageDetectorEnvironment_Classic Create( bool print2Console = true ) => Create( new LanguageDetectorEnvironmentConfigImpl(), print2Console );
    }
}
