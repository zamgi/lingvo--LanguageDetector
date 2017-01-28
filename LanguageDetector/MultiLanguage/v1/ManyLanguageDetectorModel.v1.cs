using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace lingvo.ld.v1
{    
    /// <summary>
    /// 
    /// </summary>
    public sealed class ManyLanguageDetectorModel : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public struct BucketValue
        {
            public BucketValue( Language language, float weight ) : this()
            {
                Language = language;
                Weight   = weight;
            }

            public Language Language
            {
                get;
                private set;
            }
            public float    Weight
            {
                get;
                private set;
            }

            public BucketRef NextBucket;

            public IEnumerable< BucketRef > GetAllBucketRef()
            {
                yield return (new BucketRef() { Language = this.Language, Weight = this.Weight, NextBucket = this.NextBucket });

                for ( var nb = this.NextBucket; nb != null; nb = nb.NextBucket )
                {
                    yield return (nb);
                }
            }

            public override string ToString()
            {
                return (Language + " : " + Weight);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public sealed class BucketRef
        {
            public Language Language
            {
                get;
                set;
            }
            public float    Weight
            {
                get;
                set;
            }

            public BucketRef NextBucket
            {
                get;
                set;
            }

            public override string ToString()
            {
                return (Language + " : " + Weight);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        unsafe private struct IntPtrEqualityComparer : IEqualityComparer< IntPtr >
        {
            public bool Equals( IntPtr x, IntPtr y )
            {
                if ( x == y )
                    return (true);

                for ( char* x_ptr = (char*) x,
                            y_ptr = (char*) y; ; x_ptr++, y_ptr++ )
                {
                    var x_ch = *x_ptr;
                    if ( x_ch != *y_ptr )
                        return (false);
                    if ( x_ch == '\0' )
                        return (true);
                }
            }
            public int GetHashCode( IntPtr obj )
            {
                char* ptr = (char*) obj;
                int n1 = 5381;
                int n2 = 5381;
                int n3;
                while ( (n3 = (int) (*(ushort*) ptr)) != 0 )
                {
                    n1 = ((n1 << 5) + n1 ^ n3);
                    n2 = ((n2 << 5) + n2 ^ n3);
                    ptr++;
                }
                return (n1 + n2 * 1566083941);

                #region commented
                /*
                char* ptr = (char*) obj;
                int n1 = 5381;
                int n2 = 5381;
                int n3;
                while ( (n3 = (int) (*(ushort*) ptr)) != 0 )
                {
                    n1 = ((n1 << 5) + n1 ^ n3);
                    n3 = (int) (*(ushort*) ptr);
                    if ( n3 == 0 )
                    {
                        break;
                    }
                    n2 = ((n2 << 5) + n2 ^ n3);
                    ptr += 2;
                }
                return (n1 + n2 * 1566083941);
                */
                #endregion
            }            
        }
        /*/// <summary>
        /// 
        /// </summary>
        private struct BucketRefEqualityComparer : IEqualityComparer< BucketRef >
        {
            public bool Equals( BucketRef x, BucketRef y )
            {
                return (x.Language == y.Language && x.Weight == y.Weight);
            }
            public int GetHashCode( BucketRef obj )
            {
                return (obj.Language.GetHashCode() ^ obj.Weight.GetHashCode());
            }
        }*/

        public ManyLanguageDetectorModel( ManyLanguageDetectorModelConfig config )
        {
            //var sw = Stopwatch.StartNew();
            //ParallelLoad_v1( config );
            //ParallelLoad_v2( config );
            //---            
            ParallelLoadMMF_DictionaryNative( config );

            /*
            ParallelLoadMMF_DictionaryIntptr( config );

            var dict1 = DictionaryIntptr;
            var dict2 = DictionaryNative;
            Debug.Assert( dict1.Count == dict2.Count );

            using ( var e1 = dict1.GetEnumerator() )
            using ( var e2 = dict2.GetEnumerator() )
            {
                BucketValue bucketVal;
                for ( ; e1.MoveNext() && e2.MoveNext(); )
                {
                    var s1 = e1.Current.Key;
                    var s2 = e2.Current.Key; //lingvo.core.StringsHelper.ToString( e2.Current.Key );

                    if ( !dict1.TryGetValue( s2, out bucketVal ) )
                    {
                        Debugger.Break();
                    }
                    else
                    {
                        var ar1 = bucketVal       .GetAllBucketRef().ToArray();
                        var ar2 = e2.Current.Value.GetAllBucketRef().ToArray();
                        Debug.Assert( ar1.Length == ar2.Length );

                        var isect = ar1.Intersect( ar2, default(BucketRefEqualityComparer) ).ToArray();
                        Debug.Assert( isect.Length == ar2.Length );
                    }

                    //fixed ( char* textPtr = s1 )
                    {
                        if ( !dict2.TryGetValue( s1 /*(IntPtr) textPtr* /, out bucketVal ) )
                        {
                            Debugger.Break();
                        }
                        else
                        {
                            var ar1 = bucketVal       .GetAllBucketRef().ToArray();
                            var ar2 = e1.Current.Value.GetAllBucketRef().ToArray();
                            Debug.Assert( ar1.Length == ar2.Length );

                            var isect = ar1.Intersect( ar2, default(BucketRefEqualityComparer) ).ToArray();
                            Debug.Assert( isect.Length == ar2.Length );
                        }
                    }
                }
            }
            */
            //sw.Stop();
            //Console.WriteLine( "ParallelLoad: " + sw.Elapsed );


            //sw.Restart();
            //---ConsecutivelyLoadMMF_DictionaryIntptr( config );

            /*
            ConsecutivelyLoad_Dictionary( config );

            Debug.Assert( Dictionary.Count == DictionaryIntptr.Count );

            using ( var e1 = Dictionary.GetEnumerator() )
            using ( var e2 = DictionaryIntptr.GetEnumerator() )
            {
                for ( ; e1.MoveNext() && e2.MoveNext(); )
                {
                    Debug.Assert( e1.Current.Key == StringsHelper.ToString( e2.Current.Key ) );
                }
            }*/

            //sw.Stop();
            //Console.WriteLine( "ConsecutivelyLoad: " + sw.Elapsed );            
        }
        ~ManyLanguageDetectorModel()
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
            if ( Dictionary != null )
            {
                Dictionary.Clear();
                Dictionary = null;
            }

            if ( DictionaryIntptr != null )
            {
                foreach ( var ptr in DictionaryIntptr.Keys )
                {
                    Marshal.FreeHGlobal( ptr );
                }
                DictionaryIntptr.Clear();
                DictionaryIntptr = null;
            }

            if ( DictionaryNative != null )
            {
                foreach ( var ptr in DictionaryNative.Keys )
                {
                    Marshal.FreeHGlobal( ptr );
                }
                DictionaryNative.Dispose();
                DictionaryNative = null;
            }
        }

        public Dictionary< string, BucketValue > Dictionary
        {
            get;
            private set;
        }
        public Dictionary< IntPtr, BucketValue > DictionaryIntptr
        {
            get;
            private set;
        }
        public DictionaryNative DictionaryNative
        {
            get;
            private set;
        }

        private void ParallelLoad_Dictionary_v1( ManyLanguageDetectorModelConfig config )
        {
            var dictBag = new ConcurrentBag< Dictionary< string, BucketValue > >();

            Parallel.ForEach( config.LanguageConfigs, 
                () => new Dictionary< string, BucketValue >(),
                (languageConfig, loopState, i, dict) =>
                {                    
                    var _bucketVal = default(BucketValue);

                    foreach ( var pair in languageConfig.GetModelFilenameContent() )
                    {
                        var text   = pair.Key.ToUpperInvariant();
                        var weight = pair.Value;

                        if ( dict.TryGetValue( text, out _bucketVal ) )
                        {
                            var bucketRef = new BucketRef() { Language = languageConfig.Language, Weight = weight };
                            if ( _bucketVal.NextBucket == null )
                            {
                                _bucketVal.NextBucket = bucketRef;

                                dict[ text ] = _bucketVal;
                            }
                            else
                            {
                                var br = _bucketVal.NextBucket;
                                for (; br.NextBucket != null; br = br.NextBucket );
                                br.NextBucket = bucketRef;
                            }
                        }
                        else
                        {
                            dict.Add( text, new BucketValue( languageConfig.Language, weight ) );
                        }
                    }
                    return (dict);
                },
                (dict) => 
                {
                    if ( dict.Count != 0 )
                    {
                        dictBag.Add( dict );
                    }
                }
            );

            var bucketVal = default(BucketValue);

            Dictionary = dictBag.First();
            foreach ( var dict in dictBag.Skip( 1 ) )
            {
                foreach ( var pair in dict )
                {
                    var text          = pair.Key;
                    var bucketValElse = pair.Value;

                    if ( Dictionary.TryGetValue( text, out bucketVal ) )
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

                            Dictionary[ text ] = bucketVal;
                        }
                        else
                        {
                            var br = bucketVal.NextBucket;
                            for (; br.NextBucket != null; br = br.NextBucket );
                            br.NextBucket = bucketRef;
                        }
                    }
                    else
                    {
                        Dictionary.Add( text, bucketValElse );
                    }
                }
            }

            dictBag = null;
            GC.Collect();
        }
        private void ParallelLoad_Dictionary_v2( ManyLanguageDetectorModelConfig config )
        {
            var concurrencyLevel = Environment.ProcessorCount;
            var cdict = (0 < config.ModelDictionaryCapacity)
                         ? new ConcurrentDictionary< string, BucketValue >( concurrencyLevel, config.ModelDictionaryCapacity )
                         : new ConcurrentDictionary< string, BucketValue >();

            Parallel.ForEach( config.LanguageConfigs, 
                new ParallelOptions() { MaxDegreeOfParallelism = concurrencyLevel },
                (languageConfig) =>
                {                    
                    var _bucketVal = default(BucketValue);

                    foreach ( var pair in languageConfig.GetModelFilenameContent() )
                    {
                        var text   = pair.Key.ToUpperInvariant();
                        var weight = pair.Value;

                        if ( cdict.TryGetValue( text, out _bucketVal ) )
                        {
                            lock ( cdict )
                            {
                                var bucketRef = new BucketRef() { Language = languageConfig.Language, Weight = weight };
                                if ( _bucketVal.NextBucket == null )
                                {
                                    _bucketVal.NextBucket = bucketRef;

                                    cdict[ text ] = _bucketVal;
                                }
                                else
                                {
                                    var br = _bucketVal.NextBucket;
                                    for (; br.NextBucket != null; br = br.NextBucket );
                                    br.NextBucket = bucketRef;
                                }
                            }
                        }
                        else
                        {
                            cdict.TryAdd( text, new BucketValue( languageConfig.Language, weight ) );
                        }
                    }
                }
            );

            Dictionary = new Dictionary< string, BucketValue >( cdict );
            cdict = null;
            GC.Collect();
        }
        private void ConsecutivelyLoad_Dictionary( ManyLanguageDetectorModelConfig config )
        {
            Dictionary = (0 < config.ModelDictionaryCapacity) 
                         ? new Dictionary< string, BucketValue >( config.ModelDictionaryCapacity )
                         : new Dictionary< string, BucketValue >();

            var bucketVal = default(BucketValue);

            foreach ( var languageConfig in config.LanguageConfigs )
            {
                foreach ( var pair in languageConfig.GetModelFilenameContent() )
                {
                    var text   = pair.Key.ToUpperInvariant();
                    var weight = pair.Value;

                    if ( Dictionary.TryGetValue( text, out bucketVal ) )
                    {
                        #region
                        /*
                        if ( bucketVal.Language == languageConfig.Language )
                        {
                            ...
                            _Dictionary[ text ] = bucketVal;
                        }
                        */                        
                        #endregion

                        var bucketRef = new BucketRef() { Language = languageConfig.Language, Weight = weight };
                        if ( bucketVal.NextBucket == null )
                        {
                            bucketVal.NextBucket = bucketRef;

                            Dictionary[ text ] = bucketVal;
                        }
                        else
                        {
                            var br = bucketVal.NextBucket;
                            for (; br.NextBucket != null; br = br.NextBucket );
                            br.NextBucket = bucketRef;
                        }                        
                    }
                    else
                    {
                        Dictionary.Add( text, new BucketValue( languageConfig.Language, weight ) );
                    }
                }
            }
        }

        private void ConsecutivelyLoadMMF_Dictionary( ManyLanguageDetectorModelConfig config )
        {
            Dictionary = (0 < config.ModelDictionaryCapacity) 
                         ? new Dictionary< string, BucketValue >( config.ModelDictionaryCapacity )
                         : new Dictionary< string, BucketValue >();

            var callback = new LanguageConfigAdv.LoadModelFilenameContentMMFCallback_v1( ConsecutivelyLoadMMFCallback_Dictionary );

            foreach ( var languageConfig in config.LanguageConfigs )
            {
                languageConfig.LoadModelFilenameContentMMF_v1( callback );
            }
        }
        private void ConsecutivelyLoadMMFCallback_Dictionary( ref LanguageConfigAdv.Pair_v1 pair )
        {
            BucketValue bucketVal;

            if ( Dictionary.TryGetValue( pair.Text, out bucketVal ) )
            {
                var bucketRef = new BucketRef() { Language = pair.Language, Weight = pair.Weight };
                if ( bucketVal.NextBucket == null )
                {
                    bucketVal.NextBucket = bucketRef;

                    Dictionary[ pair.Text ] = bucketVal;
                }
                else
                {
                    var br = bucketVal.NextBucket;
                    for (; br.NextBucket != null; br = br.NextBucket );
                    br.NextBucket = bucketRef;
                }                        
            }
            else
            {
                Dictionary.Add( pair.Text, new BucketValue( pair.Language, pair.Weight ) );
            }
        }

        private void ConsecutivelyLoadMMF_DictionaryIntptr( ManyLanguageDetectorModelConfig config )
        {
            DictionaryIntptr = (0 < config.ModelDictionaryCapacity) 
                ? new Dictionary< IntPtr, BucketValue >( config.ModelDictionaryCapacity, default(IntPtrEqualityComparer) )
                : new Dictionary< IntPtr, BucketValue >( default(IntPtrEqualityComparer) );

            var callback = new LanguageConfigAdv.LoadModelFilenameContentMMFCallback_v2( ConsecutivelyLoadMMFCallback_DictionaryIntptr );

            foreach ( var languageConfig in config.LanguageConfigs )
            {
                languageConfig.LoadModelFilenameContentMMF_v2( callback );
            }
        }
        unsafe private void ConsecutivelyLoadMMFCallback_DictionaryIntptr( ref LanguageConfigAdv.Pair_v2 pair )
        {
            BucketValue bucketVal;

            if ( DictionaryIntptr.TryGetValue( pair.TextPtr, out bucketVal ) )
            {
                var bucketRef = new BucketRef()
                {
                    Language = pair.Language,
                    Weight = pair.Weight,
                    NextBucket = bucketVal.NextBucket,
                };
                bucketVal.NextBucket = bucketRef;

                DictionaryIntptr[ pair.TextPtr ] = bucketVal;

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
                    for ( ; br.NextBucket != null; br = br.NextBucket ) ;
                    br.NextBucket = bucketRef;
                } 
                */
                #endregion
            }
            else
            {
                var textPtr = AllocHGlobalAndCopy( (char*) pair.TextPtr, pair.TextLength );
                DictionaryIntptr.Add( textPtr, new BucketValue( pair.Language, pair.Weight ) );
            }
        }
        unsafe private static IntPtr AllocHGlobalAndCopy( char* source, int sourceLength )
        {
            //alloc with include zero-'\0' end-of-string
            var destPtr = Marshal.AllocHGlobal( (sourceLength + 1) * sizeof(char) );
            var destination = (char*) destPtr;
            for ( ; 0 < sourceLength; sourceLength-- )
            {
                *(destination++) = *(source++);
            }
            *destination = '\0';
            return (destPtr);
        }

        /// <summary>
        /// 
        /// </summary>
        private struct ParallelLoadUnit_DictionaryIntptr
        {
            public Dictionary< IntPtr, BucketValue > DictionaryIntptr;
            public LanguageConfigAdv.LoadModelFilenameContentMMFCallback_v2 LoadMMFCallback;
            public int Capacity;

            unsafe private void LoadMMFCallbackRoutine( ref LanguageConfigAdv.Pair_v2 pair )
            {
                BucketValue bucketVal;

                if ( DictionaryIntptr.TryGetValue( pair.TextPtr, out bucketVal ) )
                {
                    var bucketRef = new BucketRef() 
                    { 
                        Language   = pair.Language, 
                        Weight     = pair.Weight,
                        NextBucket = bucketVal.NextBucket,
                    };
                    bucketVal.NextBucket = bucketRef;

                    DictionaryIntptr[ pair.TextPtr ] = bucketVal;

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
                    var textPtr = AllocHGlobalAndCopy( (char*) pair.TextPtr, pair.TextLength );
                    DictionaryIntptr.Add( textPtr, new BucketValue( pair.Language, pair.Weight ) );
                }
            }
            public void Initialize( int capacity )
            {
                if ( DictionaryIntptr == null )
                {
                    Capacity         = capacity;
                    DictionaryIntptr = new Dictionary< IntPtr, BucketValue >( capacity, default(IntPtrEqualityComparer) );
                    LoadMMFCallback  = new LanguageConfigAdv.LoadModelFilenameContentMMFCallback_v2( LoadMMFCallbackRoutine );
                }
            }

            public override string ToString()
            {
                return ("count: " + DictionaryIntptr.Count + ", (capacity: " + Capacity + ")");
            }
        }

        private void ParallelLoadMMF_DictionaryIntptr( ManyLanguageDetectorModelConfig config )
        {
            #region [.parallel load by partitions.]
            var processorCount = Environment.ProcessorCount;
            //var partitions = CreatePartitions( config.LanguageConfigs, processorCount );

            var partitions = config.LanguageConfigs.SplitByPartitionCountOrGreater( processorCount );

            var unitBag = new ConcurrentBag< ParallelLoadUnit_DictionaryIntptr >();

            Parallel.ForEach( partitions,
                new ParallelOptions() { MaxDegreeOfParallelism = processorCount },
                () => default(ParallelLoadUnit_DictionaryIntptr),
                (partition, loopState, i, unit) =>
                {
                    const int EMPIRICALLY_CHOSEN_FUSKING_NUMBER = 27;

                    var capacity = (int) (partition.TotalModelFilenameLengths / EMPIRICALLY_CHOSEN_FUSKING_NUMBER);
                    unit.Initialize( capacity );

                    foreach ( var languageConfig in partition.LanguageConfigs )
                    {
                        languageConfig.LoadModelFilenameContentMMF_v2( unit.LoadMMFCallback );
                    }

                    return (unit);
                },
                (unit) => 
                {
                    if ( unit.DictionaryIntptr.Count != 0 )
                    {
                        unitBag.Add( unit );
                    }
                }
            );
            #endregion

            #region [.merge.]
            var bucketVal = default(BucketValue);

            var dictionaryIntptr = (0 < config.ModelDictionaryCapacity) 
                ? new Dictionary< IntPtr, BucketValue >( config.ModelDictionaryCapacity, default(IntPtrEqualityComparer) )
                : new Dictionary< IntPtr, BucketValue >( default(IntPtrEqualityComparer) );

            foreach ( var dict in unitBag.Select( unit => unit.DictionaryIntptr ) )
            {
                foreach ( var pair in dict )
                {
                    var textPtr       = pair.Key;
                    var bucketValElse = pair.Value;

                    if ( dictionaryIntptr.TryGetValue( textPtr, out bucketVal ) )
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

                            dictionaryIntptr[ textPtr ] = bucketVal;
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
                        dictionaryIntptr.Add( textPtr, bucketValElse );
                    }
                }
                //dict.Clear(); //--- too slow => TODO: вытащить словарь/pull dictionary ---//
            }

            DictionaryIntptr = dictionaryIntptr;
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

        /// <summary>
        /// 
        /// </summary>
        private struct ParallelLoadUnit_DictionaryNative
        {
            public DictionaryNative DictionaryNative;
            public LanguageConfigAdv.LoadModelFilenameContentMMFCallback_v2 LoadMMFCallback;
            public int Capacity;

            public void Initialize( int capacity )
            {
                if ( DictionaryNative == null )
                {
                    Capacity         = capacity;
                    DictionaryNative = new DictionaryNative( capacity );
                    LoadMMFCallback  = new LanguageConfigAdv.LoadModelFilenameContentMMFCallback_v2( DictionaryNative.AddNewOrToEndOfChain );
                }
            }

            public override string ToString()
            {
                return ("count: " + DictionaryNative.Count + ", (capacity: " + Capacity + ")");
            }
        }

        private void ParallelLoadMMF_DictionaryNative( ManyLanguageDetectorModelConfig config )
        {
            #region [.parallel load by partitions.]
            var processorCount = Environment.ProcessorCount;

            var partitions = config.LanguageConfigs.SplitByPartitionCountOrGreater( processorCount );

            var unitBag = new ConcurrentBag< ParallelLoadUnit_DictionaryNative >();

            Parallel.ForEach( partitions,
                new ParallelOptions() { MaxDegreeOfParallelism = processorCount },
                () => default(ParallelLoadUnit_DictionaryNative),
                (partition, loopState, i, unit) =>
                {
                    const int EMPIRICALLY_CHOSEN_FUSKING_NUMBER = 27;

                    var capacity = (int) (partition.TotalModelFilenameLengths / EMPIRICALLY_CHOSEN_FUSKING_NUMBER);
                    unit.Initialize( capacity );

                    foreach ( var languageConfig in partition.LanguageConfigs )
                    {
                        languageConfig.LoadModelFilenameContentMMF_v2( unit.LoadMMFCallback );
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
            var dictionaryNative = new DictionaryNative( config.ModelDictionaryCapacity );

            foreach ( var dict in unitBag.Select( unit => unit.DictionaryNative ) )
            {
                dictionaryNative.MergeWith( dict );

                dict.Dispose();
            }

            DictionaryNative = dictionaryNative;
            unitBag = null;
            #endregion
        }
    }
}
