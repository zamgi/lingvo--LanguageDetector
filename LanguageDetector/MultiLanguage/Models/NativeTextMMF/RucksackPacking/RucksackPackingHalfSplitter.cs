using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace lingvo.ld.MultiLanguage.RucksackPacking
{
    /// <summary>
    /// 
    /// </summary>
    internal struct LanguageConfigExt
    {
        public LanguageConfig LanguageConfig { get; private set; }
        public long ModelFilenameLength { get; private set; }

        public static LanguageConfigExt[] From( IEnumerable< LanguageConfig > languageConfigs )
        {
            var array = (from lc in languageConfigs
                         select 
                            new LanguageConfigExt() 
                            { 
                                LanguageConfig      = lc, 
                                ModelFilenameLength = (new FileInfo( lc.ModelFilename )).Length 
                            }
                        ).ToArray();
            return (array);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal struct LanguageConfigPartition
    {
        public LanguageConfig[] LanguageConfigs { get; private set; }
        public long TotalModelFilenameLengths { get; private set; }

        public static LanguageConfigPartition From( IList< LanguageConfigExt > items )
        {
            var partition = new LanguageConfigPartition() { LanguageConfigs = new LanguageConfig[ items.Count ] };
            for ( var i = items.Count - 1; 0 <= i; i-- )
            {
                var item = items[ i ];
                partition.LanguageConfigs[ i ] = item.LanguageConfig;
                partition.TotalModelFilenameLengths += item.ModelFilenameLength;
            }
            return (partition);
        }
        public static LanguageConfigPartition[] From( IList< IList< LanguageConfigExt > > parts )
        {
            var result = new LanguageConfigPartition[ parts.Count ];
            for ( var i = parts.Count - 1; 0 <= i; i-- )
            {                
                result[ i ] = LanguageConfigPartition.From( parts[ i ] );
            }
            return (result);                
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class RucksackPackingHalfSplitter
    {
        /// <summary>
        /// 
        /// </summary>
        public struct Pair
        {
            internal static Pair Create( IList< LanguageConfigExt > left, IList< LanguageConfigExt > right )
            {
                var leftSum  = left .Sum( c => c.ModelFilenameLength );
                var rightSum = right.Sum( c => c.ModelFilenameLength );

                var d = new Pair()
                {
                    Left      = left,
                    Right     = right,
                    LeftSum   = leftSum,
                    RightSum  = rightSum,
                    SumDiff   = Math.Abs( leftSum    - rightSum ),
                    CountDiff = Math.Abs( left.Count - right.Count ),
                };
                return (d);
            }
            internal static Pair CreateByOne( IList< LanguageConfigExt > left )
            {
                var leftSum  = left.Sum( c => c.ModelFilenameLength );
                var rightSum = 0;

                var d = new Pair()
                {
                    Left      = left,
                    Right     = new LanguageConfigExt[ 0 ],
                    LeftSum   = leftSum,
                    RightSum  = rightSum,
                    SumDiff   = Math.Abs( leftSum    - rightSum ),
                    CountDiff = Math.Abs( left.Count - 0 ),
                };
                return (d);
            }

            public bool IsEmpty
            {
                get { return (Left == null && Right == null); } 
            }
            public bool HasRight
            {
                get { return (Right != null); }
            }
            public IList< LanguageConfigExt > Left  { get; private set; }
            public IList< LanguageConfigExt > Right { get; private set; }

            public long LeftSum   { get; private set; }
            public long RightSum  { get; private set; }
            public long SumDiff   { get; private set; }
            public int  CountDiff { get; private set; }

            public override string ToString()
            {
                if ( IsEmpty )
                {
                    return ("EMPTY");
                }
                return ("count-Left: " + Left.Count + ", count-Right: " + ((Right != null) ? Right.Count : 0) + " (diff: " + CountDiff + ")" + //---Left.ToText() + ", " + Right.ToText() +
                        "; sum-Left: " + LeftSum + ", sum-Right: " + RightSum + " (diff: " + SumDiff + ")"
                       );
            }

            /*public int CompareTo( Pair other )
            {
                var d = this.CountDiff.CompareTo( other.CountDiff );
                if ( d != 0 )
                    return (d);

                d = this.SumDiff.CompareTo( other.SumDiff );
                return (d);
            }*/
            public bool IsBetter( ref Pair other )
            {
                if ( other.IsEmpty )
                    return (true);

                var d = this.CountDiff.CompareTo( other.CountDiff );
                if ( d != 0 )
                    return (d < 0);

                d = this.SumDiff.CompareTo( other.SumDiff );
                if ( d != 0 )
                    return (d < 0);
                return (false);
            }

            /*public static bool operator >( Pair x, Pair y )
            {
                var d = x.CountDiff.CompareTo( y.CountDiff );
                if ( d != 0 )
                    return (d > 0);

                d = x.SumDiff.CompareTo( y.SumDiff );
                if ( d != 0 )
                    return (d > 0);
                return (false);
            }
            public static bool operator <( Pair x, Pair y )
            {
                var d = x.CountDiff.CompareTo( y.CountDiff );
                if ( d != 0 )
                    return (d < 0);

                d = x.SumDiff.CompareTo( y.SumDiff );
                if ( d != 0 )
                    return (d < 0);
                return (false);
            }*/
        }

        private IList< LanguageConfigExt > _Items;
        private bool[] _TempAffinity;
        private long   _MaxWeight;
        private long   _BestWeight;
        private Pair   _BestPair;

        public RucksackPackingHalfSplitter()
        {
        }
        public static Pair Split( IList< LanguageConfigExt > items )
        {
            return ((new RucksackPackingHalfSplitter()).SplitToHalf( items ));
        }
#if DEBUG
        public long FindIterationTotalCount
        {
            get;
            private set;
        } 
#endif
        public Pair SplitToHalf( IList< LanguageConfigExt > items )
        {
            _Items        = items;
            _TempAffinity = new bool[ items.Count ];
            _MaxWeight    = (items.Sum( c => c.ModelFilenameLength ) >> 1); //halt-of-total-sum
            _BestWeight   = 0;
            _BestPair     = default(Pair);
#if DEBUG
            FindIterationTotalCount = 0; 
#endif
            FindBestSplitRoutine( 0, 0 );

            if ( _BestPair.IsEmpty )
            {
                _BestPair = Pair.CreateByOne( items );
            }
            return (_BestPair);
        }

        /// <summary>
        /// reccurent routine
        /// </summary>
        /// <param name="idx">current index of element in '_Array'</param>
        /// <param name="currentWeight">current weight of reccurent iteration step</param>
        private void FindBestSplitRoutine( int idx, long currentWeight )
        {
#if DEBUG
            FindIterationTotalCount++; 
#endif
            if ( _Items.Count <= idx )
            {
                if ( _BestWeight <= currentWeight && currentWeight <= _MaxWeight )
                {
                    _BestWeight = currentWeight;
                    var pair = CreatePairFromTempAffinity();
                    if ( pair.IsBetter( ref _BestPair ) )
                    {
                        _BestPair = pair;
                    }
                }
            }
            else
            {
                // Если по весу проходит, то прибовляем стоимость и сумарный вес вызываем рекурсивно функцию search
                var nextWeight = currentWeight + _Items[ idx ].ModelFilenameLength;
                if ( nextWeight <= _MaxWeight )
                {
                    _TempAffinity[ idx ] = true;
                    FindBestSplitRoutine( idx + 1, nextWeight );
                }

                // Иначе не берем и смотрим следующий элемент
                _TempAffinity[ idx ] = false;
                FindBestSplitRoutine( idx + 1, currentWeight );
            }
        }

        private Pair CreatePairFromTempAffinity()
        {
            var items    = _Items;
            var affinity = _TempAffinity;

            var len   = affinity.Length;
            var left  = new List< LanguageConfigExt >( len );
            var right = new List< LanguageConfigExt >( len );
            for ( var i = 0; i < len; i++ )
            {
                if ( affinity[ i ] )
                {
                    left.Add( items[ i ] );
                }
                else
                {
                    right.Add( items[ i ] );
                }
            }
            return (Pair.Create( left, right ));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class RucksackPackingExtensions
    {
        private static bool IsPowerOfTwo( this int value )
        {
            return ((value & (value - 1)) == 0);
        }
        private static int GetNearestGreaterOrEqPowerOfTwo( this int value )
        {
            if ( value.IsPowerOfTwo() )
            {
                return (value);
            }

            const int BIT_COUNT      = 8 * sizeof(int);
            const int RIGHT_MOST_BIT = BIT_COUNT - 3;
            const long MAX_VALUE     = (0x1 << (RIGHT_MOST_BIT + 1));

            if ( MAX_VALUE <= value )
            {
                throw (new OverflowException());
            }

            for ( var i = RIGHT_MOST_BIT; 0 < i; i-- )
            {
                if ( ((value >> i) & 0x1) != 0 )
                {
                    return (0x1 << (i + 1));
                }
            }
            if ( value == 0 )
            {
                return (0x2);
            }

            //must never to be here
            throw (new InvalidOperationException("WTF?!?"));
        }
        private static int GetNearestLessOrEqPowerOfTwo( this int value )
        {
            if ( value.IsPowerOfTwo() )
            {
                return (value);
            }
            if ( value < 0 )
            {
                throw (new ArgumentException());
            }

            const int BIT_COUNT      = 8 * sizeof(int);
            const int RIGHT_MOST_BIT = BIT_COUNT - 1;

            for ( var i = RIGHT_MOST_BIT; 0 < i; i-- )
            {
                if ( ((value >> i) & 0x1) != 0 )
                {
                    return (0x1 << i);
                }
            }
            if ( value == 0 )
            {
                return (0x2);
            }

            //must never to be here
            throw (new InvalidOperationException("WTF?!?"));
        }

        public static IList< IList< LanguageConfigExt > > SplitByPartitionCountOrLess( this IList< LanguageConfigExt > items, int partitionCount )
        {
            if ( partitionCount < 2 )
            {
                return (new[] { items });
            }
            var lessPowerOfTwo = partitionCount.GetNearestLessOrEqPowerOfTwo();

            return (items.SplitByPowerOfTwo( lessPowerOfTwo ));
        }
        public static IList< IList< LanguageConfigExt > > SplitByPartitionCountOrGreater( this IList< LanguageConfigExt > items, int partitionCount )
        {
            if ( partitionCount < 2 )
            {
                return (new[] { items });
            }
            var greaterPowerOfTwo = partitionCount.GetNearestGreaterOrEqPowerOfTwo();

            return (items.SplitByPowerOfTwo( greaterPowerOfTwo ));
        }
        public static IList< IList< LanguageConfigExt > > SplitByPowerOfTwo( this IList< LanguageConfigExt > items, int powerOfTwo )
        {
            if ( !powerOfTwo.IsPowerOfTwo() )
            {
                throw (new ArgumentException("value '" + powerOfTwo + "' is not a power of two", "powerOfTwo"));
            }
            
            var result   = new List< IList< LanguageConfigExt > >( powerOfTwo );
            var splitter = new RucksackPackingHalfSplitter();
            var pair     = splitter.SplitToHalf( items );
            var queue    = new Queue< RucksackPackingHalfSplitter.Pair >( powerOfTwo );
            queue.Enqueue( pair );
            do
            {
                for ( var queueCount = queue.Count; 0 < queueCount; queueCount-- )
                {
                    pair = queue.Dequeue();

                    var pairLeft = splitter.SplitToHalf( pair.Left );
                    queue.Enqueue( pairLeft );

                    if ( pair.HasRight )
                    {
                        var pairRight = splitter.SplitToHalf( pair.Right );
                        queue.Enqueue( pairRight );
                    }
                }
            }
            while ( 2 < (powerOfTwo /= 2) );

            // result
            for ( ; queue.Count != 0; )
            {
                pair = queue.Dequeue();

                result.Add( pair.Left );

                if ( pair.HasRight )
                {
                    result.Add( pair.Right );
                }
            }
            return (result);
        }

        public static LanguageConfigPartition[] SplitByPartitionCountOrLess( this IEnumerable< LanguageConfig > languageConfigs, int partitionCount )
        {
            var parts = LanguageConfigExt.From( languageConfigs ).SplitByPartitionCountOrLess( partitionCount );
            return (LanguageConfigPartition.From( parts ));
        }
        public static LanguageConfigPartition[] SplitByPartitionCountOrGreater( this IEnumerable< LanguageConfig > languageConfigs, int partitionCount )
        {
            var parts = LanguageConfigExt.From( languageConfigs ).SplitByPartitionCountOrGreater( partitionCount );
            return (LanguageConfigPartition.From( parts ));
        }
    }
}
