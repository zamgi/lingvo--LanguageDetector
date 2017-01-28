using System;
using System.Collections.Generic;
using System.Globalization;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class LanguageInfo
    {
        public LanguageInfo( Language language, float weight )
        {
            Language = language;
            Weight   = weight;   
            Percent  = 100;
        }		
        public LanguageInfo( Language language, float weight, int percent )
        {
            Language = language;
            Weight   = weight;   
            Percent  = percent;
        }

        public Language Language { get; private set; }
        public float    Weight   { get; private set; }
        public int      Percent  { get; private set; }

#if DEBUG
        private static NumberFormatInfo NFI = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        public override string ToString()
        {
            return (Language.ToString() + ':' + Weight.ToString( NFI ) + " (" + Percent + "%)");
        }  
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class LanguageInfoComparer : IComparer< LanguageInfo >
    {
        public static readonly LanguageInfoComparer Instance = new LanguageInfoComparer();
        private LanguageInfoComparer()
        {
        }
        
        public int Compare( LanguageInfo x, LanguageInfo y )
        {
            var d = y.Weight - x.Weight;
            if ( d == 0 )
                return (0);
            return (d > 0 ? 1 : -1);
        }
    }
}
