using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using lingvo.ld.MultiLanguage;
using lingvo.ld.RussianLanguage;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
	public sealed class ConcurrentFactory : ILanguageDetector
	{
		private readonly int                         _InstanceCount;		
        private SemaphoreSlim                        _Semaphore;
        private ConcurrentStack< ILanguageDetector > _Stack;

        public ConcurrentFactory( MDetectorConfig config, IMModel model, int instanceCount )
		{
            if ( instanceCount <= 0 ) throw (new ArgumentException( nameof(instanceCount) ));
            if ( config == null     ) throw (new ArgumentNullException( nameof(config) ));
            if ( model  == null     ) throw (new ArgumentNullException( nameof(model) ));

            _InstanceCount = instanceCount;
            _Semaphore     = new SemaphoreSlim( _InstanceCount, _InstanceCount );
            _Stack         = new ConcurrentStack< ILanguageDetector >();
			for ( int i = 0; i < _InstanceCount; i++ )
			{
                _Stack.Push( new MDetector( config, model ) );
			}
		}
        public ConcurrentFactory( RDetectorConfig config, IRModel model, int instanceCount )
        {
            if ( instanceCount <= 0 ) throw (new ArgumentException( nameof(instanceCount) ));
            if ( config == null     ) throw (new ArgumentNullException( nameof(config) ));
            if ( model  == null     ) throw (new ArgumentNullException( nameof(model) ));

            _InstanceCount = instanceCount;
            _Semaphore     = new SemaphoreSlim( _InstanceCount, _InstanceCount );
            _Stack         = new ConcurrentStack< ILanguageDetector >();
            for ( int i = 0; i < _InstanceCount; i++ )
            {
                _Stack.Push( new RDetector( config, model ) );
            }
        }
        public void Dispose()
        {
            foreach ( var worker in _Stack )
            {
                worker.Dispose();
            }
            _Stack.Clear();
            _Semaphore.Dispose();
        }

        public LanguageInfo[] DetectLanguage( string text ) => Run( text );
        public LanguageInfo[] Run( string text )
		{
            _Semaphore.Wait();
            var worker = default(ILanguageDetector);
			var result = default(LanguageInfo[]);
			try
			{
                worker = _Stack.Pop();
                result = worker.DetectLanguage( text );
			}
			finally
			{
                if ( worker != null )
				{
                    _Stack.Push( worker );
				}
				_Semaphore.Release();
			}
			return (result);
		}
        public async Task< LanguageInfo[] > RunAsync( string text )
		{
            await _Semaphore.WaitAsync().ConfigureAwait( false );
            var worker = default(ILanguageDetector);
			var result = default(LanguageInfo[]);
			try
			{
                worker = _Stack.Pop();
                result = worker.DetectLanguage( text );
			}
			finally
			{
                if ( worker != null )
				{
                    _Stack.Push( worker );
				}
				_Semaphore.Release();
			}
			return (result);
		}
	}

    /// <summary>
    /// 
    /// </summary>
    internal static class ConcurrentFactoryExtensions
    {
        public static T Pop< T >( this ConcurrentStack< T > stack ) => stack.TryPop( out var t ) ? t : default;
    }
}
