using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using lingvo.ld.MultiLanguage;
using lingvo.ld.RussianLanguage;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        public const string SERVICE_NAME = "LanguageDetector.WebService";

        private static async Task Main( string[] args )
        {
            var hostApplicationLifetime = default(IHostApplicationLifetime);
            var logger                  = default(ILogger);
            try
            {
                //---------------------------------------------------------------//
                #region [.load language-model.]
                Console.Write( "load model..." );
                var sw = Stopwatch.StartNew();

                var config      = Config.Inst.GetMDetectorConfig();
                var modelConfig = Config.Inst.GetMModelBinaryNativeConfig();
                using var model = new MModelBinaryNative( modelConfig );

                Console.WriteLine( $"elapsed: {sw.StopElapsed()}\r\n" ); 

                using var concurrentFactory = new ConcurrentFactory( config, model, Config.Inst.CONCURRENT_FACTORY_INSTANCE_COUNT );

                /*
                var config      = Config.Inst.GetRDetectorConfig();
                var modelConfig = Config.Inst.GetRModelConfig();
                using var model = new RModelClassic( modelConfig );

                var concurrentFactory = new ConcurrentFactory( config, model, Config.Inst.CONCURRENT_FACTORY_INSTANCE_COUNT );
                */
                #endregion
                //---------------------------------------------------------------//

                var host = Host.CreateDefaultBuilder( args )
                               .ConfigureLogging( loggingBuilder => loggingBuilder.ClearProviders().AddDebug().AddConsole().AddEventSourceLogger()
                                                              .AddEventLog( new EventLogSettings() { LogName = SERVICE_NAME, SourceName = SERVICE_NAME } ) )
                               //---.UseWindowsService()
                               .ConfigureServices( (hostContext, services) => services.AddSingleton( concurrentFactory ) )
                               .ConfigureWebHostDefaults( webBuilder => webBuilder.UseStartup< Startup >() )
                               .Build();
                hostApplicationLifetime = host.Services.GetService< IHostApplicationLifetime >();
                logger                  = host.Services.GetService< ILoggerFactory >()?.CreateLogger( SERVICE_NAME );
                await host.RunAsync();
            }
            catch ( OperationCanceledException ex ) when ((hostApplicationLifetime?.ApplicationStopping.IsCancellationRequested).GetValueOrDefault())
            {
                Debug.WriteLine( ex ); //suppress
            }
            catch ( Exception ex ) when (logger != null)
            {
                logger.LogCritical( ex, "Global exception handler" );
            }
        }

        private static TimeSpan StopElapsed( this Stopwatch sw )
        {
            sw.Stop();
            return (sw.Elapsed);
        }
    }
}
