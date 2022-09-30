using System;
using System.Collections.Generic;

using lingvo.core;

namespace lingvo.ld.RussianLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RModelClassic : IRModel
    {
        private HashSet< string > _Hashset;
        public RModelClassic( LanguageConfig config )
        {
            config.ThrowIfNull( nameof(config) );
            if ( config.Language != Language.RU ) throw (new ArgumentException( nameof(config.Language) ));

            _Hashset = new HashSet< string >();
            foreach ( var p in config.GetModelFilenameContent() )
            {
                var text = p.Key.ToUpperInvariant();

                _Hashset.Add( text );
            }
        }
        public void Dispose()
        {
            if ( _Hashset != null )
            {
                _Hashset.Clear();
                _Hashset = null;
            }
        }

        public bool Contains( string ngram ) => _Hashset.Contains( ngram );
        public IEnumerable< string > GetAllRecords()
        {
            foreach ( var ngram in _Hashset )
            {
                yield return (ngram);
            }
        }
        public int RecordCount => _Hashset.Count;
    }
}
