﻿using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections
{
    /// <summary>
    /// 
    /// </summary>
    internal static class HashHelpers
    {
        private const int HASH_PRIME = 101;
        // This is the maximum prime smaller than Array.MaxArrayLength
        public const int MaxPrimeArrayLength = 0x7FEFFFFD;

        // Table of prime numbers to use as hash table sizes. 
        // A typical resize algorithm would pick the smallest prime number in this array
        // that is larger than twice the previous capacity. 
        // Suppose our Hashtable currently has capacity x and enough elements are added 
        // such that a resize needs to occur. Resizing first computes 2x then finds the 
        // first prime in the table greater than 2x, i.e. if primes are ordered 
        // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n. 
        // Doubling is important for preserving the asymptotic complexity of the 
        // hashtable operations such as add.  Having a prime guarantees that double 
        // hashing does not lead to infinite loops.  IE, your hash function will be 
        // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
        public static readonly int[] primes =
        {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        };

        // Used by Hashtable and Dictionary's SeralizationInfo .ctor's to store the SeralizationInfo
        // object until OnDeserialization is called.
        /*private static ConditionalWeakTable<object, SerializationInfo> s_SerializationInfoTable;

        internal static ConditionalWeakTable<object, SerializationInfo> SerializationInfoTable
        {
            get
            {
                if ( s_SerializationInfoTable == null )
                {
                    var newTable = new ConditionalWeakTable<object, SerializationInfo>();
                    Interlocked.CompareExchange( ref s_SerializationInfoTable, newTable, null );
                }

                return s_SerializationInfoTable;
            }
        }
        */

        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        public static bool IsPrime( int candidate )
        {
            if ( (candidate & 1) != 0 )
            {
                int limit = (int) Math.Sqrt( candidate );
                for ( int divisor = 3; divisor <= limit; divisor += 2 )
                {
                    if ( (candidate % divisor) == 0 )
                        return false;
                }
                return true;
            }
            return (candidate == 2);
        }

        [ReliabilityContract( Consistency.WillNotCorruptState, Cer.Success )]
        public static int GetPrime( int min )
        {
            if ( min < 0 )
                throw (new ArgumentException( "CapacityOverflow" ));
            Contract.EndContractBlock();

            for ( int i = 0; i < primes.Length; i++ )
            {
                int prime = primes[ i ];
                if ( prime >= min )
                {
                    return (prime);
                }
            }

            //outside of our predefined table. 
            //compute the hard way. 
            for ( int i = (min | 1); i < Int32.MaxValue; i += 2 )
            {
                if ( IsPrime( i ) && ((i - 1) % HASH_PRIME != 0) )
                {
                    return (i);
                }
            }
            return (min);
        }

        public static int GetMinPrime()
        {
            return (primes[ 0 ]);
        }

        // Returns size of hashtable to grow to.
        public static int ExpandPrime( int oldSize )
        {
            int newSize = 2 * oldSize;

            // Allow the hashtables to grow to maximum possible size (~2G elements) before encoutering capacity overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ( (uint) newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize )
            {
                Contract.Assert( MaxPrimeArrayLength == GetPrime( MaxPrimeArrayLength ), "Invalid MaxPrimeArrayLength" );
                return MaxPrimeArrayLength;
            }

            return (GetPrime( newSize ));
        }
    }
}