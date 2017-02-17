using System;
using System.Collections.Generic;
using System.Linq;

namespace lingvo.ld
{
/*
    Германская группа:
        EN - английский  +
        NL - голландский +
        SW – шведский    +
        DE - немецкий    +
        DA – датский     +
        NO – норвежский  +

    Латинская группа:
        FR - французский   +
        ES – испанский     +
        IT – итальянский   +
        PT – португальский +

    Славянская группа (кириллица)
        KK - казахский   +
        TT – татарский   +
        RU - русский     +
        UK - украинский  +
        BG – болгарский  +
        BE – белорусский +

    Славянская группа (латиница)
        PL - польский +
        CS – чешский  +
 
    Другие
        FI – финский +
*/

    /// <summary>
    /// 
    /// </summary>
    public enum Language : byte
    {
        RU = 0,  // - русский       +
        EN = 1,  // - английский    +
        NL = 2,  // - голландский   +
        FI = 3,  // - финский       +
        SW = 4,  // – шведский      +
        UK = 5,  // - украинский    +
        BG = 6,  // – болгарский    +
        BE = 7,  // - белорусский   +
        DE = 8,  // - немецкий      +
        FR = 9,  // - французский   +
        ES = 10, // - испанский     +
        KK = 11, // - казахский     +
        PL = 12, // - польский      +
        TT = 13, // – татарский     +
        IT = 14, // – итальянский   +
        //---SR = 15, // – сербский
        PT = 16, // - португальский +
        DA = 17, // – датский    +
        CS = 18, // – чешский    +
        NO = 19, // – норвежский +

        LENGTH = NO + 1,
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Languages
    {
        public static IEnumerable< Language > All
        {
            get
            {
                foreach ( var lang in Enum.GetValues( typeof(Language) ).Cast< Language >() )
                {
                    if ( lang == Language.LENGTH )
                        continue;

                    yield return (lang);
                }
            }
        }
    }
}
