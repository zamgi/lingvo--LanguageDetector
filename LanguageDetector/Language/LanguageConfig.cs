using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using lingvo.core;

namespace lingvo.ld
{
    /// <summary>
    /// 
    /// </summary>
    public class LanguageConfig
    {   
        protected const string INVALIDDATAEXCEPTION_FORMAT_MESSAGE = "Wrong format of model-filename (file-name: '{0}', line# {1}, line-value: '{2}')";
        protected const           NumberStyles     NS  = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
        protected static readonly NumberFormatInfo NFI = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        public LanguageConfig( Language language, string modelFilename )
        {
            modelFilename.ThrowIfNullOrWhiteSpace("modelFilename");

            Language      = language;
            ModelFilename = modelFilename;

            if ( !File.Exists( ModelFilename ) )
                throw (new FileNotFoundException("File not found: '" + ModelFilename + '\'', ModelFilename));
        }

        public Language Language
        {
            get;
            private set;
        }
        public string   ModelFilename
        {
            get;
            private set;
        }

        public IEnumerable< KeyValuePair< string, float > > GetModelFilenameContent()
        {
            var SPLIT_CHARS = new[] { '\t' };

            using ( var sr = new StreamReader( ModelFilename ) )
            {
                var lineCount = 0;
                var line      = default(string);
                var weight    = default(float);

                for ( line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    if ( !line.StartsWith( "#" ) )
                        break;
                }

                for ( ; line != null; line = sr.ReadLine() )
                {
                    lineCount++;

                    var a = line.Split( SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries );
                    if ( a.Length != 2 )
                        throw (new InvalidDataException(string.Format(INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, line)));

                    var text = a[ 0 ].Trim();                    
                    if ( text.IsNullOrWhiteSpace() )
                        throw (new InvalidDataException(string.Format(INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, line)));
                    
                    if ( !float.TryParse( a[ 1 ].Trim(), NS, NFI, out weight ) )
                        throw (new InvalidDataException(string.Format(INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, line)));

                    yield return (new KeyValuePair< string, float >( text, weight ));                    
                }
            }
        }
#if DEBUG
        public override string ToString()
        {
            return (Language + ", '" + ModelFilename + '\'');
        } 
#endif
    }
}
