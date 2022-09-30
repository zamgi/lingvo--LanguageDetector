using System;
using System.Collections.Generic;

namespace lingvo.ld.RussianLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRModel : IDisposable
    {
        bool Contains( string ngram );
        IEnumerable< string > GetAllRecords();
        int RecordCount { get; }
    }
}
