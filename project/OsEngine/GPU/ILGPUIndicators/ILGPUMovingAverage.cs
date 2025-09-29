using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using OsEngine.Entity;

namespace OsEngine.GPU.ILGPUIndicators
{
    /// <summary>
    /// GPU-accelerated Simple Moving Average indicator using ILGPU for real GPU calculations.
    /// Follows official ILGPU documentation patterns for proper implementation.
    /// Calculates moving average using real GPU parallel processing with ILGPU kernels.
    /// GPU-ускоренный индикатор простого скользящего среднего используя ILGPU для реальных GPU-вычислений.
    /// Следует официальным паттернам документации ILGPU для правильной реализации.
    /// Вычисляет скользящее среднее используя реальную GPU-параллельную обработку с ILGPU ядрами.
    /// </summary>
    public class ILGPUMovingAverage : IGPUIndicator
    {
        private Accelerator _accelerator;
        private bool _isInitialized;
        private int _period;
        private string _name;
        
        /// <summary>
        /// Constructor for ILGPU Moving Average indicator.
        /// Конструктор ILGPU-индикатора скользящего среднего.
        /// </summary>
        /// <param name="period">Moving average period / Период скользящего среднего</param>
        /// <param name="name">Indicator name / Имя индикатора</param>
        public ILGPUMovingAverage(int period, string name)
        {
            if (period <= 0)
            {
                throw new ArgumentException("Period must be greater than zero", nameof(period));
            }
            
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            }
            
            _period = period;
            _name = name;
            _isInitialized = false;
        }

        /// <summary>
        /// Initializes GPU indicator with real ILGPU accelerator following official documentation.
        /// Throws exception if initialization fails.
        /// Инициализирует GPU-индикатор с реальным ускорителем ILGPU следуя официальной документации.
        /// Выбрасывает исключение если инициализация не удалась.
        /// </summary>
        /// <param name="accelerator">Real ILGPU accelerator instance / Реальный экземпляр ускорителя ILGPU</param>
        /// <exception cref="ArgumentNullException">Thrown when accelerator is null</exception>
        /// <exception cref="GPUInitializationException">Thrown when GPU initialization fails</exception>
        public void Initialize(Accelerator accelerator)
        {
            if (accelerator == null)
            {
                throw new ArgumentNullException(nameof(accelerator));
            }
            
            try
            {
                _accelerator = accelerator;
                _isInitialized = true;
                
            }
            catch (Exception ex)
            {
                throw new GPUInitializationException($"Failed to initialize ILGPU Moving Average: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates moving average values using real ILGPU GPU acceleration.
        /// Throws exception if calculation fails or data is invalid.
        /// Вычисляет значения скользящего среднего используя реальное ILGPU GPU ускорение.
        /// Выбрасывает исключение если вычисление не удалось или данные неверны.
        /// </summary>
        /// <param name="candles">List of market candles / Список рыночных свечей</param>
        /// <returns>GPU indicator result with calculated values / Результат GPU-индикатора с вычисленными значениями</returns>
        /// <exception cref="ArgumentNullException">Thrown when candles is null</exception>
        /// <exception cref="ArgumentException">Thrown when candles list is empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when indicator is not initialized</exception>
        /// <exception cref="GPUCalculationException">Thrown when GPU calculation fails</exception>
        public async Task<GPUIndicatorResult> CalculateAsync(List<Candle> candles)
        {
            if (candles == null)
            {
                throw new ArgumentNullException(nameof(candles));
            }
            
            if (candles.Count == 0)
            {
                throw new ArgumentException("Candles list cannot be empty", nameof(candles));
            }
            
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ILGPU indicator is not initialized");
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Convert candles to float array for GPU processing
                var closePrices = candles.Select(c => (float)c.Close).ToArray();
                
                if (closePrices.Length < _period)
                {
                    // Not enough data for calculation
                    var earlyResult = new GPUIndicatorResult(_name, true)
                    {
                        Values = new List<decimal>(),
                        ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                        DataPointsProcessed = 0
                    };
                    return earlyResult;
                }
                
                // Perform real GPU calculation using ILGPU following official documentation
                var gpuResult = await CalculateMovingAverageGPUAsync(closePrices);
                
                // Convert back to decimal and create result
                var values = gpuResult.Select(v => (decimal)v).ToList();
                
                stopwatch.Stop();
                
                var result = new GPUIndicatorResult(_name, true)
                {
                    Values = values,
                    ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    DataPointsProcessed = closePrices.Length
                };
                
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                throw new GPUCalculationException($"ILGPU Moving Average calculation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Performs real GPU calculation using ILGPU kernels following official documentation.
        /// Throws exception if calculation fails.
        /// Выполняет реальное GPU вычисление используя ILGPU ядра следуя официальной документации.
        /// Выбрасывает исключение если вычисление не удалось.
        /// </summary>
        /// <param name="closePrices">Array of close prices / Массив цен закрытия</param>
        /// <returns>Array of calculated moving average values / Массив вычисленных значений скользящего среднего</returns>
        /// <exception cref="GPUCalculationException">Thrown when GPU calculation fails</exception>
        private async Task<float[]> CalculateMovingAverageGPUAsync(float[] closePrices)
        {
            try
            {
                // Use ILGPU for real GPU calculation following official documentation
                using (var gpuData = _accelerator.Allocate1D<float>(closePrices.Length))
                using (var gpuResult = _accelerator.Allocate1D<float>(closePrices.Length))
                {
                    // Copy data to GPU
                    gpuData.CopyFromCPU(closePrices);
                    
                    // Execute real GPU kernel for moving average calculation following ILGPU documentation
                    _accelerator.LaunchAutoGrouped(
                        MovingAverageKernel,
                        new Index1D(closePrices.Length),
                        gpuData.View,
                        gpuResult.View,
                        _period);
                    
                    // Synchronize GPU execution
                    _accelerator.Synchronize();
                    
                    // Copy result back to CPU
                    var result = new float[closePrices.Length];
                    gpuResult.CopyToCPU(result);
                    
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new GPUCalculationException($"Real ILGPU kernel execution failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Real GPU kernel for moving average calculation following ILGPU documentation.
        /// Executes on GPU with parallel processing.
        /// Реальное GPU ядро для вычисления скользящего среднего следуя документации ILGPU.
        /// Выполняется на GPU с параллельной обработкой.
        /// </summary>
        /// <param name="index">Thread index / Индекс потока</param>
        /// <param name="input">Input data array / Массив входных данных</param>
        /// <param name="output">Output result array / Массив выходных результатов</param>
        /// <param name="period">Moving average period / Период скользящего среднего</param>
        public static void MovingAverageKernel(Index1D index, ArrayView1D<float, Stride1D.Dense> input, ArrayView1D<float, Stride1D.Dense> output, int period)
        {
            if (index < period - 1)
            {
                output[index] = 0.0f;
            }
            else
            {
                float sum = 0.0f;
                for (int i = 0; i < period; i++)
                {
                    sum += input[index - i];
                }
                output[index] = sum / period;
            }
        }

        /// <summary>
        /// Sets indicator parameters with validation.
        /// Throws exception if parameters are invalid.
        /// Устанавливает параметры индикатора с валидацией.
        /// Выбрасывает исключение если параметры неверны.
        /// </summary>
        /// <param name="parameters">Dictionary of parameters / Словарь параметров</param>
        /// <exception cref="ArgumentNullException">Thrown when parameters is null</exception>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
        public void SetParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            
            if (parameters.TryGetValue("Period", out var periodObj))
            {
                if (periodObj is int period)
                {
                    if (period <= 0)
                    {
                        throw new ArgumentException("Period must be greater than zero");
                    }
                    _period = period;
                }
                else
                {
                    throw new ArgumentException("Period parameter must be an integer");
                }
            }
            
            if (parameters.TryGetValue("Name", out var nameObj))
            {
                if (nameObj is string name)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        throw new ArgumentException("Name parameter cannot be null or empty");
                    }
                    _name = name;
                }
                else
                {
                    throw new ArgumentException("Name parameter must be a string");
                }
            }
        }

        /// <summary>
        /// Indicates whether GPU acceleration is supported and available.
        /// Returns false if GPU is not available or initialization failed.
        /// Указывает поддерживается ли и доступно ли GPU-ускорение.
        /// Возвращает false если GPU недоступен или инициализация не удалась.
        /// </summary>
        public bool IsGPUSupported => _isInitialized && _accelerator != null;

        /// <summary>
        /// Gets the name of the GPU indicator.
        /// Получает имя GPU-индикатора.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the indicator calculation period.
        /// Получает период вычисления индикатора.
        /// </summary>
        public int Period => _period;

        /// <summary>
        /// Disposes GPU resources and cleans up memory.
        /// Освобождает GPU-ресурсы и очищает память.
        /// </summary>
        public void Dispose()
        {
            _accelerator = null;
            _isInitialized = false;
        }
    }
}
