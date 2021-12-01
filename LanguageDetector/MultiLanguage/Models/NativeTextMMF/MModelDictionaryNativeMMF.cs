using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private NativeMemAllocationMediator_ThreadSafe _NativeMemAllocator;
        #endregion

        #region [.ctor().]
        public MModelDictionaryNativeMMF( MModelConfig config )
        {
            _NativeMemAllocator = new NativeMemAllocationMediator_ThreadSafe( nativeBlockAllocSize: 1024 * 1024 * 2 );
            ParallelLoadMMF( config, _NativeMemAllocator );
        }
        ~MModelDictionaryNativeMMF() => DisposeNativeResources();
        public void Dispose()
        {
            DisposeNativeResources();
            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources() => _NativeMemAllocator.Dispose();
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
            private NativeMemAllocationMediator_ThreadSafe _NativeMemAllocator;

            public ParallelLoadUnit( NativeMemAllocationMediator_ThreadSafe nativeMemAllocator ) : this() => _NativeMemAllocator = nativeMemAllocator;
            public void Initialize( int capacity )
            {
                if ( DictionaryNative == null )
                {
                    Capacity         = capacity;
                    DictionaryNative = new DictionaryNative( _NativeMemAllocator, capacity );
                    LoadMMFCallback  = new LoadModelFilenameContentCallback( DictionaryNative.AddNewOrToEndOfChain );
                }
            }

            public override string ToString() => ("count: " + DictionaryNative.Count + ", (capacity: " + Capacity + ")");
        }

        private void ParallelLoadMMF( MModelConfig config, NativeMemAllocationMediator_ThreadSafe nativeMemAllocator )
        {
            #region [.parallel load by partitions.]
            var processorCount = Environment.ProcessorCount;

            var partitions = config.LanguageConfigs.SplitByPartitionCountOrGreater( processorCount );

            var unitBag = new ConcurrentBag< ParallelLoadUnit >();

            Parallel.ForEach( partitions,
                new ParallelOptions() { MaxDegreeOfParallelism = processorCount },
                () => new ParallelLoadUnit( nativeMemAllocator ),
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
            var unionDict = new DictionaryNative( nativeMemAllocator, config.ModelDictionaryCapacity );
            foreach ( var dict in unitBag.Select( unit => unit.DictionaryNative ) )
            {
                unionDict.MergeWith( dict );
            }
            _Dictionary = unionDict;
            unitBag = null;
            #endregion
        }
        #endregion

        #region [.IModel.]
        public int RecordCount => _Dictionary.Count;
        unsafe public bool TryGetValue( string ngram, out IEnumerable< WeighByLanguage > weighByLanguages )
        {            
            fixed ( char* ngramPtr = ngram )
            {
                if ( _Dictionary.TryGetValue( (IntPtr) ngramPtr, out var bucketVal ) )
                {
                    weighByLanguages = new WeighByLanguageEnumerator( in bucketVal ); //bucketVal.GetWeighByLanguages();
                    return (true);
                }
            }
            weighByLanguages = null;
            return (false);
        }
        public IEnumerable< MModelRecord > GetAllRecords() => _Dictionary.GetAllModelRecords();
        #endregion
    }
}
