using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace lingvo.ld.Controllers
{
    [ApiController, Route("[controller]")]
    public sealed class ProcessController : ControllerBase
    {
        #region [.ctor().]
        private readonly ConcurrentFactory _ConcurrentFactory;        
#if DEBUG
        private readonly ILogger< ProcessController > _Logger;
#endif
#if DEBUG
        public ProcessController( ConcurrentFactory concurrentFactory, ILogger< ProcessController > logger )
        {
            _ConcurrentFactory = concurrentFactory;
            _Logger            = logger;
        }
#else
        public ProcessController( ConcurrentFactory concurrentFactory ) => _ConcurrentFactory = concurrentFactory;
#endif
        #endregion

        [HttpPost, Route("Run")] public async Task< IActionResult > Run( [FromBody] InitParamsVM m )
        {
            try
            {
#if DEBUG
                _Logger.LogInformation( $"start process: '{m.Text}'..." );
#endif
                var p = await _ConcurrentFactory.RunAsync( m.Text );
                var result = new ResultVM( m, p );
#if DEBUG
                _Logger.LogInformation( $"end process: '{m.Text}'." );
#endif
                return Ok( result );
            }
            catch ( Exception ex )
            {
#if DEBUG
                _Logger.LogError( $"Error while process: '{m.Text}' => {ex}" );
#endif
                return Ok( new ResultVM( m, ex ) ); //---return StatusCode( 500, new ResultVM( m, ex ) ); //Internal Server Error
            }
        }
    }
}
