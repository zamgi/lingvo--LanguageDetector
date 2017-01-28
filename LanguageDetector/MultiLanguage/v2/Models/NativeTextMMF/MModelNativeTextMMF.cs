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
        #endregion

        #region [.ctor().]
        public MModelNativeTextMMF( MModelConfig config, ModelLoadTypeEnum modelLoadType )
        {
            //var sw = Stopwatch.StartNew();
            switch ( modelLoadType )
            {
                case ModelLoadTypeEnum.Consecutively:
                ConsecutivelyLoadMMF( config );
                break;

                case ModelLoadTypeEnum.Parallel:
                ParallelLoadMMF( config );
                break;

                default:
                throw (new ArgumentException( modelLoadType.ToString() ));
            }
            //sw.Stop();
            //Console.WriteLine( "Elapsed: " + sw.Elapsed );        
        }
        ~MModelNativeTextMMF()
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
                _Dictionary.Clear();
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
            public Dictionary< IntPtr, BucketValue > Dictionary;
            public LoadModelFilenameContentCallback LoadMMFCallback;
            public int Capacity;

            unsafe private void LoadMMFCallbackRoutine( ref MModelNativeTextMMFBase.Pair pair )
            {
                BucketValue bucketVal;

                if ( Dictionary.TryGetValue( pair.TextPtr, out bucketVal ) )
                {
                    var bucketRef = new BucketRef() 
                    { 
                        Language   = pair.Language, 
                        Weight     = pair.Weight,
                        NextBucket = bucketVal.NextBucket,
                    };
                    bucketVal.NextBucket = bucketRef;

                    Dictionary[ pair.TextPtr ] = bucketVal;

                    #region commented. previous
                    /*
                    var bucketRef = new BucketRef() { Language = pair.Language, Weight = pair.Weight };
                    if ( bucketVal.NextBucket == null )
                    {
                        bucketVal.NextBucket = bucketRef;

                        DictionaryIntptr[ pair.TextPtr ] = bucketVal;
                    }
                    else
                    {
                        var br = bucketVal.NextBucket;
                        for (; br.NextBucket != null; br = br.NextBucket );
                        br.NextBucket = bucketRef;
                    }
                    */
                    #endregion
                }
                else
                {
                    var textPtr = StringsHelper.AllocHGlobalAndCopy( pair.TextPtr, pair.TextLength );
                    Dictionary.Add( textPtr, new BucketValue( pair.Language, pair.Weight ) );
                }
            }
            public void Initialize( int capacity )
            {
                if ( Dictionary == null )
                {
                    Capacity        = capacity;
                    Dictionary      = new Dictionary< IntPtr, BucketValue >( capacity, default(IntPtrEqualityComparer) );
                    LoadMMFCallback = new LoadModelFilenameContentCallback( LoadMMFCallbackRoutine );
                }
            }

            public override string ToString()
            {
                return ("count: " + Dictionary.Count + ", (capacity: " + Capacity + ")");
            }
        }

        private void ParallelLoadMMF( MModelConfig config )
        {
            #region [.parallel load by partitions.]
            var processorCount = Environment.ProcessorCount;
            //var partitions = CreatePartitions( config.LanguageConfigs, processorCount );

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
                    if ( unit.Dictionary.Count != 0 )
                    {
                        unitBag.Add( unit );
                    }
                }
            );
            #endregion

            #region [.merge.]
            var bucketVal = default(BucketValue);

            var dictionary = (0 < config.ModelDictionaryCapacity) 
                ? new Dictionary< IntPtr, BucketValue >( config.ModelDictionaryCapacity, default(IntPtrEqualityComparer) )
                : new Dictionary< IntPtr, BucketValue >( default(IntPtrEqualityComparer) );

            foreach ( var dict in unitBag.Select( unit => unit.Dictionary ) )
            {
                foreach ( var pair in dict )
                {
                    var textPtr       = pair.Key;
                    var bucketValElse = pair.Value;

                    if ( dictionary.TryGetValue( textPtr, out bucketVal ) )
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
                ? new Dictionary< IntPtr, BucketValue >( config.ModelDictionaryCapacity, default(IntPtrEqualityComparer) )
                : new Dictionary< IntPtr, BucketValue >( default(IntPtrEqualityComparer) );

            var callback = new LoadModelFilenameContentCallback( ConsecutivelyLoadMMFCallback );

            foreach ( var languageConfig in config.LanguageConfigs )
            {
                LanguageModelFileReaderMMF.Read( languageConfig, callback );
            }
        }
        unsafe private void ConsecutivelyLoadMMFCallback( ref MModelNativeTextMMFBase.Pair pair )
        {
            BucketValue bucketVal;

            if ( _Dictionary.TryGetValue( pair.TextPtr, out bucketVal ) )
            {
                var bucketRef = new BucketRef()
                {
                    Language   = pair.Language,
                    Weight     = pair.Weight,
                    NextBucket = bucketVal.NextBucket,
                };
                bucketVal.NextBucket = bucketRef;

                _Dictionary[ pair.TextPtr ] = bucketVal;

                #region commented. previous
                /*
                var bucketRef = new BucketRef() { Language = pair.Language, Weight = pair.Weight };
                if ( bucketVal.NextBucket == null )
                {
                    bucketVal.NextBucket = bucketRef;

                    _Dictionary[ pair.TextPtr ] = bucketVal;
                }
                else
                {
                    var br = bucketVal.NextBucket;
                    for ( ; br.NextBucket != null; br = br.NextBucket ) ;
                    br.NextBucket = bucketRef;
                } 
                */
                #endregion
            }
            else
            {
                var textPtr = StringsHelper.AllocHGlobalAndCopy( pair.TextPtr, pair.TextLength );
                _Dictionary.Add( textPtr, new BucketValue( pair.Language, pair.Weight ) );
            }
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
