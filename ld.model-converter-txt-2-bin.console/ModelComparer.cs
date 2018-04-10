#if DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

using lingvo.ld.MultiLanguage;

namespace lingvo.ld.modelconverter
{
    using WeighByLanguageNative = MModelBinaryNative.WeighByLanguageNative;

    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LanguageWeightPair
    {
        public float    Weight;
        public Language Language;

        public override string ToString()
        {
            return (Language.ToString() + ':' + Weight.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal struct LanguageWeightPairEqualityComparer : IEqualityComparer< LanguageWeightPair >
    {
        public bool Equals( LanguageWeightPair x, LanguageWeightPair y )
        {
            return (x.Language == y.Language && x.Weight == y.Weight);
        }
        public int GetHashCode( LanguageWeightPair obj )
        {
            return (obj.Language.GetHashCode() ^ obj.Weight.GetHashCode());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class ModelComparer
    {
        private static LanguageWeightPair[] Intersect( LanguageWeightPair[] x, LanguageWeightPair[] y )
        {
            var isect = x.Intersect( y, default(LanguageWeightPairEqualityComparer) ).ToArray();
            return (isect);
        }

        unsafe public static void Compare( MModelBinaryNative model_1, MModelClassic model_2 )
        {
            var dict1 = MModelBinaryNative.FuskingTraitor.GetDictionary( model_1 );
            var dict2 = MModelClassic     .FuskingTraitor.GetDictionary( model_2 ); 

            Debug.Assert( dict1.Count == dict2.Count );

            using ( var e1 = dict1.GetEnumerator() )
            using ( var e2 = dict2.GetEnumerator() )
            {
                BucketValue bucketVal;
                IntPtr weighByLanguagesBasePtr;
                for ( ; e1.MoveNext() && e2.MoveNext(); )
                {
                    var s1 = e1.Current.Key;
                    var s2 = e2.Current.Key;

                    //Debug.Assert( s1.ToText() == s2 );

                    fixed ( char* s2_ptr = s2 )
                    if ( !dict1.TryGetValue( (IntPtr) s2_ptr, out weighByLanguagesBasePtr ) )
                    {
                        Debugger.Break();
                    }
                    if ( !dict2.TryGetValue( s1.ToText(), out bucketVal ) )
                    {
                        Debugger.Break();
                    }

                    {
                        var ar1 = e1.Current.Value.ToWeighByLanguageNative().ToLanguageWeightPairs();
                        var ar2 = bucketVal.GetAllBucketRef().ToLanguageWeightPairs();
                        Debug.Assert( ar1.Length == ar2.Length );

                        var isect = Intersect( ar1, ar2 );
                        Debug.Assert( isect.Length == ar2.Length );
                    }
                    {
                        var ar1 = weighByLanguagesBasePtr.ToWeighByLanguageNative().ToLanguageWeightPairs();
                        var ar2 = e2.Current.Value.GetAllBucketRef().ToLanguageWeightPairs();
                        Debug.Assert( ar1.Length == ar2.Length );

                        var isect = Intersect( ar1, ar2 );
                        Debug.Assert( isect.Length == ar2.Length );
                    }
                }
            }
        }

        /*unsafe private static void Compare( DictionaryNative dict1, Dictionary< IntPtr, LanguageWeightPair[] > dict2 )
        {
            Debug.Assert( dict1.Count == dict2.Count );

            using ( var e1 = dict1.GetEnumerator() )
            using ( var e2 = dict2.GetEnumerator() )
            {
                BucketValue bucketVal;
                LanguageWeightPair[] pairs;
                for ( ; e1.MoveNext() && e2.MoveNext(); )
                {
                    var s1 = e1.Current.Key;
                    var s2 = e2.Current.Key;

                    Debug.Assert( s1.ToText() == s2.ToText() );

                    if ( !dict1.TryGetValue( s1, out bucketVal ) )
                    {
                        Debugger.Break();
                    }
                    if ( !dict2.TryGetValue( s2, out pairs ) )
                    {
                        Debugger.Break();
                    }

                    {
                        var ar1 = bucketVal.GetAllBucketRef().ToLanguageWeightPairs().ToArray();
                        var ar2 = e2.Current.Value;
                        Debug.Assert( ar1.Length == ar2.Length );

                        var isect = ar1.Intersect( ar2, default(LanguageWeightPairEqualityComparer) ).ToArray();
                        Debug.Assert( isect.Length == ar2.Length );
                    }
                    {
                        var ar1 = pairs;
                        var ar2 = e1.Current.Value.GetAllBucketRef().ToLanguageWeightPairs().ToArray();
                        Debug.Assert( ar1.Length == ar2.Length );

                        var isect = ar1.Intersect( ar2, default(LanguageWeightPairEqualityComparer) ).ToArray();
                        Debug.Assert( isect.Length == ar2.Length );
                    }
                }
            }
        }*/
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class Extensions
    {
        public static int GetLength( char* _base )
        {
            for ( var ptr = _base; ; ptr++ )
            {
                if ( *ptr == '\0' )
                {
                    return ((int)(ptr - _base));
                }
            }
        }
        public static int GetLength( this IntPtr _base )
        {
            return (GetLength( (char*) _base ));
        }

        public static string ToText( char* value )
        {
            if ( value == null )
            {
                return (null);
            }

            var length = GetLength( value );
            if ( length == 0 )
            {
                return (string.Empty);
            }

            var str = new string( '\0', length );
            fixed ( char* str_ptr = str )
            {                
                for ( var wf_ptr = str_ptr; ; )
                {
                    var ch = *(value++);
                    if ( ch == '\0' )
                        break;
                    *(wf_ptr++) = ch;
                }
            }
            return (str);
        }
        public static string ToText( char* value, int length )
        {
            if ( value == null )
            {
                return (null);
            }

            if ( length == 0 )
            {
                return (string.Empty);
            }

            var str = new string( '\0', length );
            fixed ( char* str_ptr = str )
            {
                for ( var wf_ptr = str_ptr; 0 < length; length-- )
                {
                    var ch = *(value++);
                    if ( ch == '\0' )
                        break;
                    *(wf_ptr++) = ch;
                }
            }
            return (str);
        }
        public static string ToText( this IntPtr value )
        {
            return (ToText( (char*) value ));
        }
        public static string ToText( this IntPtr value, int length )
        {
            return (ToText( (char*) value, length ));
        }

        public static LanguageWeightPair[] ToLanguageWeightPairs( this WeighByLanguageNative wbln )
        {
            var array = new LanguageWeightPair[ wbln.CountBuckets ];
            for ( var i = 0; i < wbln.CountBuckets; i++ )
            {
                var wbn = &wbln.WeighByLanguagesBasePtr[ i ];
                array[ i ] = new LanguageWeightPair() { Language = wbn->Language, Weight = wbn->Weight };
            }
            return (array);
        }
        public static LanguageWeightPair[] ToLanguageWeightPairs( this IEnumerable< BucketRef > brs )
        {
            return (brs.Select( br => new LanguageWeightPair() { Language = br.Language, Weight = br.Weight } ).ToArray());
        }

        public static WeighByLanguageNative ToWeighByLanguageNative( this IntPtr value )
        {
            return (WeighByLanguageNative.Create( value ));
        }
    }
}

#endif