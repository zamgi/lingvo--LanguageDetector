using System;
using System.Collections.Generic;

using lingvo.core;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MModelBinaryNativeConfig
    {
        private readonly HashSet< string > _ModelFilenames;

        public MModelBinaryNativeConfig()
        {
            _ModelFilenames = new HashSet< string >( StringComparer.InvariantCultureIgnoreCase );
        }
        public MModelBinaryNativeConfig( IEnumerable< string > modelFilenames )
        {
            modelFilenames.ThrowIfNullOrWhiteSpaceAnyElement( "modelFilenames" );

            _ModelFilenames = new HashSet< string >( modelFilenames, StringComparer.InvariantCultureIgnoreCase );
        }

        public void AddModelFilename( string modelFilename )
        {
            modelFilename.ThrowIfNullOrWhiteSpace( "modelFilename" );

            _ModelFilenames.Add( modelFilename );
        }

        public IEnumerable< string > ModelFilenames
        {
            get { return (_ModelFilenames); }
        }
        public int ModelDictionaryCapacity
        {
            get;
            set;
        }
    }
}
