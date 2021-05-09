using System;
using System.Collections.Concurrent;
using System.Threading;

using lingvo.ld.MultiLanguage;
using lingvo.ld.RussianLanguage;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
	internal sealed class ConcurrentFactory : ILanguageDetector
	{
		private readonly int                         _InstanceCount;		
		private Semaphore                            _Semaphore;
        private ConcurrentStack< ILanguageDetector > _Stack;

        public ConcurrentFactory( MDetectorConfig config, IMModel model, int instanceCount )
		{
            if ( instanceCount <= 0 ) throw (new ArgumentException("instanceCount"));
            if ( config == null     ) throw (new ArgumentNullException("config"));
            if ( model  == null     ) throw (new ArgumentNullException("model"));

            _InstanceCount = instanceCount;
            _Semaphore     = new Semaphore( _InstanceCount, _InstanceCount );
            _Stack         = new ConcurrentStack< ILanguageDetector >();
			for ( int i = 0; i < _InstanceCount; i++ )
			{
                _Stack.Push( new MDetector( config, model ) );
			}
            //_IMModel = model;
		}
        public ConcurrentFactory( RDetectorConfig config, IRModel model, int instanceCount )
        {
            if ( instanceCount <= 0 ) throw (new ArgumentException( "instanceCount" ));
            if ( config == null     ) throw (new ArgumentNullException( "config" ));
            if ( model  == null     ) throw (new ArgumentNullException("model"));

            _InstanceCount = instanceCount;
            _Semaphore     = new Semaphore( _InstanceCount, _InstanceCount );
            _Stack         = new ConcurrentStack< ILanguageDetector >();
            for ( int i = 0; i < _InstanceCount; i++ )
            {
                _Stack.Push( new RDetector( config, model ) );
            }
        }

        public LanguageInfo[] DetectLanguage( string text )
		{
			_Semaphore.WaitOne();
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
        public static T Pop< T >( this ConcurrentStack< T > stack )
        {
            T t;
            if ( stack.TryPop( out t ) )
                return (t);
            return (default(T));
        }
    }
}
