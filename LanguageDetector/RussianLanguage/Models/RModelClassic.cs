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
            config.ThrowIfNull( "config" );
            if ( config.Language != Language.RU )
                throw (new ArgumentException( "config.Language" ));

            _Hashset = new HashSet< string >();
            foreach ( var pair in config.GetModelFilenameContent() )
            {
                var text = pair.Key.ToUpperInvariant();

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

        public bool Contains( string ngram )
        {
            return (_Hashset.Contains( ngram ));
        }
        public IEnumerable< string > GetAllRecords()
        {
            foreach ( var ngram in _Hashset )
            {
                yield return (ngram);
            }
        }
        public int RecordCount
        {
            get { return (_Hashset.Count); }
        }
    }
}
