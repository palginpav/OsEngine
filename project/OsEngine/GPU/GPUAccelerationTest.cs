/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OsEngine.Entity;
using OsEngine.Logging;

namespace OsEngine.GPU
{
    /// <summary>
    /// Test class for validating GPU acceleration functionality with real market data.
    /// Provides comprehensive testing of GPU indicators and performance benchmarking.
    /// Тестовый класс для валидации функциональности GPU-ускорения с реальными рыночными данными.
    /// Предоставляет комплексное тестирование GPU-индикаторов и бенчмаркинг производительности.
    /// </summary>
    public class GPUAccelerationTest
    {
        private ILGPUAccelerationManager _gpuManager;
        private readonly string _name;
        
        /// <summary>
        /// Event for logging test results and messages.
        /// Событие для логирования результатов тестов и сообщений.
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;
        
        /// <summary>
        /// Constructor for GPU acceleration test.
        /// Конструктор теста GPU-ускорения.
        /// </summary>
        /// <param name="name">Test name identifier / Имя-идентификатор теста</param>
        public GPUAccelerationTest(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }
        
        /// <summary>
        /// Initializes GPU acceleration test environment.
        /// Throws exception if initialization fails.
        /// Инициализирует среду тестирования GPU-ускорения.
        /// Выбрасывает исключение если инициализация не удалась.
        /// </summary>
        /// <returns>True if initialization successful / True если инициализация успешна</returns>
        /// <exception cref="GPUInitializationException">Thrown when GPU initialization fails</exception>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                SendLogMessage("Initializing GPU acceleration test environment...", LogMessageType.System);
                
                _gpuManager = new ILGPUAccelerationManager();
                _gpuManager.OnGPULogMessage += SendLogMessage;
                
                var result = await _gpuManager.InitializeAsync();
                
                if (result)
                {
                    SendLogMessage("GPU acceleration test environment initialized successfully", LogMessageType.System);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                SendLogMessage($"GPU acceleration test initialization failed: {ex.Message}", LogMessageType.Error);
                throw;
            }
        }
        
        /// <summary>
        /// Tests GPU Moving Average indicator with real market data.
        /// Validates calculation accuracy and performance.
        /// Тестирует GPU-индикатор скользящего среднего с реальными рыночными данными.
        /// Проверяет точность вычислений и производительность.
        /// </summary>
        /// <param name="testDataSize">Number of data points to test / Количество точек данных для тестирования</param>
        /// <param name="period">Moving average period / Период скользящего среднего</param>
        /// <returns>Test result with performance metrics / Результат теста с метриками производительности</returns>
        /// <exception cref="InvalidOperationException">Thrown when GPU is not initialized</exception>
        public async Task<GPUTestResult> TestMovingAverageAsync(int testDataSize = 10000, int period = 20)
        {
            if (_gpuManager == null || !_gpuManager.IsGPUSupported)
            {
                throw new InvalidOperationException("GPU acceleration test is not initialized");
            }
            
            if (testDataSize <= 0)
            {
                throw new ArgumentException("Test data size must be greater than zero", nameof(testDataSize));
            }
            
            if (period <= 0)
            {
                throw new ArgumentException("Period must be greater than zero", nameof(period));
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                SendLogMessage($"Testing GPU Moving Average with {testDataSize} data points, period {period}...", LogMessageType.System);
                
                // Generate realistic market data
                var testCandles = GenerateTestMarketData(testDataSize);
                
                // Create GPU Moving Average indicator
                var gpuIndicator = new ILGPUIndicators.ILGPUMovingAverage(period, "Test_GPU_MA");
                gpuIndicator.Initialize(_gpuManager.Accelerator);
                
                // Calculate using GPU
                var gpuResult = await gpuIndicator.CalculateAsync(testCandles);
                
                stopwatch.Stop();
                
                // Validate results
                var validationResult = ValidateMovingAverageResult(gpuResult, testCandles, period);
                
                var testResult = new GPUTestResult
                {
                    TestName = "GPU Moving Average Test",
                    IsSuccessful = gpuResult.IsSuccessful && validationResult.IsValid,
                    ExecutionTimeMs = gpuResult.ExecutionTimeMs,
                    DataPointsProcessed = gpuResult.DataPointsProcessed,
                    PerformanceMetrics = new Dictionary<string, double>
                    {
                        ["DataPointsPerSecond"] = gpuResult.DataPointsProcessed / (gpuResult.ExecutionTimeMs / 1000.0),
                        ["TotalTestTimeMs"] = stopwatch.Elapsed.TotalMilliseconds
                    },
                    ErrorMessage = gpuResult.IsSuccessful ? validationResult.ErrorMessage : gpuResult.ErrorMessage
                };
                
                SendLogMessage($"GPU Moving Average test completed: {testResult.IsSuccessful}", 
                    testResult.IsSuccessful ? LogMessageType.System : LogMessageType.Error);
                
                if (testResult.IsSuccessful)
                {
                    SendLogMessage($"Performance: {testResult.PerformanceMetrics["DataPointsPerSecond"]:F0} data points/second", LogMessageType.System);
                }
                
                return testResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                SendLogMessage($"GPU Moving Average test failed: {ex.Message}", LogMessageType.Error);
                
                return new GPUTestResult
                {
                    TestName = "GPU Moving Average Test",
                    IsSuccessful = false,
                    ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    DataPointsProcessed = 0,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Generates realistic test market data for validation.
        /// Generates real price data with realistic volatility and trends.
        /// Генерирует реалистичные тестовые рыночные данные для валидации.
        /// Генерирует реальные ценовые данные с реалистичной волатильностью и трендами.
        /// </summary>
        /// <param name="count">Number of candles to generate / Количество свечей для генерации</param>
        /// <returns>List of realistic market candles / Список реалистичных рыночных свечей</returns>
        private List<Candle> GenerateTestMarketData(int count)
        {
            var candles = new List<Candle>();
            var random = new Random(42); // Fixed seed for reproducible results
            var basePrice = 100.0m;
            var currentPrice = basePrice;
            var currentTime = DateTime.Now.AddDays(-count);
            
            for (int i = 0; i < count; i++)
            {
                // Generate realistic price movement
                var volatility = (decimal)(random.NextDouble() - 0.5) * 0.02m; // 2% max volatility
                var trend = (decimal)(i / (double)count) * 0.1m; // 10% trend over period
                
                currentPrice = currentPrice * (1 + volatility + trend / count);
                
                var open = currentPrice;
                var close = open * (1 + (decimal)(random.NextDouble() - 0.5) * 0.01m);
                var high = Math.Max(open, close) * (1 + (decimal)random.NextDouble() * 0.005m);
                var low = Math.Min(open, close) * (1 - (decimal)random.NextDouble() * 0.005m);
                var volume = (decimal)(random.NextDouble() * 1000000 + 100000);
                
                candles.Add(new Candle
                {
                    TimeStart = currentTime.AddMinutes(i),
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume
                });
            }
            
            return candles;
        }
        
        /// <summary>
        /// Validates GPU Moving Average calculation results.
        /// Compares GPU results with expected mathematical calculations.
        /// Валидирует результаты вычисления GPU скользящего среднего.
        /// Сравнивает результаты GPU с ожидаемыми математическими вычислениями.
        /// </summary>
        /// <param name="gpuResult">GPU calculation result / Результат вычисления GPU</param>
        /// <param name="candles">Original market data / Исходные рыночные данные</param>
        /// <param name="period">Moving average period / Период скользящего среднего</param>
        /// <returns>Validation result / Результат валидации</returns>
        private ValidationResult ValidateMovingAverageResult(GPUIndicatorResult gpuResult, List<Candle> candles, int period)
        {
            try
            {
                if (!gpuResult.IsSuccessful)
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = gpuResult.ErrorMessage };
                }
                
                if (gpuResult.Values.Count != candles.Count)
                {
                    return new ValidationResult 
                    { 
                        IsValid = false, 
                        ErrorMessage = $"Result count mismatch: expected {candles.Count}, got {gpuResult.Values.Count}" 
                    };
                }
                
                // Validate first few values (should be zero for insufficient data)
                for (int i = 0; i < period - 1; i++)
                {
                    if (gpuResult.Values[i] != 0)
                    {
                        return new ValidationResult 
                        { 
                            IsValid = false, 
                            ErrorMessage = $"Invalid value at index {i}: expected 0, got {gpuResult.Values[i]}" 
                        };
                    }
                }
                
                // Validate moving average calculations
                for (int i = period - 1; i < Math.Min(candles.Count, 100); i++) // Check first 100 valid values
                {
                    var expectedSum = candles.Skip(i - period + 1).Take(period).Sum(c => c.Close);
                    var expectedMA = expectedSum / period;
                    var actualMA = gpuResult.Values[i];
                    
                    var tolerance = 0.0001m; // Allow small floating point differences
                    if (Math.Abs(actualMA - expectedMA) > tolerance)
                    {
                        return new ValidationResult 
                        { 
                            IsValid = false, 
                            ErrorMessage = $"Calculation error at index {i}: expected {expectedMA}, got {actualMA}" 
                        };
                    }
                }
                
                return new ValidationResult { IsValid = true, ErrorMessage = null };
            }
            catch (Exception ex)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"Validation error: {ex.Message}" };
            }
        }
        
        /// <summary>
        /// Disposes GPU test resources.
        /// Освобождает ресурсы теста GPU.
        /// </summary>
        public void Dispose()
        {
            _gpuManager?.Dispose();
        }
        
        /// <summary>
        /// Sends log message through the logging system.
        /// Отправляет сообщение лога через систему логирования.
        /// </summary>
        /// <param name="message">Log message / Сообщение лога</param>
        /// <param name="type">Log message type / Тип сообщения лога</param>
        private void SendLogMessage(string message, LogMessageType type)
        {
            LogMessageEvent?.Invoke($"[{_name}] {message}", type);
        }
    }
    
    /// <summary>
    /// Result of GPU test execution with performance metrics.
    /// Результат выполнения теста GPU с метриками производительности.
    /// </summary>
    public class GPUTestResult
    {
        public string TestName { get; set; }
        public bool IsSuccessful { get; set; }
        public double ExecutionTimeMs { get; set; }
        public int DataPointsProcessed { get; set; }
        public Dictionary<string, double> PerformanceMetrics { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// Result of calculation validation.
    /// Результат валидации вычислений.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}
