using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using lingvo.ld.MultiLanguage;
using lingvo.ld.RussianLanguage;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
	internal sealed class ConcurrentFactory : ILanguageDetector //, IDisposable
	{
		private readonly int                         _InstanceCount;		
		private Semaphore                            _Semaphore;
        private ConcurrentStack< ILanguageDetector > _Stack;
        //private bool                                 _Disposed;
        //private ManyLanguageDetectorModel            _ManyLanguageDetectorModel; //need to hold for suppress finalizer
        //private IMModel                              _IMModel;
        //private IRModel                              _IRModel;

        /*public ConcurrentFactory( MDetectorConfig config, ManyLanguageDetectorModel model, int instanceCount )
		{
            if ( instanceCount <= 0 ) throw (new ArgumentException("instanceCount"));
            if ( config == null     ) throw (new ArgumentNullException("config"));
            if ( model  == null     ) throw (new ArgumentNullException("model"));

            _InstanceCount = instanceCount;
            _Semaphore     = new Semaphore( _InstanceCount, _InstanceCount );
            _Stack         = new ConcurrentStack< ILanguageDetector >();
			for ( int i = 0; i < _InstanceCount; i++ )
			{
                _Stack.Push( new ManyLanguageDetector( config, model ) );
			}
            //_ManyLanguageDetectorModel = model;
		}*/
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
            //_IRModel = model;
        }

        public LanguageInfo[] DetectLanguage( string text )
		{
            /*if ( _Disposed )
			{
				throw (new ObjectDisposedException( this.GetType().Name ));
			}*/

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

		/*public bool IsDisposed
		{
			get { return (_Disposed); }
		}
		public void Dispose()
		{
            if ( !_Disposed )
			{
				_Disposed = true;
				for ( int i = 0; i < _InstanceCount; i++ )
				{
					_Semaphore.WaitOne();
				}
				_Semaphore.Release( _InstanceCount );
				_Semaphore = null;
				_Stack     = null;				
			}
		}
        */
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
