using System;
using System.Collections.Generic;

using lingvo.core;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        [M(O.AggressiveInlining)] 
        private static MModelRecord ToModelRecord( this in KeyValuePair< string, BucketValue > p ) => new MModelRecord() { Ngram            = p.Key, 
                                                                                                                           WeighByLanguages = new WeighByLanguageEnumerator( p.Value ) };

        [M(O.AggressiveInlining)] 
        private static MModelRecord ToModelRecord( this in KeyValuePair< IntPtr, BucketValue > p ) => new MModelRecord() { Ngram            = StringsHelper.ToString( p.Key ), 
                                                                                                                           WeighByLanguages = new WeighByLanguageEnumerator( p.Value ) };

        public static IEnumerable< MModelRecord > GetAllModelRecords( this Dictionary< string, BucketValue > dict )
        {
            foreach ( var p in dict )
            {
                yield return (p.ToModelRecord());
            }            
        }
        public static IEnumerable< MModelRecord > GetAllModelRecords( this Dictionary< IntPtr, BucketValue > dict )
        {
            foreach ( var p in dict )
            {
                yield return (p.ToModelRecord());
            }             
        }
        public static IEnumerable< MModelRecord > GetAllModelRecords( this DictionaryNative dict )
        {
            foreach ( var p in dict )
            {
                yield return (p.ToModelRecord());
            }             
        }
    }
}
