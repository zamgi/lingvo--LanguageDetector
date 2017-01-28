using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using lingvo.core;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        private static MModelRecord ToModelRecord( this KeyValuePair< string, BucketValue > pair )
        {
            return (new MModelRecord() { Ngram            = pair.Key, 
                                        WeighByLanguages = new WeighByLanguageEnumerator( pair.Value ) });
        }
        private static MModelRecord ToModelRecord( this KeyValuePair< IntPtr, BucketValue > pair )
        {
            return (new MModelRecord() { Ngram            = StringsHelper.ToString( pair.Key ), 
                                        WeighByLanguages = new WeighByLanguageEnumerator( pair.Value ) });
        }
        /*private static ModelRecord ToModelRecord( this KeyValuePair< IntPtr, IntPtr > pair )
        {
            return (new ModelRecord() { Ngram            = StringsHelper.ToString( pair.Key ), 
                                        WeighByLanguages = new WeighByLanguageEnumerator( pair.Value ) });
        }*/

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
        /*public static IEnumerable< ModelRecord > GetAllModelRecords( this Dictionary< IntPtr, IntPtr > dict )
        {
            foreach ( var p in dict )
            {
                yield return (p.ToModelRecord());
            }
        }*/
    }
}
