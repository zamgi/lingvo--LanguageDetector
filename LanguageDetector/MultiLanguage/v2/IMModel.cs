using System;
using System.Collections.Generic;
using System.Linq;
using lingvo.core;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public struct WeighByLanguage
    {
        public float    Weight;
        public Language Language;
#if DEBUG
        public override string ToString()
        {
            return (Language.ToString() + ':' + Weight.ToString());
        } 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public struct MModelRecord
    {
        public string Ngram;
        public IEnumerable< WeighByLanguage > WeighByLanguages;
#if DEBUG
        public override string ToString()
        {
            return (Ngram + " => {" + string.Join( ", ", WeighByLanguages.Select( t => t.ToString() ) ) + '}');
        } 
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IMModel : IDisposable
    {
        bool TryGetValue( string ngram, out IEnumerable< WeighByLanguage > weighByLanguages );

        IEnumerable< MModelRecord > GetAllRecords();

        int RecordCount { get; }
    }
}
