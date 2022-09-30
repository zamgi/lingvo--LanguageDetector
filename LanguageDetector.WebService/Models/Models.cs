using System;
using System.Collections.Generic;
using System.Linq;

using JP = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public struct InitParamsVM
    {
        public string Text { get; set; }

#if DEBUG
        public override string ToString() => Text;
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ResultVM
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly struct language_info_t
        {
            [JP("l")] public string language          { get; init; }
            [JP("n")] public string language_fullname { get; init; }
            [JP("p")] public float  percent           { get; init; }
        }

        public ResultVM( in InitParamsVM m, Exception ex ) : this() => (InitParams, ExceptionMessage) = (m, ex.ToString());
        public ResultVM( in InitParamsVM m, LanguageInfo[] languageInfos ) : this()
        {
            InitParams    = m;
            LanguageInfos = (from li in languageInfos
                                select
                                new language_info_t()
                                {
                                    language          = li.Language.ToString(),
                                    language_fullname = li.Language.ToText(),
                                    percent           = li.Percent,
                                }
                            ).ToList();
        }

        [JP("ip")   ] public InitParamsVM InitParams       { get; }
        [JP("err")  ] public string       ExceptionMessage { get; }
        [JP("langs")] public IReadOnlyList< language_info_t > LanguageInfos { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static string ToText( this Language language )
        {
            switch ( language )
            {
                case Language.RU: return ("russian / русский");
                case Language.EN: return ("english / английский");
                case Language.NL: return ("dutch / голландский");
                case Language.FI: return ("finnish / финский");
                case Language.SW: return ("swedish / шведский");
                case Language.UK: return ("ukrainian / украинский");
                case Language.BG: return ("bulgarian / болгарский");
                case Language.BE: return ("belorussian / белорусский");
                case Language.DE: return ("german / немецкий");
                case Language.FR: return ("french / французский");
                case Language.ES: return ("spanish / испанский");
                case Language.KK: return ("kazakh / казахский");
                case Language.PL: return ("polish / польский");
                case Language.TT: return ("tatar / татарский");
                case Language.IT: return ("italian / итальянский");
                //case Language.SR: return ("serbian / сербский");
                case Language.PT: return ("portuguese / португальский");
                case Language.DA: return ("danish / датский");
                case Language.CS: return ("czech / чешский");
                case Language.NO: return ("norwegian / норвежский");
            }
            return (language.ToString());
        }
    }
}
