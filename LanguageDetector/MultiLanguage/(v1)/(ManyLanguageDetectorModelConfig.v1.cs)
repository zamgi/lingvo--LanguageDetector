using System.Collections.Generic;

using lingvo.core;

namespace lingvo.ld.v1
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ManyLanguageDetectorModelConfig
    {
        private readonly Dictionary< Language, LanguageConfigAdv > _Dictionary;

        public ManyLanguageDetectorModelConfig()
        {
            _Dictionary = new Dictionary< Language, LanguageConfigAdv >();
        }

        public void AddLanguageConfig( LanguageConfigAdv config )
        {
            config.ThrowIfNull("congif");

            _Dictionary.Add( config.Language, config );
        }

        public IEnumerable<LanguageConfigAdv> LanguageConfigs
        {
            get { return (_Dictionary.Values); }
        }
        public int ModelDictionaryCapacity
        {
            get;
            set;
        }
    }
}
