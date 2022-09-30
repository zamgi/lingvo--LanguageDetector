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
                case Language.RU: return ("russian / �������");
                case Language.EN: return ("english / ����������");
                case Language.NL: return ("dutch / �����������");
                case Language.FI: return ("finnish / �������");
                case Language.SW: return ("swedish / ��������");
                case Language.UK: return ("ukrainian / ����������");
                case Language.BG: return ("bulgarian / ����������");
                case Language.BE: return ("belorussian / �����������");
                case Language.DE: return ("german / ��������");
                case Language.FR: return ("french / �����������");
                case Language.ES: return ("spanish / ���������");
                case Language.KK: return ("kazakh / ���������");
                case Language.PL: return ("polish / ��������");
                case Language.TT: return ("tatar / ���������");
                case Language.IT: return ("italian / �����������");
                //case Language.SR: return ("serbian / ��������");
                case Language.PT: return ("portuguese / �������������");
                case Language.DA: return ("danish / �������");
                case Language.CS: return ("czech / �������");
                case Language.NO: return ("norwegian / ����������");
            }
            return (language.ToString());
        }
    }
}
