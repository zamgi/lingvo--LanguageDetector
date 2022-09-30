using lingvo.urls;

namespace lingvo.ld.MultiLanguage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MDetectorConfig
    {
        /// <summary>
        /// порог 10% по языку. меньше этого порога - отбрасывать в unk.
        /// </summary>
        private const int THRESHOLD_PERCENT                      = 10;
        /// <summary>
        /// если 3 и более языков, начиная с первого отличаются не более чем на 8% между собой, то это либо неизвестный язык, либо сильно смешенный – отбрасывать в unk. 
        /// </summary>
        private const int THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE   = 8;
        /// <summary>
        /// если в тексте более чем из 9 слов определилось не более 10% слов - отбрасывать его в unk.
        /// </summary>
        private const int THRESHOLD_DETECTING_WORD_COUNT         = 9;
        /// <summary>
        /// если в тексте более чем из 9 слов определилось не более 10% слов - отбрасывать его в unk.
        /// </summary>
        private const int THRESHOLD_PERCENT_DETECTING_WORD_COUNT = 10;
        /// <summary>
        /// порог на абсолютный вес для каждого языка
        /// </summary>
        private const float THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE   = 0.00001f;

        public MDetectorConfig()
        {
            ThresholdPercent                   = THRESHOLD_PERCENT;
            ThresholdPercentBetween3Language   = THRESHOLD_PERCENT_BETWEEN_3_LANGUAGE;
            ThresholdDetectingWordCount        = THRESHOLD_DETECTING_WORD_COUNT;
            ThresholdPercentDetectingWordCount = THRESHOLD_PERCENT_DETECTING_WORD_COUNT;
            ThresholdAbsoluteWeightLanguage    = THRESHOLD_ABSOLUTE_WEIGHT_LANGUAGE;
        }

        public UrlDetectorModel UrlDetectorModel { get; set; }
        /// <summary>
        /// порог 10% по языку. меньше этого порога - отбрасывать в unk.
        /// </summary>
        public int              ThresholdPercent { get; set; }
        /// <summary>
        /// если 3 и более языков, начиная с первого отличаются не более чем на 8% между собой, то это либо неизвестный язык, либо сильно смешенный – отбрасывать в unk. 
        /// </summary>
        public int              ThresholdPercentBetween3Language { get; set; }
        /// <summary>
        /// если в тексте более чем из 9 слов определилось не более 10% слов - отбрасывать его в unk.
        /// </summary>
        public int              ThresholdDetectingWordCount { get; set; }
        /// <summary>
        /// если в тексте более чем из 9 слов определилось не более 10% слов - отбрасывать его в unk.
        /// </summary>
        public int              ThresholdPercentDetectingWordCount { get; set; }
        /// <summary>
        /// порог на абсолютный вес для каждого языка
        /// </summary>
        public float            ThresholdAbsoluteWeightLanguage { get; set; }
    }
}
