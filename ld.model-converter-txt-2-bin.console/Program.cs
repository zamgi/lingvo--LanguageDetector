using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;

namespace lingvo.ld.modelconverter
{
    using lingvo.ld.MultiLanguage;
    using lingvo.ld.MultiLanguage.modelconverter;
    using lingvo.ld.v1;

    using BucketRef   = lingvo.ld.v1.ManyLanguageDetectorModel.BucketRef;
    using BucketValue = lingvo.ld.v1.ManyLanguageDetectorModel.BucketValue;    

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

                #region [.use boost priority.]
                //if ( Config.Inst.USE_BOOST_PRIORITY )
				//{
					var process = Process.GetCurrentProcess();
					process.PriorityClass         = ProcessPriorityClass.RealTime;
					process.PriorityBoostEnabled  = true;
					Thread.CurrentThread.Priority = ThreadPriority.Highest;
				//}
                #endregion

                #region [.print to console config.]
                Console.WriteLine(Environment.NewLine + "----------------------------------------------");
                Console.WriteLine( "      LANGUAGE_MODELS_FOLDER: '" + Config.Inst.LANGUAGE_MODELS_FOLDER + "'" );
                Console.WriteLine( "ML_MODEL_DICTIONARY_CAPACITY: '" + Config.Inst.ML_MODEL_DICTIONARY_CAPACITY + "'" );
                Console.WriteLine( "            OUTPUT_FILE_NAME: '" + Config.Inst.OUTPUT_FILE_NAME + "'" );
                if ( Config.Inst.OUTPUT_FILE_SIZE_IN_BYTES != 0 )
                {
                Console.WriteLine( "   OUTPUT_FILE_SIZE_IN_BYTES: '" + Config.Inst.OUTPUT_FILE_SIZE_IN_BYTES + "'" );
                }
				Console.WriteLine("----------------------------------------------" + Environment.NewLine);
                #endregion

                #region [.main routine.]
                //---ConvertFromTxt2Bin();

                //---
                Test4SpeedModelBinaryNative();

#if DEBUG
                Comare_ModelBinaryNative_And_ModelClassic(); 
#endif

                Console.WriteLine( Environment.NewLine + "[.....finita fusking comedy.....]" );
                Console.ReadLine();
                #endregion
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();

                Console.WriteLine( Environment.NewLine + "[.....finita fusking comedy (push ENTER 4 exit).....]" );
                Console.ReadLine();
            }
        }

        private static void ConvertFromTxt2Bin()
        {
            var modelConfig = Config.Inst.GetModelConfig();
            var model = TxtModelFactory.GetModelClassic( modelConfig );
            var config = new Txt2BinModelConverterConfig()
            {
                Model = model,
                OutputFileName = Config.Inst.OUTPUT_FILE_NAME,
                OutputFileSizeInBytes = Config.Inst.OUTPUT_FILE_SIZE_IN_BYTES, //1024 * 1024 * 99,
            };
            var outputFileNames = Txt2BinModelConverter.Run( config );

            Console.WriteLine();
            Console.WriteLine( " output-files: " );
            Console.WriteLine( " --------------" );
            for ( var i = 0; i < outputFileNames.Count; i++ )
            {
                Console.WriteLine( ' '  + (i + 1).ToString() + "). '" + outputFileNames[ i ] + '\'' );
            }
            Console.WriteLine( " --------------\r\n" );
        }

        private static void Test4SpeedModelBinaryNative()
        {            
            var modelConfig = Config.Inst.GetModelBinaryNativeConfig();

            var sw = Stopwatch.StartNew();
            var model = TxtModelFactory.GetModelBinaryNative( modelConfig );
            sw.Stop();

            Console.WriteLine( "elapsed: " + sw.Elapsed + ", record-count: " + model.RecordCount );
            Console.ReadLine();

            model.Dispose();
            model = null;

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            GC.WaitForPendingFinalizers();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }


        private static void Comare_ModelBinaryNative_And_ModelClassic()
        {
#if DEBUG
            using ( var model_1 = TxtModelFactory.GetModelBinaryNative( Config.Inst.GetModelBinaryNativeConfig() ) )
            using ( var model_2 = TxtModelFactory.GetModelClassic( Config.Inst.GetModelConfig() ) )
            {
                ModelComparer.Compare( model_1, model_2 );
            } 
#else
            throw (new NotImplementedException( "Allowed only in DEBUG mode" ));
#endif
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class TxtModelFactory
    {
        public static MModelClassic GetModelClassic( MModelConfig config )
        {
            var model = new MModelClassic( config );
            return (model);
        }

        public static MModelBinaryNative GetModelBinaryNative( MModelBinaryNativeConfig config )
        {
            var model = new MModelBinaryNative( config );
            return (model);
        }
    }
}
