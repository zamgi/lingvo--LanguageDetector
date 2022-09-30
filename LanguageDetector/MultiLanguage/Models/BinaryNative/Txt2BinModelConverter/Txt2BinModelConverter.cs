using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace lingvo.ld.MultiLanguage.modelconverter
{
    /// <summary>
    /// 
    /// </summary>
    public struct Txt2BinModelConverterConfig
    {
        public IMModel Model                { get; set; }
        public int?   BufferSize            { get; set; }
        public string OutputFileName        { get; set; }
        public int?   OutputFileSizeInBytes { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Txt2BinModelConverter
    {
        private const int    DEFAULT_BUFFER_SIZE    = 0x2000;
        private const string DEFAULT_FILE_EXTENSION = ".bin";

        private IMModel _Model;
        private int    _BufferSize;
        private string _OutputDirectoryName;
        private string _OutputFileNamePattern;
        private string _OutputFileExtension;
        private int    _OutputFileSizeInBytes;
        private int    _OutputFileNumber;        

        private Txt2BinModelConverter( in Txt2BinModelConverterConfig config )
        {
            if ( config.Model == null ) throw (new ArgumentNullException( nameof(config.Model) ));
            if ( string.IsNullOrWhiteSpace( config.OutputFileName ) ) throw (new ArgumentNullException( nameof(config.OutputFileName) ));


            _Model                 = config.Model;
            _BufferSize            = config.BufferSize.GetValueOrDefault( DEFAULT_BUFFER_SIZE );
            _OutputDirectoryName   = Path.GetDirectoryName( config.OutputFileName );
            _OutputFileNamePattern = Path.GetFileNameWithoutExtension( config.OutputFileName );
            _OutputFileExtension   = Path.GetExtension( config.OutputFileName );
            if ( string.IsNullOrWhiteSpace( _OutputFileExtension ) )
            {
                _OutputFileExtension = DEFAULT_FILE_EXTENSION;
            }
            _OutputFileSizeInBytes = config.OutputFileSizeInBytes.GetValueOrDefault();
            _OutputFileNumber         = 0;
        }

        private void   IncrementOutputFileNumber() => _OutputFileNumber++;
        private void   EnsureOutputDirectoryExists()
        {
            if ( !Directory.Exists( _OutputDirectoryName ) )
            {
                Directory.CreateDirectory( _OutputDirectoryName );
            }
        }        
        private string GetOutputFileNameWithNumber() => Path.Combine( _OutputDirectoryName, _OutputFileNamePattern + '-' + _OutputFileNumber + _OutputFileExtension );
        private string GetSingleOutputFileName() => Path.Combine( _OutputDirectoryName, _OutputFileNamePattern + _OutputFileExtension );
        private static void DeleteFileIfExists( string fileName )
        {
            if ( File.Exists( fileName ) )
            {
                File.SetAttributes( fileName, FileAttributes.Normal );
                File.Delete( fileName );
            }
        }
        private static byte LanguageToByte( Language language )
        {
            checked
            {
                return ((byte) language);
            }
        }

        unsafe private IList< string > Save()
        {
            var bufferSize      = _BufferSize;
            var tempBuffer      = new byte[ bufferSize ];
            var outputFileNames = new List< string >(); 

            fixed ( byte* tempBufferBase = tempBuffer )
            using ( var allRecordsIterator = _Model.GetAllRecords().GetEnumerator() )
            {
                #region [.move to first record.]
                if ( !allRecordsIterator.MoveNext() )
                {
                    throw (new InvalidDataException( "No data found in model" ));
                } 
                #endregion

                for ( var endOfData = false; !endOfData; )
                {
                    IncrementOutputFileNumber();
                    var fileName = GetOutputFileNameWithNumber();                    
                    EnsureOutputDirectoryExists();
                    DeleteFileIfExists( fileName );
                    outputFileNames.Add( fileName );

                    using ( var fs = File.OpenWrite( fileName ) )
                    {
                        for ( var r = allRecordsIterator.Current; ; r = allRecordsIterator.Current )
                        {
                            #region [.write 'textPtr' as C#-chars (with double-byte-zero '\0').]
                            fixed ( char* ngramPtr = r.Ngram )
                            {
                                var tempBufferCharPtr = (char*) tempBufferBase;
                                for ( var idx = 0; ; idx++ )
                                {
                                    if ( bufferSize < idx )
                                    {
                                        throw (new InvalidDataException( string.Format( "WTF?!?! - buffer size is too small: [{0} < idx]", bufferSize ) ));
                                    }
                                    var ch = ngramPtr[ idx ];
                                    tempBufferCharPtr[ idx ] = ch;
                                    if ( ch == '\0' )
                                    {
                                        var len = 2 * (++idx);
                                        fs.Write( tempBuffer, 0, len ); 
                                        break;
                                    }
                                }
                            }
                            #endregion

                            #region [.write count of buckets.]
                            var weighByLanguages = r.WeighByLanguages.ToArray();

                            byte count = Convert.ToByte( weighByLanguages.Length );
                            fs.WriteByte( count );                
                            #endregion

                            #region [.write buckets.]
                            for ( var i = 0; i < count; i++ )
                            {
                                var wl = weighByLanguages[ i ];

                                //Language
                                fs.WriteByte( LanguageToByte( wl.Language ) );
                                //Weight
                                *((int*) tempBufferBase) = *((int*) &wl.Weight);
                                fs.Write( tempBuffer, 0, sizeof( int/*float*/) );
                            }                    
                            #endregion

                            #region [.move to next record.]
                            if ( !allRecordsIterator.MoveNext() )
                            {
                                endOfData = true;
                                break;
                            }
                            #endregion

                            #region [.check for file-size.]
                            if ( (0 < _OutputFileSizeInBytes) && (_OutputFileSizeInBytes <= fs.Length) )
                            {
                                break;
                            } 
                            #endregion
                        }
                    }
                }
            }

            #region [.if in result we have a single file, then rename him without file-number.]
            if ( _OutputFileNumber == 1 )
            {
                var sourceFileName = GetOutputFileNameWithNumber();
                var destFileName = GetSingleOutputFileName();

                DeleteFileIfExists( destFileName );
                File.Move( sourceFileName, destFileName );

                outputFileNames.Clear();
                outputFileNames.Add( destFileName );
            } 
            #endregion

            return (outputFileNames);
        }

        public static IList< string > Run( in Txt2BinModelConverterConfig config )
        {
            var converter = new Txt2BinModelConverter( config );
            var outputFileNames = converter.Save();
            return (outputFileNames);
        }
    }
}
