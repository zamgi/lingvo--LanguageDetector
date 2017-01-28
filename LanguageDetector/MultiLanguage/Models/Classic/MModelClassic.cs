using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lingvo.ld.MultiLanguage
{    
    /// <summary>
    /// 
    /// </summary>
    public sealed class MModelClassic : IMModel
    {
#if DEBUG
        /// <summary>
        /// 
        /// </summary>
        public static class FuskingTraitor
        {
            public static Dictionary< string, BucketValue > GetDictionary( MModelClassic model )
            {
                return (model._Dictionary);
            }
        } 
#endif

        #region [.private field's.]
        private Dictionary< string, BucketValue > _Dictionary;
        #endregion

        #region [.ctor().]
        public MModelClassic( MModelConfig config )
        {
            //var sw = Stopwatch.StartNew();
            //ParallelLoad_v1( config );
            //ParallelLoad_v2( config );
            //sw.Stop();
            //Console.WriteLine( "ParallelLoad: " + sw.Elapsed );

            //sw.Restart();
            ConsecutivelyLoad( config );
            //sw.Stop();
            //Console.WriteLine( "ConsecutivelyLoad: " + sw.Elapsed );            
        }
        public void Dispose()
        {
            if ( _Dictionary != null )
            {
                _Dictionary.Clear();
                _Dictionary = null;
            }
        } 
        #endregion

        #region [.model-dictionary loading.]
        private void ParallelLoad_v1( MModelConfig config )
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

            _Dictionary = dictBag.First();
            foreach ( var dict in dictBag.Skip( 1 ) )
            {
                foreach ( var pair in dict )
                {
                    var text          = pair.Key;
                    var bucketValElse = pair.Value;

                    if ( _Dictionary.TryGetValue( text, out bucketVal ) )
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

                            _Dictionary[ text ] = bucketVal;
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
                        _Dictionary.Add( text, bucketValElse );
                    }
                }
            }

            dictBag = null;
            GC.Collect();
        }
        private void ParallelLoad_v2( MModelConfig config )
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

            _Dictionary = new Dictionary< string, BucketValue >( cdict );
            cdict = null;
            GC.Collect();
        }
        private void ConsecutivelyLoad( MModelConfig config )
        {
            _Dictionary = (0 < config.ModelDictionaryCapacity) 
                         ? new Dictionary< string, BucketValue >( config.ModelDictionaryCapacity )
                         : new Dictionary< string, BucketValue >();

            var bucketVal = default(BucketValue);

            foreach ( var languageConfig in config.LanguageConfigs )
            {
                foreach ( var pair in languageConfig.GetModelFilenameContent() )
                {
                    var text   = pair.Key.ToUpperInvariant();
                    var weight = pair.Value;

                    if ( _Dictionary.TryGetValue( text, out bucketVal ) )
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

                            _Dictionary[ text ] = bucketVal;
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
                        _Dictionary.Add( text, new BucketValue( languageConfig.Language, weight ) );
                    }
                }
            }
        }
        #endregion

        #region [.IModel.]
        public int RecordCount
        {
            get { return (_Dictionary.Count); }
        }
        public bool TryGetValue( string ngram, out IEnumerable< WeighByLanguage > weighByLanguages )
        {
            BucketValue bucketVal;
            if ( _Dictionary.TryGetValue( ngram, out bucketVal ) )
            {
                weighByLanguages = new WeighByLanguageEnumerator( ref bucketVal ); //bucketVal.GetWeighByLanguages();
                return (true);
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
