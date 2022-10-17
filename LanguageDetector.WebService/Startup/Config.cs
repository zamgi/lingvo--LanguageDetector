using System.Configuration;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config : LanguageDetectorEnvironmentConfigImpl
    {        
        public Config()
        {
            MAX_INPUTTEXT_LENGTH              = int.Parse( ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH" ] );
            CONCURRENT_FACTORY_INSTANCE_COUNT = int.Parse( ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ] );
        }

        public int MAX_INPUTTEXT_LENGTH              { get; }
        public int CONCURRENT_FACTORY_INSTANCE_COUNT { get; }
    }
}