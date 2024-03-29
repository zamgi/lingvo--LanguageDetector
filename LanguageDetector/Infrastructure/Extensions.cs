﻿using System;
using System.Collections.Generic;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.core
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static void ThrowIfNull( this object obj, string paramName )
        {
            if ( obj == null ) throw (new ArgumentNullException( paramName ));
        }
        public static void ThrowIfNullOrWhiteSpace( this string text, string paramName )
        {
            if ( string.IsNullOrWhiteSpace( text ) ) throw (new ArgumentNullException( paramName ));
        }
        /*public static void ThrowIfNullAnyElement< T >( this T[] array, string paramName )
        {
            if ( array == null )
                throw (new ArgumentNullException( paramName ));
            foreach ( var a in array )
            {
                if ( a == null )
                    throw (new ArgumentNullException( paramName + " => some array element is NULL" ));
            }
        }
        public static void ThrowIfNullAnyElement< T >( this ICollection< T > collection, string paramName )
        {
            if ( collection == null )
                throw (new ArgumentNullException( paramName ));
            foreach ( var c in collection )
            {
                if ( c == null )
                    throw (new ArgumentNullException( paramName + " => some collection element is NULL" ));
            }
        }
        */
        public static void ThrowIfNullOrWhiteSpaceAnyElement( this IEnumerable< string > sequence, string paramName )
        {
            if ( sequence == null ) throw (new ArgumentNullException( paramName ));

            foreach ( var c in sequence )
            {
                if ( string.IsNullOrWhiteSpace( c ) )
                {
                    throw (new ArgumentNullException( $"'{paramName}' => some collection element is NULL-or-WhiteSpace" ));
                }
            }
        }

        [M(O.AggressiveInlining)] public static bool IsNullOrWhiteSpace( this string text ) => string.IsNullOrWhiteSpace( text );
        [M(O.AggressiveInlining)] public static bool IsNullOrEmpty( this string text ) => string.IsNullOrEmpty( text );
    }
}
