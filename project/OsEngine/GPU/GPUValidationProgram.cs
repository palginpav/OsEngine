/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OsEngine.Entity;
using OsEngine.Logging;

namespace OsEngine.GPU
{
    /// <summary>
    /// GPU validation program for testing GPU acceleration functionality.
    /// Provides comprehensive testing and validation of GPU indicators with real market data.
    /// Программа валидации GPU для тестирования функциональности GPU-ускорения.
    /// Предоставляет комплексное тестирование и валидацию GPU-индикаторов с реальными рыночными данными.
    /// </summary>
    public class GPUValidationProgram
    {
        private GPUAccelerationTest _gpuTest;
        private readonly string _name;
        
        /// <summary>
        /// Event for logging validation results and messages.
        /// Событие для логирования результатов валидации и сообщений.
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;
        
        /// <summary>
        /// Constructor for GPU validation program.
        /// Конструктор программы валидации GPU.
        /// </summary>
        /// <param name="name">Program name identifier / Имя-идентификатор программы</param>
        public GPUValidationProgram(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }
        
        /// <summary>
        /// Runs comprehensive GPU validation tests with real hardware and data.
        /// Executes all GPU tests and provides detailed performance metrics.
        /// Запускает комплексные тесты валидации GPU с реальным оборудованием и данными.
        /// Выполняет все GPU-тесты и предоставляет детальные метрики производительности.
        /// </summary>
        /// <returns>Validation result summary / Сводка результатов валидации</returns>
        /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
        public async Task<GPUValidationSummary> RunValidationAsync()
        {
            try
            {
                SendLogMessage("Starting GPU validation program...", LogMessageType.System);
                
                var summary = new GPUValidationSummary
                {
                    StartTime = DateTime.Now,
                    TestResults = new List<GPUTestResult>()
                };
                
                // Initialize GPU test environment
                _gpuTest = new GPUAccelerationTest($"{_name}_Test");
                _gpuTest.LogMessageEvent += SendLogMessage;
                
                var initResult = await _gpuTest.InitializeAsync();
                if (!initResult)
                {
                    throw new InvalidOperationException("Failed to initialize GPU test environment");
                }
                
                SendLogMessage("GPU test environment initialized successfully", LogMessageType.System);
                
                // Test 1: Small dataset (1000 points)
                SendLogMessage("Running test 1: Small dataset (1000 points)...", LogMessageType.System);
                var test1Result = await _gpuTest.TestMovingAverageAsync(1000, 20);
                summary.TestResults.Add(test1Result);
                
                // Test 2: Medium dataset (10000 points)
                SendLogMessage("Running test 2: Medium dataset (10000 points)...", LogMessageType.System);
                var test2Result = await _gpuTest.TestMovingAverageAsync(10000, 50);
                summary.TestResults.Add(test2Result);
                
                // Test 3: Large dataset (100000 points)
                SendLogMessage("Running test 3: Large dataset (100000 points)...", LogMessageType.System);
                var test3Result = await _gpuTest.TestMovingAverageAsync(100000, 100);
                summary.TestResults.Add(test3Result);
                
                // Calculate summary statistics
                summary.EndTime = DateTime.Now;
                summary.TotalExecutionTime = summary.EndTime - summary.StartTime;
                summary.SuccessfulTests = summary.TestResults.Count(t => t.IsSuccessful);
                summary.FailedTests = summary.TestResults.Count(t => !t.IsSuccessful);
                summary.TotalDataPointsProcessed = summary.TestResults.Sum(t => t.DataPointsProcessed);
                summary.AverageDataPointsPerSecond = summary.TestResults
                    .Where(t => t.IsSuccessful && t.PerformanceMetrics.ContainsKey("DataPointsPerSecond"))
                    .Average(t => t.PerformanceMetrics["DataPointsPerSecond"]);
                
                // Determine overall success
                summary.IsOverallSuccessful = summary.FailedTests == 0;
                
                // Log summary
                SendLogMessage($"GPU validation completed: {summary.SuccessfulTests}/{summary.TestResults.Count} tests passed", 
                    summary.IsOverallSuccessful ? LogMessageType.System : LogMessageType.Error);
                
                if (summary.IsOverallSuccessful)
                {
                    SendLogMessage($"Average performance: {summary.AverageDataPointsPerSecond:F0} data points/second", LogMessageType.System);
                    SendLogMessage($"Total data processed: {summary.TotalDataPointsProcessed:N0} points", LogMessageType.System);
                }
                
                return summary;
            }
            catch (Exception ex)
            {
                SendLogMessage($"GPU validation failed: {ex.Message}", LogMessageType.Error);
                throw;
            }
            finally
            {
                _gpuTest?.Dispose();
            }
        }
        
        /// <summary>
        /// Runs a quick GPU functionality test with minimal data.
        /// Useful for basic validation without extensive testing.
        /// Запускает быстрый тест функциональности GPU с минимальными данными.
        /// Полезен для базовой валидации без обширного тестирования.
        /// </summary>
        /// <returns>True if basic GPU functionality works / True если базовая функциональность GPU работает</returns>
        public async Task<bool> RunQuickTestAsync()
        {
            try
            {
                SendLogMessage("Running quick GPU functionality test...", LogMessageType.System);
                
                _gpuTest = new GPUAccelerationTest($"{_name}_QuickTest");
                _gpuTest.LogMessageEvent += SendLogMessage;
                
                var initResult = await _gpuTest.InitializeAsync();
                if (!initResult)
                {
                    SendLogMessage("GPU initialization failed", LogMessageType.Error);
                    return false;
                }
                
                var testResult = await _gpuTest.TestMovingAverageAsync(100, 10);
                
                if (testResult.IsSuccessful)
                {
                    SendLogMessage("Quick GPU test passed successfully", LogMessageType.System);
                    return true;
                }
                else
                {
                    SendLogMessage($"Quick GPU test failed: {testResult.ErrorMessage}", LogMessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                SendLogMessage($"Quick GPU test failed with exception: {ex.Message}", LogMessageType.Error);
                return false;
            }
            finally
            {
                _gpuTest?.Dispose();
            }
        }
        
        /// <summary>
        /// Disposes GPU validation program resources.
        /// Освобождает ресурсы программы валидации GPU.
        /// </summary>
        public void Dispose()
        {
            _gpuTest?.Dispose();
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
    /// Summary of GPU validation test results with comprehensive metrics.
    /// Сводка результатов тестов валидации GPU с комплексными метриками.
    /// </summary>
    public class GPUValidationSummary
    {
        /// <summary>
        /// Start time of validation process.
        /// Время начала процесса валидации.
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// End time of validation process.
        /// Время окончания процесса валидации.
        /// </summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// Total execution time of all tests.
        /// Общее время выполнения всех тестов.
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }
        
        /// <summary>
        /// List of individual test results.
        /// Список результатов отдельных тестов.
        /// </summary>
        public List<GPUTestResult> TestResults { get; set; }
        
        /// <summary>
        /// Number of successful tests.
        /// Количество успешных тестов.
        /// </summary>
        public int SuccessfulTests { get; set; }
        
        /// <summary>
        /// Number of failed tests.
        /// Количество неудачных тестов.
        /// </summary>
        public int FailedTests { get; set; }
        
        /// <summary>
        /// Total data points processed across all tests.
        /// Общее количество обработанных точек данных во всех тестах.
        /// </summary>
        public int TotalDataPointsProcessed { get; set; }
        
        /// <summary>
        /// Average data points processed per second.
        /// Среднее количество обработанных точек данных в секунду.
        /// </summary>
        public double AverageDataPointsPerSecond { get; set; }
        
        /// <summary>
        /// Indicates whether overall validation was successful.
        /// Указывает была ли общая валидация успешной.
        /// </summary>
        public bool IsOverallSuccessful { get; set; }
    }
}
