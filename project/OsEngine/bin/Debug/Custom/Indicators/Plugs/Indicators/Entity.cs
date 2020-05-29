using System.Collections.Generic;


namespace OsEngine.Indicators
{
    public class Entity
    {

        public static readonly List<string> CandlePointsArray = new List<string>
            {"Open","High","Low","Close","Median","Typical","OHLC4"};

        /// <summary>
        /// what price of candle taken when building
        /// какая цена свечи берётся при построении
        /// </summary>
        public enum CandlePointType
        {
            /// <summary>
            /// Open
            /// открытие
            /// </summary>
            Open,

            /// <summary>
            /// High
            /// максимум
            /// </summary>
            High,

            /// <summary>
            /// Low
            /// минимум
            /// </summary>
            Low,

            /// <summary>
            /// Close
            /// закрытие
            /// </summary>
            Close,

            /// <summary>
            /// HL2 price (High + Low) / 2
            /// HL2 цена (High + Low) / 2
            /// </summary>
            Median,

            /// <summary>
            /// HLC3 price (High + Low + Close) / 3
            /// HLC3 цена (High + Low + Close) / 3
            /// </summary>
            Typical,

            /// <summary>
            /// OHLC4 price (Open + High + Low + Close) / 4
            /// OHLC4 цена (Open + High + Low + Close) / 4
            /// </summary>
            OHLC4
        }

    }
}