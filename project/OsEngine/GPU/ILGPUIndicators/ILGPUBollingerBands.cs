using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using OsEngine.GPU;
using OsEngine.Entity;

namespace OsEngine.GPU.ILGPUIndicators
{
    /// <summary>
    /// GPU-accelerated Bollinger Bands indicator using ILGPU.
    /// Calculates upper, middle (SMA), and lower bands with configurable standard deviation multiplier.
    /// 
    /// GPU-ускоренный индикатор полос Боллинджера с использованием ILGPU.
    /// Вычисляет верхнюю, среднюю (SMA) и нижнюю полосы с настраиваемым множителем стандартного отклонения.
    /// </summary>
    public class ILGPUBollingerBands : IGPUIndicator
    {
        #region Fields

        private int _period;
        private float _standardDeviationMultiplier;
        private string _candlePoint;
        private string _name;
        private Accelerator _accelerator;
        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// Indicator name / Название индикатора
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Indicator period / Период индикатора
        /// </summary>
        public int Period => _period;

        /// <summary>
        /// Whether GPU acceleration is supported / Поддерживается ли GPU ускорение
        /// </summary>
        public bool IsGPUSupported => _isInitialized && _accelerator != null;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for GPU Bollinger Bands indicator.
        /// 
        /// Конструктор для GPU индикатора полос Боллинджера.
        /// </summary>
        /// <param name="period">Bollinger Bands period (typically 20) / Период полос Боллинджера (обычно 20)</param>
        /// <param name="standardDeviationMultiplier">Standard deviation multiplier (typically 2.0) / Множитель стандартного отклонения (обычно 2.0)</param>
        /// <param name="candlePoint">Candle point to use (Open, High, Low, Close) / Точка свечи для использования</param>
        /// <param name="name">Indicator name / Название индикатора</param>
        public ILGPUBollingerBands(int period, float standardDeviationMultiplier, string candlePoint, string name)
        {
            _period = period;
            _standardDeviationMultiplier = standardDeviationMultiplier;
            _candlePoint = candlePoint;
            _name = name ?? "ILGPU_BollingerBands_Default";
            _isInitialized = false;
        }

        #endregion

        #region IGPUIndicator Implementation

        /// <summary>
        /// Initialize the GPU indicator with accelerator.
        /// 
        /// Инициализация GPU индикатора с ускорителем.
        /// </summary>
        /// <param name="accelerator">ILGPU accelerator / ILGPU ускоритель</param>
        public void Initialize(Accelerator accelerator)
        {
            _accelerator = accelerator;
            _isInitialized = true;
        }

        /// <summary>
        /// Set indicator parameters.
        /// 
        /// Установка параметров индикатора.
        /// </summary>
        /// <param name="parameters">Parameter dictionary / Словарь параметров</param>
        public void SetParameters(Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("Period") && parameters["Period"] is int period)
                _period = period;
            if (parameters.ContainsKey("StandardDeviationMultiplier") && parameters["StandardDeviationMultiplier"] is float multiplier)
                _standardDeviationMultiplier = multiplier;
            if (parameters.ContainsKey("CandlePoint") && parameters["CandlePoint"] is string candlePoint)
                _candlePoint = candlePoint;
            if (parameters.ContainsKey("Name") && parameters["Name"] is string name)
                _name = name;
        }

        /// <summary>
        /// Calculate indicator values asynchronously using GPU.
        /// 
        /// Асинхронное вычисление значений индикатора с использованием GPU.
        /// </summary>
        /// <param name="candles">Input candle data / Входные данные свечей</param>
        /// <returns>Calculation result / Результат вычисления</returns>
        public async Task<GPUIndicatorResult> CalculateAsync(List<Candle> candles)
        {
            if (!_isInitialized || _accelerator == null)
            {
                return new GPUIndicatorResult(_name, false) { ErrorMessage = "GPU indicator not initialized" };
            }

            try
            {
                if (candles == null || candles.Count == 0)
                {
                    return new GPUIndicatorResult(_name, false) { ErrorMessage = "No candle data provided" };
                }

                // Extract price data from candles
                float[] inputArray = new float[candles.Count];
                for (int i = 0; i < candles.Count; i++)
                {
                    inputArray[i] = (float)GetCandleValue(candles[i]);
                }

                // Calculate Bollinger Bands on GPU
                var (upperBand, middleBand, lowerBand) = await CalculateBollingerBandsOnGPUAsync(inputArray);

                // Convert results back to decimal
                List<decimal> upperBandValues = new List<decimal>();
                List<decimal> middleBandValues = new List<decimal>();
                List<decimal> lowerBandValues = new List<decimal>();

                for (int i = 0; i < upperBand.Length; i++)
                {
                    upperBandValues.Add((decimal)Math.Round(upperBand[i], 2));
                    middleBandValues.Add((decimal)Math.Round(middleBand[i], 2));
                    lowerBandValues.Add((decimal)Math.Round(lowerBand[i], 2));
                }

                return new GPUIndicatorResult(_name, true)
                {
                    Values = middleBandValues, // Primary values (middle band)
                    UpperBandValues = upperBandValues,
                    LowerBandValues = lowerBandValues
                };
            }
            catch (Exception ex)
            {
                return new GPUIndicatorResult(_name, false) { ErrorMessage = $"GPU calculation error: {ex.Message}" };
            }
        }

        #endregion

        #region GPU Calculation Methods

        /// <summary>
        /// Calculate Bollinger Bands on GPU using SMA and standard deviation calculations.
        /// 
        /// Вычисление полос Боллинджера на GPU используя расчеты SMA и стандартного отклонения.
        /// </summary>
        /// <param name="inputData">Input price data / Входные ценовые данные</param>
        /// <returns>Tuple of upper, middle, and lower band arrays / Кортеж массивов верхней, средней и нижней полос</returns>
        private async Task<(float[] upperBand, float[] middleBand, float[] lowerBand)> CalculateBollingerBandsOnGPUAsync(float[] inputData)
        {
            // Use Task.Run to properly handle CPU-bound GPU work asynchronously
            return await Task.Run(() =>
            {
                // Allocate GPU memory
                using var inputBuffer = _accelerator.Allocate1D(inputData);
                using var upperBandBuffer = _accelerator.Allocate1D<float>(inputData.Length);
                using var middleBandBuffer = _accelerator.Allocate1D<float>(inputData.Length);
                using var lowerBandBuffer = _accelerator.Allocate1D<float>(inputData.Length);

                // Execute Bollinger Bands calculation kernel
                var bollingerKernel = _accelerator.LoadAutoGroupedStreamKernel<
                    Index1D,
                    ArrayView1D<float, Stride1D.Dense>,
                    ArrayView1D<float, Stride1D.Dense>,
                    ArrayView1D<float, Stride1D.Dense>,
                    ArrayView1D<float, Stride1D.Dense>,
                    int,
                    float>(BollingerBandsKernel);

                bollingerKernel(
                    (int)inputBuffer.Length,
                    inputBuffer.View,
                    upperBandBuffer.View,
                    middleBandBuffer.View,
                    lowerBandBuffer.View,
                    _period,
                    _standardDeviationMultiplier);

                _accelerator.Synchronize();

                // Copy results back to CPU
                var upperBand = new float[inputData.Length];
                var middleBand = new float[inputData.Length];
                var lowerBand = new float[inputData.Length];

                upperBandBuffer.CopyToCPU(upperBand);
                middleBandBuffer.CopyToCPU(middleBand);
                lowerBandBuffer.CopyToCPU(lowerBand);

                return (upperBand, middleBand, lowerBand);
            });
        }

        /// <summary>
        /// GPU kernel for Bollinger Bands calculation.
        /// Calculates upper, middle (SMA), and lower bands with standard deviation.
        /// 
        /// GPU ядро для расчета полос Боллинджера.
        /// Вычисляет верхнюю, среднюю (SMA) и нижнюю полосы со стандартным отклонением.
        /// </summary>
        /// <param name="index">Thread index / Индекс потока</param>
        /// <param name="input">Input price data / Входные ценовые данные</param>
        /// <param name="upperBand">Upper band output / Выход верхней полосы</param>
        /// <param name="middleBand">Middle band (SMA) output / Выход средней полосы (SMA)</param>
        /// <param name="lowerBand">Lower band output / Выход нижней полосы</param>
        /// <param name="period">Bollinger Bands period / Период полос Боллинджера</param>
        /// <param name="multiplier">Standard deviation multiplier / Множитель стандартного отклонения</param>
        public static void BollingerBandsKernel(
            Index1D index,
            ArrayView1D<float, Stride1D.Dense> input,
            ArrayView1D<float, Stride1D.Dense> upperBand,
            ArrayView1D<float, Stride1D.Dense> middleBand,
            ArrayView1D<float, Stride1D.Dense> lowerBand,
            int period,
            float multiplier)
        {
            if (index >= input.Length)
                return;

            // Not enough data for calculation
            if (index < period - 1)
            {
                upperBand[index] = 0.0f;
                middleBand[index] = 0.0f;
                lowerBand[index] = 0.0f;
                return;
            }

            // Calculate Simple Moving Average (middle band)
            float sma = CalculateSMA(input, index, period);
            middleBand[index] = sma;

            // Calculate standard deviation
            float standardDeviation = CalculateStandardDeviation(input, index, period, sma);

            // Calculate upper and lower bands
            float bandWidth = standardDeviation * multiplier;
            upperBand[index] = sma + bandWidth;
            lowerBand[index] = sma - bandWidth;
        }

        /// <summary>
        /// Calculate Simple Moving Average for Bollinger Bands.
        /// 
        /// Вычисление простого скользящего среднего для полос Боллинджера.
        /// </summary>
        /// <param name="input">Input data array / Массив входных данных</param>
        /// <param name="index">Current index / Текущий индекс</param>
        /// <param name="period">SMA period / Период SMA</param>
        /// <returns>SMA value / Значение SMA</returns>
        private static float CalculateSMA(ArrayView1D<float, Stride1D.Dense> input, int index, int period)
        {
            float sum = 0.0f;
            int startIndex = index - period + 1;
            
            for (int i = startIndex; i <= index; i++)
            {
                sum += input[i];
            }
            
            return sum / period;
        }

        /// <summary>
        /// Calculate standard deviation for Bollinger Bands.
        /// 
        /// Вычисление стандартного отклонения для полос Боллинджера.
        /// </summary>
        /// <param name="input">Input data array / Массив входных данных</param>
        /// <param name="index">Current index / Текущий индекс</param>
        /// <param name="period">Calculation period / Период расчета</param>
        /// <param name="sma">Simple Moving Average value / Значение простого скользящего среднего</param>
        /// <returns>Standard deviation value / Значение стандартного отклонения</returns>
        private static float CalculateStandardDeviation(ArrayView1D<float, Stride1D.Dense> input, int index, int period, float sma)
        {
            float sumSquaredDifferences = 0.0f;
            int startIndex = index - period + 1;
            
            // Calculate sum of squared differences from SMA
            for (int i = startIndex; i <= index; i++)
            {
                float difference = input[i] - sma;
                sumSquaredDifferences += difference * difference;
            }
            
            // Calculate variance and standard deviation
            float variance = sumSquaredDifferences / period;
            return (float)Math.Sqrt(variance);
        }

        /// <summary>
        /// Get candle value based on selected candle point.
        /// 
        /// Получение значения свечи на основе выбранной точки свечи.
        /// </summary>
        /// <param name="candle">Candle / Свеча</param>
        /// <returns>Candle value / Значение свечи</returns>
        private decimal GetCandleValue(Candle candle)
        {
            switch (_candlePoint.ToLower())
            {
                case "open":
                    return candle.Open;
                case "high":
                    return candle.High;
                case "low":
                    return candle.Low;
                case "close":
                default:
                    return candle.Close;
            }
        }

        #endregion
    }
}
