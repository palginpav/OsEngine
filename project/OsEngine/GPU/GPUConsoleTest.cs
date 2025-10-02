/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Threading.Tasks;
using OsEngine.Logging;

namespace OsEngine.GPU
{
    /// <summary>
    /// Console application for testing GPU acceleration functionality.
    /// Provides command-line interface for GPU validation and testing.
    /// Консольное приложение для тестирования функциональности GPU-ускорения.
    /// Предоставляет интерфейс командной строки для валидации и тестирования GPU.
    /// </summary>
    public class GPUConsoleTest
    {
        private GPUValidationProgram _validationProgram;
        
        /// <summary>
        /// Main entry point for GPU console test application.
        /// Only available when building as console application to avoid conflicts with WPF App.Main().
        /// Главная точка входа для консольного приложения тестирования GPU.
        /// Доступна только при сборке как консольное приложение, чтобы избежать конфликтов с WPF App.Main().
        /// </summary>
        /// <param name="args">Command line arguments / Аргументы командной строки</param>
        /// <returns>Exit code / Код выхода</returns>
#if CONSOLE_APP
        public static async Task<int> Main(string[] args)
        {
            var consoleTest = new GPUConsoleTest();
            return await consoleTest.RunAsync(args);
        }
#endif
        
        /// <summary>
        /// Runs the GPU console test application with specified arguments.
        /// Запускает консольное приложение тестирования GPU с указанными аргументами.
        /// </summary>
        /// <param name="args">Command line arguments / Аргументы командной строки</param>
        /// <returns>Exit code / Код выхода</returns>
        public async Task<int> RunAsync(string[] args)
        {
            try
            {
                Console.WriteLine("=== OsEngine GPU Acceleration Test ===");
                Console.WriteLine("=== Тест GPU-ускорения OsEngine ===");
                Console.WriteLine();
                
                _validationProgram = new GPUValidationProgram("ConsoleTest");
                _validationProgram.LogMessageEvent += OnLogMessage;
                
                // Parse command line arguments
                bool runQuickTest = args.Length > 0 && args[0].ToLower() == "quick";
                
                if (runQuickTest)
                {
                    Console.WriteLine("Running quick GPU test...");
                    Console.WriteLine("Запуск быстрого теста GPU...");
                    Console.WriteLine();
                    
                    var quickResult = await _validationProgram.RunQuickTestAsync();
                    
                    if (quickResult)
                    {
                        Console.WriteLine();
                        Console.WriteLine("✓ Quick GPU test PASSED / Быстрый тест GPU ПРОЙДЕН");
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("✗ Quick GPU test FAILED / Быстрый тест GPU НЕ ПРОЙДЕН");
                        return 1;
                    }
                }
                else
                {
                    Console.WriteLine("Running comprehensive GPU validation...");
                    Console.WriteLine("Запуск комплексной валидации GPU...");
                    Console.WriteLine();
                    
                    var summary = await _validationProgram.RunValidationAsync();
                    
                    Console.WriteLine();
                    Console.WriteLine("=== GPU Validation Summary / Сводка валидации GPU ===");
                    Console.WriteLine($"Start Time / Время начала: {summary.StartTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"End Time / Время окончания: {summary.EndTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Total Execution Time / Общее время выполнения: {summary.TotalExecutionTime.TotalSeconds:F2} seconds");
                    Console.WriteLine($"Successful Tests / Успешные тесты: {summary.SuccessfulTests}");
                    Console.WriteLine($"Failed Tests / Неудачные тесты: {summary.FailedTests}");
                    Console.WriteLine($"Total Data Points Processed / Общее количество обработанных точек: {summary.TotalDataPointsProcessed:N0}");
                    Console.WriteLine($"Average Performance / Средняя производительность: {summary.AverageDataPointsPerSecond:F0} points/second");
                    Console.WriteLine();
                    
                    if (summary.IsOverallSuccessful)
                    {
                        Console.WriteLine("✓ GPU validation PASSED / Валидация GPU ПРОЙДЕНА");
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine("✗ GPU validation FAILED / Валидация GPU НЕ ПРОЙДЕНА");
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"✗ GPU test failed with exception / Тест GPU не удался с исключением:");
                Console.WriteLine($"Error / Ошибка: {ex.Message}");
                Console.WriteLine($"Stack Trace / Трассировка стека: {ex.StackTrace}");
                return 1;
            }
            finally
            {
                _validationProgram?.Dispose();
            }
        }
        
        /// <summary>
        /// Handles log messages from GPU validation program.
        /// Обрабатывает сообщения логов от программы валидации GPU.
        /// </summary>
        /// <param name="message">Log message / Сообщение лога</param>
        /// <param name="type">Log message type / Тип сообщения лога</param>
        private void OnLogMessage(string message, LogMessageType type)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = type switch
            {
                LogMessageType.Error => "ERROR",
                LogMessageType.System => "INFO ",
                _ => "DEBUG"
            };
            
            Console.WriteLine($"[{timestamp}] {prefix}: {message}");
        }
    }
}
