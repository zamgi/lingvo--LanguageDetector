using System.Collections.Generic;

using lingvo.core;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MModelConfig
    {
        private readonly Dictionary< Language, LanguageConfig > _Dictionary;
        public MModelConfig() => _Dictionary = new Dictionary< Language, LanguageConfig >();

        public void AddLanguageConfig( LanguageConfig config )
        {
            config.ThrowIfNull( nameof(config) );
            _Dictionary.Add( config.Language, config );
        }

        public IEnumerable< LanguageConfig > LanguageConfigs => _Dictionary.Values;
        public int ModelDictionaryCapacity { get; set; }
    }
}
