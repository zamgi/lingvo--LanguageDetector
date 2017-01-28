using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using lingvo.ld.MultiLanguage.RucksackPacking;

namespace lingvo.ld.MultiLanguage
{    
    /// <summary>
    /// 
    /// </summary>
    public sealed class MModelDictionaryNativeMMF : MModelNativeTextMMFBase, IMModel
    {
        #region [.private field's.]
        private DictionaryNative _Dictionary;
        #endregion

        #region [.ctor().]
        public MModelDictionaryNativeMMF( MModelConfig config )
        {
            //var sw = Stopwatch.StartNew();
            ParallelLoadMMF( config );
            //sw.Stop();
            //Console.WriteLine( "Elapsed: " + sw.Elapsed );        
        }
        ~MModelDictionaryNativeMMF()
        {
            DisposeNativeResources();
        }

        public void Dispose()
        {
            DisposeNativeResources();

            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources()
        {
            if ( _Dictionary != null )
            {
                foreach ( var ptr in _Dictionary.Keys )
                {
                    Marshal.FreeHGlobal( ptr );
                }
                _Dictionary.Dispose();
                _Dictionary = null;
            }
        } 
        #endregion

        #region [.model-dictionary loading.]
        /// <summary>
        /// 
        /// </summary>
        private struct ParallelLoadUnit
        {
            public DictionaryNative DictionaryNative;
            public LoadModelFilenameContentCallback LoadMMFCallback;
            public int Capacity;

            public void Initialize( int capacity )
            {
                if ( DictionaryNative == null )
                {
                    Capacity         = capacity;
                    DictionaryNative = new DictionaryNative( capacity );
                    LoadMMFCallback  = new LoadModelFilenameContentCallback( DictionaryNative.AddNewOrToEndOfChain );
                }
            }

            public override string ToString()
            {
                return ("count: " + DictionaryNative.Count + ", (capacity: " + Capacity + ")");
            }
        }

        private void ParallelLoadMMF( MModelConfig config )
        {
            #region [.parallel load by partitions.]
            var processorCount = Environment.ProcessorCount;

            var partitions = config.LanguageConfigs.SplitByPartitionCountOrGreater( processorCount );

            var unitBag = new ConcurrentBag< ParallelLoadUnit >();

            Parallel.ForEach( partitions,
                new ParallelOptions() { MaxDegreeOfParallelism = processorCount },
                () => default(ParallelLoadUnit),
                (partition, loopState, i, unit) =>
                {
                    const int EMPIRICALLY_CHOSEN_FUSKING_NUMBER = 27;

                    var capacity = (int) (partition.TotalModelFilenameLengths / EMPIRICALLY_CHOSEN_FUSKING_NUMBER);
                    unit.Initialize( capacity );

                    foreach ( var languageConfig in partition.LanguageConfigs )
                    {
                        LanguageModelFileReaderMMF.Read( languageConfig, unit.LoadMMFCallback );
                    }

                    return (unit);
                },
                (unit) => 
                {
                    if ( unit.DictionaryNative != null && unit.DictionaryNative.Count != 0 )
                    {
                        unitBag.Add( unit );
                    }
                }
            );
            #endregion

            #region [.merge.]
            var dictionary = new DictionaryNative( config.ModelDictionaryCapacity );

            foreach ( var dict in unitBag.Select( unit => unit.DictionaryNative ) )
            {
                dictionary.MergeWith( dict );

                dict.Dispose();
            }

            _Dictionary = dictionary;
            unitBag = null;
            #endregion
        }
        #endregion

        #region [.IModel.]
        public int RecordCount
        {
            get { return (_Dictionary.Count); }
        }
        unsafe public bool TryGetValue( string ngram, out IEnumerable< WeighByLanguage > weighByLanguages )
        {            
            fixed ( char* ngramPtr = ngram )
            {
                BucketValue bucketVal;
                if ( _Dictionary.TryGetValue( (IntPtr) ngramPtr, out bucketVal ) )
                {
                    weighByLanguages = new WeighByLanguageEnumerator( ref bucketVal ); //bucketVal.GetWeighByLanguages();
                    return (true);
                }
            }
            weighByLanguages = null;
            return (false);
        }
        public IEnumerable< MModelRecord > GetAllRecords()
        {
            return (_Dictionary.GetAllModelRecords());
        }
        #endregion
    }
}
