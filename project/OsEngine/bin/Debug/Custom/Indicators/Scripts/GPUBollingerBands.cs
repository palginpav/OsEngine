using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.GPU;
using OsEngine.GPU.ILGPUIndicators;

namespace OsEngine.Indicators
{
    /// <summary>
    /// GPU-accelerated Bollinger Bands indicator using ILGPU.
    /// Calculates upper, middle (SMA), and lower bands with configurable standard deviation multiplier.
    /// 
    /// GPU-ускоренный индикатор полос Боллинджера с использованием ILGPU.
    /// Вычисляет верхнюю, среднюю (SMA) и нижнюю полосы с настраиваемым множителем стандартного отклонения.
    /// </summary>
    [Indicator("GPUBollingerBands")]
    public class GPUBollingerBands : Aindicator
    {
        #region Fields

        private IndicatorParameterInt _length;
        private IndicatorParameterDecimal _standardDeviationMultiplier;
        private IndicatorParameterString _candlePoint;
        private IndicatorDataSeries _upperBand;
        private IndicatorDataSeries _middleBand;
        private IndicatorDataSeries _lowerBand;
        private ILGPUAccelerationManager _gpuManager;
        private ILGPUBollingerBands _gpuIndicator;
        private bool _gpuInitialized;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for GPU Bollinger Bands indicator.
        /// 
        /// Конструктор для GPU индикатора полос Боллинджера.
        /// </summary>
        public GPUBollingerBands()
        {
            _gpuInitialized = false;
        }

        #endregion

        #region Aindicator Implementation

        /// <summary>
        /// Handle indicator state changes.
        /// 
        /// Обработка изменений состояния индикатора.
        /// </summary>
        /// <param name="state">Indicator state / Состояние индикатора</param>
        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                // Create parameters
                _length = CreateParameterInt("Length", 20);
                _standardDeviationMultiplier = CreateParameterDecimal("Standard Deviation Multiplier", 2.0m);
                _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);

                // Create series
                _upperBand = CreateSeries("Upper Band", Color.Red, IndicatorChartPaintType.Line, true);
                _middleBand = CreateSeries("Middle Band", Color.Blue, IndicatorChartPaintType.Line, true);
                _lowerBand = CreateSeries("Lower Band", Color.Green, IndicatorChartPaintType.Line, true);

                // Initialize GPU acceleration
                InitializeGPU();
            }
        }

        /// <summary>
        /// Processes new candle data and calculates Bollinger Bands values using GPU acceleration.
        /// 
        /// Обрабатывает новые данные свечей и вычисляет значения полос Боллинджера с использованием GPU ускорения.
        /// </summary>
        /// <param name="candles">List of candles / Список свечей</param>
        /// <param name="index">Current candle index / Текущий индекс свечи</param>
        public override void OnProcess(List<Candle> candles, int index)
        {
            if (!_gpuInitialized || _gpuManager == null || _gpuIndicator == null)
            {
                return;
            }

            try
            {
                // Use GPU calculation for all available data
                ProcessWithGPU(candles, index);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"GPU Bollinger Bands calculation error at index {index}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clears the indicator data.
        /// 
        /// Очищает данные индикатора.
        /// </summary>
        public new void Clear()
        {
            _upperBand?.Clear();
            _middleBand?.Clear();
            _lowerBand?.Clear();
            _gpuInitialized = false;
            _gpuIndicator = null;
            _gpuManager = null;
        }

        /// <summary>
        /// Deletes the indicator and releases resources.
        /// 
        /// Удаляет индикатор и освобождает ресурсы.
        /// </summary>
        public new void Delete()
        {
            _gpuIndicator = null;
            _gpuManager?.Dispose();
            _gpuManager = null;
            _gpuInitialized = false;
            base.Delete();
        }

        #endregion

        #region GPU Processing

        /// <summary>
        /// Initializes GPU acceleration for Bollinger Bands calculation.
        /// 
        /// Инициализирует GPU ускорение для расчета полос Боллинджера.
        /// </summary>
        private void InitializeGPU()
        {
            try
            {
                _gpuManager = new ILGPUAccelerationManager();
                _gpuInitialized = _gpuManager.InitializeAsync().GetAwaiter().GetResult();
                
                if (!_gpuInitialized)
                {
                    throw new InvalidOperationException("GPU initialization returned false - GPU may not be available or compatible");
                }

                // Create GPU indicator with current parameters
                var parameters = new Dictionary<string, object>
                {
                    { "Period", _length.ValueInt },
                    { "StandardDeviationMultiplier", (float)_standardDeviationMultiplier.ValueDecimal },
                    { "CandlePoint", _candlePoint.ValueString },
                    { "Name", $"GPU_BollingerBands_{Name}" }
                };

                _gpuIndicator = (ILGPUBollingerBands)_gpuManager.CreateIndicator<ILGPUBollingerBands>(parameters);
            }
            catch (Exception ex)
            {
                _gpuInitialized = false;
                throw new InvalidOperationException($"GPU initialization failed for GPUBollingerBands indicator: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processes candle data using GPU acceleration.
        /// 
        /// Обрабатывает данные свечей с использованием GPU ускорения.
        /// </summary>
        /// <param name="candles">List of candles / Список свечей</param>
        /// <param name="index">Current candle index / Текущий индекс свечи</param>
        private void ProcessWithGPU(List<Candle> candles, int index)
        {
            try
            {
                // Calculate how many candles we need for the calculation
                int startIndex = Math.Max(0, index - _length.ValueInt * 2); // Extra buffer for calculation
                int endIndex = index + 1;
                
                if (endIndex - startIndex < _length.ValueInt)
                {
                    return; // Not enough data
                }

                // Prepare batch of candles for GPU processing
                var batchCandles = candles.GetRange(startIndex, endIndex - startIndex);
                
                // Calculate Bollinger Bands using GPU
                var result = _gpuIndicator.CalculateAsync(batchCandles).GetAwaiter().GetResult();
                
                if (result.IsSuccessful && result.Values != null && result.Values.Count > 0)
                {
                    // Set the Bollinger Bands values for the current index
                    int resultIndex = result.Values.Count - 1; // Last calculated value
                    if (resultIndex >= 0 && resultIndex < result.Values.Count)
                    {
                        // Set middle band (SMA)
                        _middleBand.Values[index] = result.Values[resultIndex];
                        
                        // Set upper band
                        if (result.UpperBandValues != null && resultIndex < result.UpperBandValues.Count)
                        {
                            _upperBand.Values[index] = result.UpperBandValues[resultIndex];
                        }
                        
                        // Set lower band
                        if (result.LowerBandValues != null && resultIndex < result.LowerBandValues.Count)
                        {
                            _lowerBand.Values[index] = result.LowerBandValues[resultIndex];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"GPU Bollinger Bands calculation error for GPUBollingerBands indicator at index {index}: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
