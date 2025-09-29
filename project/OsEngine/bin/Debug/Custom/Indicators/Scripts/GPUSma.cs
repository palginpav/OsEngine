using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using OsEngine.Entity;
using OsEngine.GPU;
using OsEngine.GPU.ILGPUIndicators;
using OsEngine.Market;

namespace OsEngine.Indicators
{
    /// <summary>
    /// GPU-accelerated Simple Moving Average indicator for OsEngine.
    /// Provides fast calculation of moving average using parallel processing on GPU.
    /// GPU-ускоренный индикатор простого скользящего среднего для OsEngine.
    /// Обеспечивает быстрое вычисление скользящего среднего с использованием параллельной обработки на GPU.
    /// </summary>
    [Indicator("GPUSma")]
    public class GPUSma : Aindicator
    {
        private IndicatorParameterInt _length;
        private IndicatorParameterString _candlePoint;
        private IndicatorDataSeries _series;
        private ILGPUAccelerationManager _gpuManager;
        private bool _gpuInitialized;

        /// <summary>
        /// Constructor for GPU SMA indicator.
        /// Конструктор для GPU SMA индикатора.
        /// </summary>
        public GPUSma()
        {
        }

        /// <summary>
        /// Initializes the GPU SMA indicator with parameters and GPU acceleration.
        /// Инициализирует GPU SMA индикатор с параметрами и GPU-ускорением.
        /// </summary>
        /// <param name="state">Indicator state / Состояние индикатора</param>
        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _length = CreateParameterInt("Length", 14);
                _candlePoint = CreateParameterStringCollection("Candle Point", "Close", Entity.CandlePointsArray);
                _series = CreateSeries("GPU MA", Color.DodgerBlue, IndicatorChartPaintType.Line, true);


                // Initialize GPU acceleration
                InitializeGPU();
            }
        }

        /// <summary>
        /// Processes candle data using GPU acceleration when available, falls back to CPU calculation.
        /// Обрабатывает данные свечей с использованием GPU-ускорения когда доступно, иначе использует CPU вычисления.
        /// </summary>
        /// <param name="candles">List of candles / Список свечей</param>
        /// <param name="index">Current candle index / Текущий индекс свечи</param>
        public override void OnProcess(List<Candle> candles, int index)
        {
            if (_length.ValueInt > index)
            {
                _series.Values[index] = 0;
                return;
            }


            if (_gpuInitialized && _gpuManager != null)
            {
                // Use GPU acceleration for all calculations - no CPU fallbacks
                if (index >= _length.ValueInt)
                {
                    ProcessWithGPU(candles, index);
                }
                else
                {
                    _series.Values[index] = 0;
                }
            }
            else
            {
                // GPU not available - throw exception instead of CPU fallback
                throw new InvalidOperationException($"GPU acceleration not available for GPUSma indicator. GPU initialized: {_gpuInitialized}, GPU manager: {_gpuManager != null}");
            }
        }

        /// <summary>
        /// Clears indicator data and resets state.
        /// Очищает данные индикатора и сбрасывает состояние.
        /// </summary>
        public new void Clear()
        {
            try
            {
                // Clear all series data
                if (_series != null)
                {
                    _series.Values.Clear();
                }
                
                // Call base clear to handle standard cleanup
                base.Clear();
                
                // Reset GPU state
                _gpuInitialized = false;
            }
            catch (Exception ex)
            {
                base.Clear(); // Ensure base cleanup still happens
            }
        }

        /// <summary>
        /// Initializes GPU acceleration system.
        /// Инициализирует систему GPU-ускорения.
        /// </summary>
        private void InitializeGPU()
        {
            try
            {
                _gpuManager = new ILGPUAccelerationManager();
                
                // Initialize GPU synchronously - block until completion
                _gpuInitialized = _gpuManager.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _gpuInitialized = false;
                throw new InvalidOperationException($"GPU initialization failed for GPUSma indicator: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processes calculation using GPU acceleration.
        /// Обрабатывает вычисления с использованием GPU-ускорения.
        /// </summary>
        /// <param name="candles">List of candles / Список свечей</param>
        /// <param name="index">Current candle index / Текущий индекс свечи</param>
        private void ProcessWithGPU(List<Candle> candles, int index)
        {
            try
            {
                
                // For GPU processing, we'll process a batch of recent candles
                int startIndex = Math.Max(0, index - _length.ValueInt * 2);
                int endIndex = index + 1;
                
                if (endIndex - startIndex < _length.ValueInt)
                {
                    // Not enough data for GPU calculation - set to 0
                    _series.Values[index] = 0;
                    return;
                }

                var batchCandles = candles.GetRange(startIndex, endIndex - startIndex);
                
                // Create GPU indicator with correct period using the manager's factory method
                var parameters = new Dictionary<string, object>
                {
                    { "Period", _length.ValueInt },
                    { "Name", $"GPU_MA_{Name}" }
                };
                var gpuIndicator = _gpuManager.CreateIndicator<ILGPUMovingAverage>(parameters);

                var startTime = DateTime.Now;
                var result = gpuIndicator.CalculateAsync(batchCandles).Result;
                var executionTime = (DateTime.Now - startTime).TotalMilliseconds;
                
                if (result.IsSuccessful && result.Values.Count > 0)
                {
                    int gpuIndex = result.Values.Count - 1;
                    _series.Values[index] = result.Values[gpuIndex];
                    
                }
                else
                {
                    // GPU calculation failed - throw exception instead of CPU fallback
                    throw new InvalidOperationException($"GPU calculation failed for GPUSma indicator at index {index}");
                }
            }
            catch (Exception ex)
            {
                // GPU error - throw exception instead of CPU fallback
                throw new InvalidOperationException($"GPU calculation error for GPUSma indicator at index {index}: {ex.Message}", ex);
            }
        }



        /// <summary>
        /// Disposes GPU resources when indicator is deleted.
        /// Освобождает GPU ресурсы при удалении индикатора.
        /// </summary>
        public new void Delete()
        {
            try
            {
                // Clear all series data first to remove chart artifacts
                if (_series != null)
                {
                    _series.Values.Clear();
                }
                
                // Clear all data series
                if (DataSeries != null)
                {
                    foreach (var series in DataSeries)
                    {
                        series.Values.Clear();
                    }
                }
                
                // Dispose GPU resources
                _gpuManager?.Dispose();
                _gpuManager = null;
                _gpuInitialized = false;
                
                // Call base delete to complete cleanup
                base.Delete();
            }
            catch (Exception ex)
            {
                base.Delete(); // Ensure base cleanup still happens
            }
        }
    }
}
