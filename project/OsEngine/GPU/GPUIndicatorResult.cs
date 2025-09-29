/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;

namespace OsEngine.GPU
{
    /// <summary>
    /// Result of GPU indicator calculation containing real computed values.
    /// Результат вычисления GPU-индикатора содержащий реальные вычисленные значения.
    /// </summary>
    public class GPUIndicatorResult
    {
        /// <summary>
        /// List of calculated indicator values from GPU computation.
        /// Список вычисленных значений индикатора из GPU-вычислений.
        /// </summary>
        public List<decimal> Values { get; set; }
        
        /// <summary>
        /// List of signal line values for indicators with multiple components (e.g., MACD).
        /// Список значений сигнальной линии для индикаторов с несколькими компонентами (например, MACD).
        /// </summary>
        public List<decimal> SignalValues { get; set; }
        
        /// <summary>
        /// List of histogram values for indicators with multiple components (e.g., MACD).
        /// Список значений гистограммы для индикаторов с несколькими компонентами (например, MACD).
        /// </summary>
        public List<decimal> HistogramValues { get; set; }
        
        /// <summary>
        /// List of upper band values for Bollinger Bands indicator.
        /// Список значений верхней полосы для индикатора полос Боллинджера.
        /// </summary>
        public List<decimal> UpperBandValues { get; set; }
        
        /// <summary>
        /// List of lower band values for Bollinger Bands indicator.
        /// Список значений нижней полосы для индикатора полос Боллинджера.
        /// </summary>
        public List<decimal> LowerBandValues { get; set; }
        
        /// <summary>
        /// Name of the indicator that performed the calculation.
        /// Имя индикатора который выполнил вычисление.
        /// </summary>
        public string IndicatorName { get; set; }
        
        /// <summary>
        /// Indicates whether the calculation was performed on GPU.
        /// Указывает было ли вычисление выполнено на GPU.
        /// </summary>
        public bool IsGPUAccelerated { get; set; }
        
        /// <summary>
        /// Calculation execution time in milliseconds.
        /// Время выполнения вычисления в миллисекундах.
        /// </summary>
        public double ExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Number of data points processed.
        /// Количество обработанных точек данных.
        /// </summary>
        public int DataPointsProcessed { get; set; }
        
        /// <summary>
        /// Error message if calculation failed, null if successful.
        /// Сообщение об ошибке если вычисление не удалось, null если успешно.
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Indicates whether the calculation was successful.
        /// Указывает было ли вычисление успешным.
        /// </summary>
        public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage);
        
        /// <summary>
        /// Constructor for GPU indicator result.
        /// Конструктор результата GPU-индикатора.
        /// </summary>
        /// <param name="indicatorName">Name of the indicator / Имя индикатора</param>
        /// <param name="isGPUAccelerated">Whether GPU was used / Использовался ли GPU</param>
        public GPUIndicatorResult(string indicatorName, bool isGPUAccelerated)
        {
            IndicatorName = indicatorName ?? throw new ArgumentNullException(nameof(indicatorName));
            IsGPUAccelerated = isGPUAccelerated;
            Values = new List<decimal>();
            SignalValues = new List<decimal>();
            HistogramValues = new List<decimal>();
            UpperBandValues = new List<decimal>();
            LowerBandValues = new List<decimal>();
            ExecutionTimeMs = 0;
            DataPointsProcessed = 0;
        }
        
        /// <summary>
        /// Creates a successful result with calculated values.
        /// Создает успешный результат с вычисленными значениями.
        /// </summary>
        /// <param name="values">Calculated indicator values / Вычисленные значения индикатора</param>
        /// <param name="executionTimeMs">Execution time in milliseconds / Время выполнения в миллисекундах</param>
        /// <returns>Successful GPU indicator result / Успешный результат GPU-индикатора</returns>
        public static GPUIndicatorResult CreateSuccess(List<decimal> values, double executionTimeMs)
        {
            var result = new GPUIndicatorResult("Unknown", true)
            {
                Values = values ?? throw new ArgumentNullException(nameof(values)),
                ExecutionTimeMs = executionTimeMs,
                DataPointsProcessed = values.Count,
                ErrorMessage = null
            };
            return result;
        }
        
        /// <summary>
        /// Creates a failed result with error message.
        /// Создает неудачный результат с сообщением об ошибке.
        /// </summary>
        /// <param name="errorMessage">Error message / Сообщение об ошибке</param>
        /// <returns>Failed GPU indicator result / Неудачный результат GPU-индикатора</returns>
        public static GPUIndicatorResult CreateFailure(string errorMessage)
        {
            var result = new GPUIndicatorResult("Unknown", false)
            {
                ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
                Values = new List<decimal>(),
                ExecutionTimeMs = 0,
                DataPointsProcessed = 0
            };
            return result;
        }
    }
}
