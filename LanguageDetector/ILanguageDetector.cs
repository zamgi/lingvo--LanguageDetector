
namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILanguageDetector
    {
        LanguageInfo[] DetectLanguage( string text );
    }
}
