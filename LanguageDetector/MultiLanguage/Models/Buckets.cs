using System;
using System.Collections;
using System.Collections.Generic;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
#if DEBUG
    public struct BucketValue
#else
    internal struct BucketValue
#endif
    {
        public BucketValue( Language language, float weight ) : this()
        {
            Language = language;
            Weight   = weight;
        }

        public Language Language
        {
            get;
            private set;
        }
        public float    Weight
        {
            get;
            private set;
        }

        public BucketRef NextBucket;

        public IEnumerable< BucketRef > GetAllBucketRef()
        {
            yield return (new BucketRef() { Language = this.Language, Weight = this.Weight, NextBucket = this.NextBucket });

            for ( var nb = this.NextBucket; nb != null; nb = nb.NextBucket )
            {
                yield return (nb);
            }
        }
        public IEnumerable< WeighByLanguage > GetWeighByLanguages()
        {
            yield return (new WeighByLanguage() { Language = this.Language, Weight = this.Weight });

            for ( var next = this.NextBucket; next != null; next = next.NextBucket )
            {
                yield return (new WeighByLanguage() { Language = next.Language, Weight = next.Weight });
            }            
        }

        public override string ToString()
        {
            return (Language + " : " + Weight);
        }
    }

    /// <summary>
    /// 
    /// </summary>
#if DEBUG
    public sealed class BucketRef
#else
    internal sealed class BucketRef
#endif
    {
        public Language Language
        {
            get;
            set;
        }
        public float    Weight
        {
            get;
            set;
        }

        public BucketRef NextBucket
        {
            get;
            set;
        }

        public override string ToString()
        {
            return (Language + " : " + Weight);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal struct WeighByLanguageEnumerator : IEnumerator< WeighByLanguage >, IEnumerable< WeighByLanguage >
    {
        private BucketRef _Next;
        private WeighByLanguage _Current;
        private bool _IsStarted;

        public WeighByLanguageEnumerator( in BucketValue bucketVal )
        {                
            _IsStarted = false;                
            _Next = bucketVal.NextBucket;
            _Current = new WeighByLanguage() { Language = bucketVal.Language, Weight = bucketVal.Weight };
        }

        public WeighByLanguage Current => _Current;
        public bool MoveNext()
        {
            if ( !_IsStarted )
            {
                _IsStarted = true;
                return (true);
            }

            if ( _Next != null )
            {
                _Current = new WeighByLanguage() { Language = _Next.Language, Weight = _Next.Weight };
                _Next = _Next.NextBucket;
                return (true);
            }

            return (false);
        }
        public void Dispose() { }
        object IEnumerator.Current => _Current;
        public void Reset() => throw (new NotSupportedException());
        WeighByLanguageEnumerator GetEnumerator() => this;
        IEnumerator< WeighByLanguage > IEnumerable< WeighByLanguage >.GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class IntPtrEqualityComparer : IEqualityComparer< IntPtr >
    {
        public static IntPtrEqualityComparer Inst { get; } = new IntPtrEqualityComparer();
        private IntPtrEqualityComparer() { }
        public bool Equals( IntPtr x, IntPtr y )
        {
            if ( x == y )
                return (true);

            for ( char* x_ptr = (char*) x,
                        y_ptr = (char*) y; ; x_ptr++, y_ptr++ )
            {
                var x_ch = *x_ptr;
                if ( x_ch != *y_ptr )
                    return (false);
                if ( x_ch == '\0' )
                    return (true);
            }
        }
        public int GetHashCode( IntPtr obj )
        {
            char* ptr = (char*) obj;
            int n1 = 5381;
            int n2 = 5381;
            int n3;
            while ( (n3 = (int) (*(ushort*) ptr)) != 0 )
            {
                n1 = ((n1 << 5) + n1 ^ n3);
                n2 = ((n2 << 5) + n2 ^ n3);
                ptr++;
            }
            return (n1 + n2 * 1566083941);
        }            
    }
}
