using System;

using lingvo.core;
using lingvo.urls;

namespace lingvo.ld.RussianLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RDetectorConfig
    {
        public float            Threshold
        {
            get;
            set;
        }
        public int              CyrillicLettersPercent
        {
            get;
            set;
        }
        public UrlDetectorModel UrlDetectorModel
        {
            get;
            set;
        }
    }
}
