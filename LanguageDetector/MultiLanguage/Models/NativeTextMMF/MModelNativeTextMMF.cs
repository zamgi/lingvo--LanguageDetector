using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using lingvo.ld.MultiLanguage.RucksackPacking;
using lingvo.core;

namespace lingvo.ld.MultiLanguage
{    
    /// <summary>
    /// 
    /// </summary>
    public sealed class MModelNativeTextMMF : MModelNativeTextMMFBase, IMModel
    {
        #region [.private field's.]
        private Dictionary< IntPtr, BucketValue > _Dictionary;
        private INativeMemAllocationMediator _NativeMemAllocator;
        #endregion

        #region [.ctor().]
        public MModelNativeTextMMF( MModelConfig config, ModelLoadTypeEnum modelLoadType )
        {
            const int nativeBlockAllocSize = 1024 * 1024 * 2;

            switch ( modelLoadType )
            {
                case ModelLoadTypeEnum.Consecutively:
                    _NativeMemAllocator = new NativeMemAllocationMediator( nativeBlockAllocSize );
                    ConsecutivelyLoadMMF( config );
                break;

                case ModelLoadTypeEnum.Parallel:
                    _NativeMemAllocator = new NativeMemAllocationMediator_ThreadSafe( nativeBlockAllocSize );
                    ParallelLoadMMF( config, _NativeMemAllocator );
                break;

                default:
                    throw (new ArgumentException( modelLoadType.ToString() ));
            }
        }
        ~MModelNativeTextMMF() => DisposeNativeResources();
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
            public Dictionary< IntPtr, BucketValue > Dictionary;
            public LoadModelFilenameContentCallback LoadMMFCallback;
            public int Capacity;
            private INativeMemAllocationMediator _NativeMemAllocator;

            public ParallelLoadUnit( INativeMemAllocationMediator nativeMemAllocator ) : this() => _NativeMemAllocator = nativeMemAllocator;
            unsafe private void LoadMMFCallbackRoutine( ref MModelNativeTextMMFBase.Pair pair )
            {
                if ( Dictionary.TryGetValue( pair.TextPtr, out var bucketVal ) )
                {
                    var bucketRef = new BucketRef() 
                    { 
                        Language   = pair.Language, 
                        Weight     = pair.Weight,
                        NextBucket = bucketVal.NextBucket,
                    };
                    bucketVal.NextBucket = bucketRef;

                    Dictionary[ pair.TextPtr ] = bucketVal;
                }
                else
                {
                    var textPtr = _NativeMemAllocator.AllocAndCopy( (char*) pair.TextPtr, pair.TextLength );
                    Dictionary.Add( textPtr, new BucketValue( pair.Language, pair.Weight ) );
                }
            }
            public void Initialize( int capacity )
            {
                if ( Dictionary == null )
                {
                    Capacity        = capacity;
                    Dictionary      = new Dictionary< IntPtr, BucketValue >( capacity, IntPtrEqualityComparer.Inst );
                    LoadMMFCallback = new LoadModelFilenameContentCallback( LoadMMFCallbackRoutine );
                }
            }

            public override string ToString() => ("count: " + Dictionary.Count + ", (capacity: " + Capacity + ")");
        }

        private void ParallelLoadMMF( MModelConfig config, INativeMemAllocationMediator nativeMemAllocator )
        {
            #region [.parallel load by partitions.]
            var processorCount = Environment.ProcessorCount;
            //var partitions = CreatePartitions( config.LanguageConfigs, processorCount );

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
                    if ( unit.Dictionary.Count != 0 )
                    {
                        unitBag.Add( unit );
                    }
                }
            );
            #endregion

            #region [.merge.]
            var dictionary = (0 < config.ModelDictionaryCapacity) 
                ? new Dictionary< IntPtr, BucketValue >( config.ModelDictionaryCapacity, IntPtrEqualityComparer.Inst )
                : new Dictionary< IntPtr, BucketValue >( IntPtrEqualityComparer.Inst );

            foreach ( var dict in unitBag.Select( unit => unit.Dictionary ) )
            {
                foreach ( var pair in dict )
                {
                    var textPtr       = pair.Key;
                    var bucketValElse = pair.Value;

                    if ( dictionary.TryGetValue( textPtr, out var bucketVal ) )
                    {
                        var bucketRef = new BucketRef()
                        {
                            Language   = bucketValElse.Language,
                            Weight     = bucketValElse.Weight,
                            NextBucket = bucketValElse.NextBucket,
                        };
                        if ( bucketVal.NextBucket == null )
                        {
                            bucketVal.NextBucket = bucketRef;

                            dictionary[ textPtr ] = bucketVal;
                        }
                        else
                        {
                            var br = bucketVal.NextBucket;
                            for ( ; br.NextBucket != null; br = br.NextBucket );
                            br.NextBucket = bucketRef;
                        }
                    }
                    else
                    {
                        dictionary.Add( textPtr, bucketValElse );
                    }
                }
                //dict.Clear(); //--- too slow => TODO: вытащить словарь/pull dictionary ---//
            }

            _Dictionary = dictionary;
            unitBag = null;
            #endregion
        }
        /*private static LanguageConfigPartition[] CreatePartitions( IEnumerable< LanguageConfig > languageConfigs, int processorCount )
        {
            var array = languageConfigs.Select( lc => new { LanguageConfig = lc, ModelFilenameLength = (new FileInfo( lc.ModelFilename )).Length } )
                                       .OrderByDescending( a => a.ModelFilenameLength )
                                       .ToArray();
            var partSize = (int) (1.0 * array.Length / processorCount + 0.5);

            var partitions = new LanguageConfigPartition[ processorCount ];
            for ( var i = 0; i < array.Length; i++ )
            {
                int partIndex;
                int itemIndexInPart = Math.DivRem( i, partSize - 1, out partIndex );

                if ( partitions[ partIndex ].LanguageConfigs == null )
                {
                    partitions[ partIndex ].LanguageConfigs = new LanguageConfig[ partSize ];
                }
                //Debug.Assert( part[ itemIndexInPart ] == null );
                var a = array[ i ];
                partitions[ partIndex ].LanguageConfigs[ itemIndexInPart ] = a.LanguageConfig;
                partitions[ partIndex ].TotalModelFilenameLengths += a.ModelFilenameLength;
            }
            return (partitions);
        }*/

        private void ConsecutivelyLoadMMF( MModelConfig config )
        {
            _Dictionary = (0 < config.ModelDictionaryCapacity) 
                ? new Dictionary< IntPtr, BucketValue >( config.ModelDictionaryCapacity, IntPtrEqualityComparer.Inst )
                : new Dictionary< IntPtr, BucketValue >( IntPtrEqualityComparer.Inst );

            var callback = new LoadModelFilenameContentCallback( ConsecutivelyLoadMMFCallback );

            foreach ( var languageConfig in config.LanguageConfigs )
            {
                LanguageModelFileReaderMMF.Read( languageConfig, callback );
            }
        }
        unsafe private void ConsecutivelyLoadMMFCallback( ref MModelNativeTextMMFBase.Pair pair )
        {
            if ( _Dictionary.TryGetValue( pair.TextPtr, out var bucketVal ) )
            {
                var bucketRef = new BucketRef()
                {
                    Language   = pair.Language,
                    Weight     = pair.Weight,
                    NextBucket = bucketVal.NextBucket,
                };
                bucketVal.NextBucket = bucketRef;

                _Dictionary[ pair.TextPtr ] = bucketVal;
            }
            else
            {
                var textPtr = _NativeMemAllocator.AllocAndCopy( (char*) pair.TextPtr, pair.TextLength );
                _Dictionary.Add( textPtr, new BucketValue( pair.Language, pair.Weight ) );
            }
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
