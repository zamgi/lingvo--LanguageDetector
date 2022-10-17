using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;

namespace lingvo.ld.TestApp
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );

                #region [.GC.]
                GCSettings.LatencyMode = GCLatencyMode.LowLatency;
                if ( GCSettings.LatencyMode != GCLatencyMode.LowLatency )
                {
                    GCSettings.LatencyMode = GCLatencyMode.Batch;
                } 
                #endregion

                //Test__MModelBinaryNative();
                Run_4_Files( @"E:\" );

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
            #region comm.
            //using ( var m = new MModelDictionaryNativeMMF( Config.Inst.GetMModelConfig() ) )
            //using ( var m = new MModelNativeTextMMF( Config.Inst.GetMModelConfig(), ModelLoadTypeEnum.Consecutively ) ) 
            //{
            //    Console.WriteLine( m.TryGetValue( "XZ", out var q ) );
            //}
            #endregion

            using var env = MLanguageDetectorEnvironment_BinaryNative.Create();
            var detector = env.CreateMDetector();

            GCCollect();

            Console.WriteLine( $"Model.RecordCount: {env.Model.RecordCount}\r\n" );

            var text = "[.....push enter for continue.....]";
            var languageInfos = detector.DetectLanguage( text );
            languageInfos.Print2Console( text );
        }
        private static void Run_4_Files( string path )
        {
            using var env = MLanguageDetectorEnvironment_BinaryNative.Create();
            var detector = env.CreateMDetector();

            var n = 0;
            foreach ( var fn in EnumerateAllFiles( path ) )
            {
                var text = File.ReadAllText( fn ).Cut( 10_000_000 );

                var languageInfos = detector.DetectLanguage( text );

                Console_Write( $"{++n}.) ", ConsoleColor.DarkGray );
                languageInfos.Print2Console( text );
            }
        }

        private static void Test__MModelClassic()
        {
            using var env = MLanguageDetectorEnvironment_Classic.Create();
            var detector = env.CreateMDetector();

            GCCollect();

            Console.WriteLine( $"Model.RecordCount: {env.Model.RecordCount}\r\n" );

            var text = "[.....push enter for continue.....]";
            var languageInfos = detector.DetectLanguage( text );
            languageInfos.Print2Console( text );
        }

        private static void Print2Console( this IList< LanguageInfo > languageInfos, string text )
        {
            Console.Write( $"text: " );
            Console_WriteLine( $"'{text.Cut().Norm()}'", ConsoleColor.DarkGray );
            if ( languageInfos.Any() )
                Console.WriteLine( "  " + string.Join( "\r\n  ", string.Join( ", ", languageInfos.Select( i => $"{i.Language}, {i.Percent} %" ) ) ) );
            else
                Console_WriteLine( "  [text language is not defined]", ConsoleColor.DarkRed );
            Console.WriteLine();
        }

        private static IEnumerable< string > EnumerateAllFiles( string path, string searchPattern = "*.txt" )
        {
            try
            {
                var seq = Directory.EnumerateDirectories( path ).SafeWalk()
                                   .SelectMany( _path => EnumerateAllFiles( _path ) );
                return (seq.Concat( Directory.EnumerateFiles( path, searchPattern )/*.SafeWalk()*/ ));
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( ex.GetType().Name + ": '" + ex.Message + '\'' );
                return (Enumerable.Empty< string >());
            }
        }

        private static void Console_Write( string msg, ConsoleColor color )
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write( msg );
            Console.ForegroundColor = fc;
        }
        private static void Console_WriteLine( string msg, ConsoleColor color )
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine( msg );
            Console.ForegroundColor = fc;
        }

        private static void GCCollect()
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            GC.WaitForPendingFinalizers();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static string Cut( this string s, int max_len = 150 ) => (max_len < s.Length) ? s.Substring( 0, max_len ) + "..." : s;
        public static string Norm( this string s )
        {
            s = s.Replace( '\n', ' ' ).Replace( '\r', ' ' ).Replace( '\t', ' ' );
            for (; ; )
            {
                var t = s.Replace( "  ", " " );
                if ( t == s )
                {
                    break;
                }
                s = t;
            }
            return (s);
        }
        public static bool IsNullOrEmpty( this string value ) => string.IsNullOrEmpty( value );
        public static IEnumerable< T > SafeWalk< T >( this IEnumerable< T > source )
        {
            using ( var enumerator = source.GetEnumerator() )
            {
                for ( ; ; )
                {
                    try
                    {
                        if ( !enumerator.MoveNext() )
                            break;
                    }
                    catch ( Exception ex )
                    {
                        Debug.WriteLine( ex.GetType().Name + ": '" + ex.Message + '\'' );
                        continue;
                    }

                    yield return (enumerator.Current);
                }
            }
        }
    }
}
