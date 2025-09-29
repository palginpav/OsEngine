/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using OsEngine.Entity;

namespace OsEngine.GPU
{
    /// <summary>
    /// Interface for GPU-accelerated technical indicators in OsEngine.
    /// Provides standardized methods for GPU indicator calculation and parameter management.
    /// Интерфейс для GPU-ускоренных технических индикаторов в OsEngine.
    /// Предоставляет стандартизированные методы для вычисления GPU-индикаторов и управления параметрами.
    /// </summary>
    public interface IGPUIndicator
    {
        /// <summary>
        /// Calculates indicator values using GPU acceleration with real market data.
        /// Throws exception if calculation fails or data is invalid.
        /// Вычисляет значения индикатора с использованием GPU-ускорения с реальными рыночными данными.
        /// Выбрасывает исключение если вычисление не удалось или данные неверны.
        /// </summary>
        /// <param name="candles">List of real market candles / Список реальных рыночных свечей</param>
        /// <returns>Indicator calculation result with real values / Результат вычисления индикатора с реальными значениями</returns>
        /// <exception cref="ArgumentNullException">Thrown when candles parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when candles list is empty</exception>
        /// <exception cref="GPUCalculationException">Thrown when GPU calculation fails</exception>
        Task<GPUIndicatorResult> CalculateAsync(List<Candle> candles);
        
        /// <summary>
        /// Sets indicator parameters with validation and real value assignment.
        /// Throws exception if parameters are invalid or out of range.
        /// Устанавливает параметры индикатора с валидацией и присвоением реальных значений.
        /// Выбрасывает исключение если параметры неверны или вне диапазона.
        /// </summary>
        /// <param name="parameters">Dictionary of parameter names and real values / Словарь имен параметров и реальных значений</param>
        /// <exception cref="ArgumentNullException">Thrown when parameters is null</exception>
        /// <exception cref="ArgumentException">Thrown when parameter values are invalid</exception>
        void SetParameters(Dictionary<string, object> parameters);
        
        /// <summary>
        /// Indicates whether GPU acceleration is supported and available.
        /// Returns false if GPU is not available or initialization failed.
        /// Указывает поддерживается ли и доступно ли GPU-ускорение.
        /// Возвращает false если GPU недоступен или инициализация не удалась.
        /// </summary>
        bool IsGPUSupported { get; }
        
        /// <summary>
        /// Initializes GPU indicator with real accelerator context.
        /// Throws exception if initialization fails.
        /// Инициализирует GPU-индикатор с реальным контекстом ускорителя.
        /// Выбрасывает исключение если инициализация не удалась.
        /// </summary>
        /// <param name="accelerator">Real GPU accelerator instance / Реальный экземпляр GPU-ускорителя</param>
        /// <exception cref="ArgumentNullException">Thrown when accelerator is null</exception>
        /// <exception cref="GPUInitializationException">Thrown when GPU initialization fails</exception>
        void Initialize(Accelerator accelerator);
        
        /// <summary>
        /// Gets the name of the GPU indicator.
        /// Получает имя GPU-индикатора.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the indicator calculation period.
        /// Получает период вычисления индикатора.
        /// </summary>
        int Period { get; }
    }
}
