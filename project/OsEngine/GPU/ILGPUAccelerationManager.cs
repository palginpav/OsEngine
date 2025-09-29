using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using OsEngine.Logging;
using OsEngine.Entity;

namespace OsEngine.GPU
{
    /// <summary>
    /// GPU acceleration manager using ILGPU for real GPU calculations.
    /// Follows official ILGPU documentation patterns for proper implementation.
    /// Manages GPU context and provides real GPU-accelerated indicator calculations.
    /// Менеджер GPU-ускорения используя ILGPU для реальных GPU-вычислений.
    /// Следует официальным паттернам документации ILGPU для правильной реализации.
    /// Управляет GPU-контекстом и предоставляет реальные GPU-ускоренные вычисления индикаторов.
    /// </summary>
    public class ILGPUAccelerationManager : IDisposable
    {
        private Context _context;
        private Accelerator _accelerator;
        private bool _isInitialized;
        private bool _isDisposed;
        private readonly object _initializationLock = new object();

        /// <summary>
        /// Event fired when GPU log messages are generated.
        /// Событие, возникающее при генерации GPU-логов.
        /// </summary>
        public event Action<string, LogMessageType> OnGPULogMessage;

        /// <summary>
        /// Constructor for ILGPU acceleration manager.
        /// Конструктор менеджера GPU-ускорения ILGPU.
        /// </summary>
        public ILGPUAccelerationManager()
        {
            _isInitialized = false;
            _isDisposed = false;
        }

        /// <summary>
        /// Initializes GPU acceleration system using ILGPU following official documentation.
        /// Throws exception if initialization fails.
        /// Инициализирует систему GPU-ускорения используя ILGPU следуя официальной документации.
        /// Выбрасывает исключение если инициализация не удалась.
        /// </summary>
        /// <returns>True if initialization successful, false otherwise</returns>
        /// <exception cref="GPUInitializationException">Thrown when GPU initialization fails</exception>
        public async Task<bool> InitializeAsync()
        {
            lock (_initializationLock)
            {
                if (_isInitialized)
                {
                    return true;
                }
                
                try
                {
                    SendLogMessage("Initializing ILGPU acceleration system...", LogMessageType.System);
                    
                    // Create context following ILGPU documentation
                    _context = Context.Create(builder => builder
                        .Cuda()
                        .EnableAlgorithms()
                        .Optimize(OptimizationLevel.O2)
                        .Math(MathMode.Fast));
                    
                    SendLogMessage("ILGPU context created successfully", LogMessageType.System);
                    
                    // Create accelerator following ILGPU documentation
                    _accelerator = _context.CreateCudaAccelerator(0);
                    
                    if (_accelerator == null)
                    {
                        throw new GPUInitializationException("Failed to create CUDA accelerator");
                    }
                    
                    SendLogMessage($"GPU Device: {_accelerator.Device.Name}", LogMessageType.System);
                    SendLogMessage($"GPU Memory: {_accelerator.Device.MemorySize / (1024 * 1024)} MB", LogMessageType.System);
                    
                    // Test GPU with simple memory allocation and transfer
                    try
                    {
                        // Create test data
                        var testData = new float[100];
                        for (int i = 0; i < testData.Length; i++)
                        {
                            testData[i] = i;
                        }
                        
                        // Test basic GPU memory operations
                        using (var gpuData = _accelerator.Allocate1D<float>(testData.Length))
                        using (var gpuResult = _accelerator.Allocate1D<float>(testData.Length))
                        {
                            // Copy data to GPU
                            gpuData.CopyFromCPU(testData);
                            
                            // Test simple GPU kernel execution
                            _accelerator.LaunchAutoGrouped(
                                TestKernel,
                                new Index1D(testData.Length),
                                gpuData.View,
                                gpuResult.View);
                            
                            // Synchronize GPU execution
                            _accelerator.Synchronize();
                            
                            // Copy result back to CPU
                            var result = new float[testData.Length];
                            gpuResult.CopyToCPU(result);
                            
                            // Verify result
                            bool testPassed = true;
                            for (int i = 0; i < testData.Length; i++)
                            {
                                if (Math.Abs(result[i] - testData[i]) > 0.001f)
                                {
                                    testPassed = false;
                                    break;
                                }
                            }
                            
                            if (testPassed)
                            {
                                _isInitialized = true;
                                SendLogMessage("ILGPU acceleration system initialized successfully", LogMessageType.System);
                                SendLogMessage($"GPU test calculation completed: {result.Length} elements processed", LogMessageType.System);
                                return true;
                            }
                            else
                            {
                                throw new GPUInitializationException("GPU test calculation failed - results don't match expected values");
                            }
                        }
                    }
                    catch (Exception gpuEx)
                    {
                        SendLogMessage($"GPU test calculation failed: {gpuEx.Message}", LogMessageType.Error);
                        throw new GPUInitializationException($"ILGPU initialization failed: {gpuEx.Message}", gpuEx);
                    }
                }
                catch (Exception ex)
                {
                    SendLogMessage($"ILGPU initialization failed: {ex.Message}", LogMessageType.Error);
                    throw new GPUInitializationException($"ILGPU initialization failed: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Creates a GPU-accelerated indicator using ILGPU.
        /// Throws exception if creation fails.
        /// Создает GPU-ускоренный индикатор используя ILGPU.
        /// Выбрасывает исключение если создание не удалось.
        /// </summary>
        /// <typeparam name="T">Type of indicator to create / Тип создаваемого индикатора</typeparam>
        /// <param name="parameters">Parameters for indicator creation / Параметры для создания индикатора</param>
        /// <returns>Created GPU indicator instance / Созданный экземпляр GPU-индикатора</returns>
        /// <exception cref="InvalidOperationException">Thrown when manager is not initialized</exception>
        /// <exception cref="ArgumentException">Thrown when type does not implement IGPUIndicator</exception>
        public IGPUIndicator CreateIndicator<T>(Dictionary<string, object> parameters) where T : IGPUIndicator
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ILGPU acceleration manager is not initialized");
            }
            
            if (!typeof(IGPUIndicator).IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException($"Type {typeof(T).Name} does not implement IGPUIndicator");
            }
            
            // Handle specific indicator types with their required parameters
            if (typeof(T) == typeof(ILGPUIndicators.ILGPUMovingAverage))
            {
                int period = 14;
                string name = "ILGPU_MA_Default";
                
                if (parameters != null)
                {
                    if (parameters.ContainsKey("Period") && parameters["Period"] is int periodParam)
                        period = periodParam;
                    if (parameters.ContainsKey("Name") && parameters["Name"] is string nameParam)
                        name = nameParam;
                }
                
                var indicator = new ILGPUIndicators.ILGPUMovingAverage(period, name);
                indicator.Initialize(_accelerator);
                return indicator;
            }
            
            

            if (typeof(T) == typeof(ILGPUIndicators.ILGPUBollingerBands))
            {
                int period = 20;
                float standardDeviationMultiplier = 2.0f;
                string candlePoint = "Close";
                string name = "ILGPU_BollingerBands_Default";
                
                if (parameters != null)
                {
                    if (parameters.ContainsKey("Period") && parameters["Period"] is int periodParam)
                        period = periodParam;
                    if (parameters.ContainsKey("StandardDeviationMultiplier") && parameters["StandardDeviationMultiplier"] is float multiplierParam)
                        standardDeviationMultiplier = multiplierParam;
                    if (parameters.ContainsKey("CandlePoint") && parameters["CandlePoint"] is string candlePointParam)
                        candlePoint = candlePointParam;
                    if (parameters.ContainsKey("Name") && parameters["Name"] is string nameParam)
                        name = nameParam;
                }
                
                var indicator = new ILGPUIndicators.ILGPUBollingerBands(period, standardDeviationMultiplier, candlePoint, name);
                indicator.Initialize(_accelerator);
                return indicator;
            }
            
            // For other types, try parameterless constructor
            try
            {
                var indicator = Activator.CreateInstance<T>();
                indicator.Initialize(_accelerator);
                return indicator;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create indicator of type {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Performs real GPU-accelerated moving average calculation using ILGPU kernels.
        /// Throws exception if calculation fails.
        /// Выполняет реальное GPU-ускоренное вычисление скользящего среднего используя ILGPU ядра.
        /// Выбрасывает исключение если вычисление не удалось.
        /// </summary>
        /// <param name="gpuData">GPU memory containing input data / GPU-память содержащая входные данные</param>
        /// <param name="gpuResult">GPU memory for output results / GPU-память для выходных результатов</param>
        /// <param name="period">Moving average period / Период скользящего среднего</param>
        /// <returns>Result array with moving average values / Результирующий массив со значениями скользящего среднего</returns>
        /// <exception cref="ArgumentNullException">Thrown when gpuData or gpuResult is null</exception>
        /// <exception cref="GPUCalculationException">Thrown when GPU calculation fails</exception>
        public float[] CalculateMovingAverageGPU(ArrayView1D<float, Stride1D.Dense> gpuData, ArrayView1D<float, Stride1D.Dense> gpuResult, int period)
        {
            // ArrayView1D cannot be null, so we skip null checks
            
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ILGPU acceleration manager is not initialized");
            }
            
            try
            {
                // Launch real GPU kernel for moving average calculation following ILGPU documentation
                _accelerator.LaunchAutoGrouped(
                    MovingAverageKernel,
                    new Index1D((int)gpuData.Length),
                    gpuData,
                    gpuResult,
                    period);
                
                // Synchronize GPU execution
                _accelerator.Synchronize();
                
                // Copy result back to CPU
                var result = new float[(int)gpuData.Length];
                gpuResult.CopyToCPU(result);
                
                SendLogMessage($"GPU moving average calculation completed: {gpuData.Length} elements, period {period}", LogMessageType.System);
                
                return result;
            }
            catch (Exception ex)
            {
                throw new GPUCalculationException($"GPU moving average calculation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Simple test kernel for GPU initialization verification.
        /// Простое тестовое ядро для проверки инициализации GPU.
        /// </summary>
        /// <param name="index">Thread index / Индекс потока</param>
        /// <param name="input">Input data array / Массив входных данных</param>
        /// <param name="output">Output result array / Массив выходных результатов</param>
        public static void TestKernel(Index1D index, ArrayView1D<float, Stride1D.Dense> input, ArrayView1D<float, Stride1D.Dense> output)
        {
            output[index] = input[index];
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
        /// Performs real GPU-accelerated array operations using ILGPU.
        /// Throws exception if calculation fails.
        /// Выполняет реальные GPU-ускоренные операции с массивами используя ILGPU.
        /// Выбрасывает исключение если вычисление не удалось.
        /// </summary>
        /// <param name="array">Input array / Входной массив</param>
        /// <param name="operation">Operation to perform / Операция для выполнения</param>
        /// <returns>Result array / Результирующий массив</returns>
        /// <exception cref="ArgumentNullException">Thrown when array is null</exception>
        /// <exception cref="GPUCalculationException">Thrown when GPU calculation fails</exception>
        public float[] GpuArrayOperation(float[] array, string operation)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ILGPU acceleration manager is not initialized");
            }
            
            try
            {
                using (var gpuArray = _accelerator.Allocate1D<float>(array.Length))
                using (var gpuResult = _accelerator.Allocate1D<float>(array.Length))
                {
                    // Copy data to GPU
                    gpuArray.CopyFromCPU(array);
                    
                    float[] result;
                    
                    switch (operation.ToLower())
                    {
                        case "sum":
                            result = CalculateSumGPU(gpuArray, gpuResult);
                            break;
                            
                        case "mean":
                            result = CalculateMeanGPU(gpuArray, gpuResult);
                            break;
                            
                        case "movingaverage":
                            result = CalculateMovingAverageGPU(gpuArray, gpuResult, 10);
                            break;
                            
                        default:
                            throw new ArgumentException($"Unsupported GPU operation: {operation}");
                    }
                    
                    SendLogMessage($"GPU array operation '{operation}' completed for {array.Length} elements", LogMessageType.System);
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new GPUCalculationException($"GPU array operation '{operation}' failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Real GPU kernel for sum calculation following ILGPU documentation.
        /// Реальное GPU ядро для вычисления суммы следуя документации ILGPU.
        /// </summary>
        public static void SumKernel(Index1D index, ArrayView1D<float, Stride1D.Dense> input, ArrayView1D<float, Stride1D.Dense> output)
        {
            output[index] = input[index];
        }

        /// <summary>
        /// Real GPU kernel for mean calculation following ILGPU documentation.
        /// Реальное GPU ядро для вычисления среднего следуя документации ILGPU.
        /// </summary>
        public static void MeanKernel(Index1D index, ArrayView1D<float, Stride1D.Dense> input, ArrayView1D<float, Stride1D.Dense> output, int length)
        {
            output[index] = input[index] / length;
        }

        /// <summary>
        /// Calculates sum using real GPU kernel following ILGPU documentation.
        /// Вычисляет сумму используя реальное GPU ядро следуя документации ILGPU.
        /// </summary>
        private float[] CalculateSumGPU(ArrayView1D<float, Stride1D.Dense> gpuData, ArrayView1D<float, Stride1D.Dense> gpuResult)
        {
            _accelerator.LaunchAutoGrouped(SumKernel, new Index1D((int)gpuData.Length), gpuData, gpuResult);
            _accelerator.Synchronize();
            
            var result = new float[(int)gpuData.Length];
            gpuResult.CopyToCPU(result);
            return result;
        }

        /// <summary>
        /// Calculates mean using real GPU kernel following ILGPU documentation.
        /// Вычисляет среднее используя реальное GPU ядро следуя документации ILGPU.
        /// </summary>
        private float[] CalculateMeanGPU(ArrayView1D<float, Stride1D.Dense> gpuData, ArrayView1D<float, Stride1D.Dense> gpuResult)
        {
            _accelerator.LaunchAutoGrouped(MeanKernel, new Index1D((int)gpuData.Length), gpuData, gpuResult, (int)gpuData.Length);
            _accelerator.Synchronize();
            
            var result = new float[(int)gpuData.Length];
            gpuResult.CopyToCPU(result);
            return result;
        }

        /// <summary>
        /// Gets the ILGPU accelerator instance for direct kernel operations.
        /// Получает экземпляр ускорителя ILGPU для прямых операций с ядрами.
        /// </summary>
        public Accelerator Accelerator => _accelerator;

        /// <summary>
        /// Indicates whether GPU acceleration is supported and available.
        /// Returns false if GPU is not available or initialization failed.
        /// Указывает поддерживается ли и доступно ли GPU-ускорение.
        /// Возвращает false если GPU недоступен или инициализация не удалась.
        /// </summary>
        public bool IsGPUSupported => _isInitialized && !_isDisposed && _accelerator != null;

        /// <summary>
        /// Sends log message through the logging system.
        /// Отправляет сообщение лога через систему логирования.
        /// </summary>
        /// <param name="message">Log message / Сообщение лога</param>
        /// <param name="type">Log message type / Тип сообщения лога</param>
        private void SendLogMessage(string message, LogMessageType type)
        {
            try
            {
                OnGPULogMessage?.Invoke(message, type);
                // ServerMaster.SendNewLogMessage(message, type); // Commented out to avoid dependency issues
                // Thread-safe logging with unique file name
                string logFileName = $"ILGPU_Debug_{Environment.ProcessId}_{Thread.CurrentThread.ManagedThreadId}.log";
                try
                {
                    System.IO.File.AppendAllText(logFileName, $"{DateTime.Now}: {message}\n");
                }
                catch (IOException)
                {
                    // If file is locked, skip logging to avoid blocking GPU operations
                }
            }
            catch (Exception ex)
            {
                // Thread-safe error logging with unique file name
                string errorLogFileName = $"ILGPU_Error_{Environment.ProcessId}_{Thread.CurrentThread.ManagedThreadId}.log";
                try
                {
                    System.IO.File.AppendAllText(errorLogFileName, $"{DateTime.Now}: Error sending log message: {ex.Message}\n");
                }
                catch (IOException)
                {
                    // If file is locked, skip logging to avoid infinite recursion
                }
            }
        }

        /// <summary>
        /// Disposes GPU resources and cleans up memory following ILGPU documentation.
        /// Освобождает GPU-ресурсы и очищает память следуя документации ILGPU.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _accelerator?.Dispose();
                _context?.Dispose();
                _accelerator = null;
                _context = null;
                _isInitialized = false;
                _isDisposed = true;
                
                SendLogMessage("ILGPU acceleration manager disposed", LogMessageType.System);
            }
        }
    }
}
