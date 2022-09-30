using System;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILanguageDetector : IDisposable
    {
        LanguageInfo[] DetectLanguage( string text );
    }
}
